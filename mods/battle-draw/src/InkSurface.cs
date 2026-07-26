using Godot;

namespace BattleDraw;

/// <summary>
/// Combat ink that mirrors vanilla map drawing (<see cref="MegaCrit.Sts2.Core.Nodes.Screens.Map.NMapDrawings"/>):
/// <list type="bullet">
/// <item>Half-resolution transparent <see cref="SubViewport"/>s (local + remote)</item>
/// <item>Draw/erase as live <see cref="Line2D"/> from map_line_draw / map_line_erase scenes</item>
/// <item>Eraser uses subtractive blend shader (full wipe, no residual "negative" ink)</item>
/// <item>Display TextureRects use premultiplied-alpha blend</item>
/// </list>
/// Points are stored in viewport space (screen × <see cref="ResScale"/>).
/// </summary>
public sealed class InkSurface
{
    /// <summary>Same half-res scale as NMapDrawings (points × 0.5).</summary>
    public const float ResScale = 0.5f;

    private const string DrawScenePath = "res://scenes/screens/map/map_line_draw.tscn";
    private const string EraseScenePath = "res://scenes/screens/map/map_line_erase.tscn";
    private const string DrawShaderPath = "res://shaders/map_drawing/line_draw.gdshader";
    private const string EraseShaderPath = "res://shaders/map_drawing/line_erase.gdshader";
    private const string DrawTrailPath = "res://images/packed/vfx/trail2.png";
    private const string EraseTrailPath = "res://images/packed/vfx/trail3.png";

    private PackedScene? _drawScene;
    private PackedScene? _eraseScene;
    private Material? _drawMaterialTemplate;
    private Material? _eraseMaterialTemplate;
    private Texture2D? _drawTrail;
    private Texture2D? _eraseTrail;
    private bool _assetsReady;

    private SubViewport? _localVp;
    private SubViewport? _remoteVp;
    private TextureRect? _localRect;
    private TextureRect? _remoteRect;
    private Vector2 _viewportSize = new(1920, 1080);
    private Vector2I _inkSize = new(960, 540);

    public Node2D? Root { get; private set; }

    public void Attach(Node2D parent)
    {
        Root = parent;
        EnsureAssets();

        // Remote under local so local ink paints over peer doodles.
        (_remoteVp, _remoteRect) = MakeLayer("RemoteDraw", parent, z: 0);
        (_localVp, _localRect) = MakeLayer("LocalDraw", parent, z: 1);
        EnsureSize(new Vector2(1920, 1080));
    }

    private (SubViewport vp, TextureRect rect) MakeLayer(string name, Node parent, int z)
    {
        // SubViewport must live in the tree to produce a texture. Not under SubViewportContainer —
        // we composite via TextureRect + PremultAlpha like map_drawing.tscn.
        var vp = new SubViewport
        {
            Name = name + "Viewport",
            Disable3D = true,
            TransparentBg = true,
            HandleInputLocally = false,
            RenderTargetUpdateMode = SubViewport.UpdateMode.WhenVisible,
            Size = _inkSize,
            // Avoid inheriting world lights; 2D-only ink.
            OwnWorld3D = true,
        };
        parent.AddChild(vp);

        var rect = new TextureRect
        {
            Name = name + "Texture",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
            ZIndex = z,
            // blend_mode = 4 → PremultipliedAlpha (map_drawing DrawViewportTextureRect)
            Material = new CanvasItemMaterial
            {
                BlendMode = CanvasItemMaterial.BlendModeEnum.PremultAlpha,
            },
        };
        parent.AddChild(rect);

        // GetTexture is valid once the viewport is inside the tree.
        rect.Texture = vp.GetTexture();
        return (vp, rect);
    }

    public void EnsureSize(Vector2 viewportSize)
    {
        _viewportSize = viewportSize;
        int w = Math.Max(64, (int)(viewportSize.X * ResScale));
        int h = Math.Max(64, (int)(viewportSize.Y * ResScale));
        if (_inkSize.X == w && _inkSize.Y == h && _localVp != null)
        {
            SyncRectSizes();
            return;
        }

        _inkSize = new Vector2I(w, h);
        if (_localVp != null)
            _localVp.Size = _inkSize;
        if (_remoteVp != null)
            _remoteVp.Size = _inkSize;
        SyncRectSizes();
    }

    private void SyncRectSizes()
    {
        if (_localRect != null)
        {
            _localRect.Position = Vector2.Zero;
            _localRect.Size = _viewportSize;
            if (_localVp != null)
                _localRect.Texture = _localVp.GetTexture();
        }

        if (_remoteRect != null)
        {
            _remoteRect.Position = Vector2.Zero;
            _remoteRect.Size = _viewportSize;
            if (_remoteVp != null)
                _remoteRect.Texture = _remoteVp.GetTexture();
        }
    }

    public void SetRemoteVisible(bool visible)
    {
        if (_remoteRect != null && GodotObject.IsInstanceValid(_remoteRect))
            _remoteRect.Visible = visible;
    }

    /// <summary>
    /// Create a map-style stroke Line2D parented into the local or remote viewport.
    /// Width uses screen/brush units (same numbers as map pen restyle); points go in half-res space.
    /// </summary>
    public Line2D BeginStroke(bool erase, Color color, float width, bool remote)
    {
        EnsureAssets();
        SubViewport? vp = remote ? _remoteVp : _localVp;
        if (vp == null)
            throw new InvalidOperationException("InkSurface not attached.");

        // Keep updating while someone is actively drawing so strokes appear immediately.
        vp.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;

        Line2D line = CreateLine(erase);
        // Eraser: white subtracts all channels (combat is multi-color; map uses monochrome per player).
        line.DefaultColor = erase ? Colors.White : color;
        // Match MapBrushPatches: pen = size; eraser thicker for a full wipe.
        if (erase)
        {
            float w = Math.Max(width * 2.5f, width + 4f);
            line.Width = Math.Clamp(w, 4f, 48f);
        }
        else
        {
            line.Width = Math.Clamp(Math.Max(1f, width), 1f, 48f);
        }

        line.Position = Vector2.Zero;
        line.ClearPoints();
        vp.AddChild(line);
        return line;
    }

    public void AddPointScreen(Line2D line, Vector2 screenPos)
    {
        if (line == null || !GodotObject.IsInstanceValid(line))
            return;
        line.AddPoint(screenPos * ResScale);
    }

    public void SeedStroke(Line2D line, Vector2 screenPos)
    {
        // Map seeds two near-identical points so caps/texture have length.
        Vector2 p = screenPos * ResScale;
        line.AddPoint(p);
        line.AddPoint(p + new Vector2(0f, 0.5f));
    }

    public void EndStrokeActivity()
    {
        // Drop to WhenVisible after a stroke so idle combat does not pay Always cost.
        if (_localVp != null)
            _localVp.RenderTargetUpdateMode = SubViewport.UpdateMode.WhenVisible;
        if (_remoteVp != null)
            _remoteVp.RenderTargetUpdateMode = SubViewport.UpdateMode.WhenVisible;
    }

    public void ClearLocal() => FreeLines(_localVp);

    public void ClearRemote() => FreeLines(_remoteVp);

    public void ClearAll()
    {
        ClearLocal();
        ClearRemote();
    }

    private static void FreeLines(SubViewport? vp)
    {
        if (vp == null || !GodotObject.IsInstanceValid(vp))
            return;
        foreach (Node child in vp.GetChildren())
        {
            if (child is Line2D)
                child.QueueFree();
        }
    }

    private Line2D CreateLine(bool erase)
    {
        PackedScene? scene = erase ? _eraseScene : _drawScene;
        if (scene != null)
        {
            try
            {
                Line2D line = scene.Instantiate<Line2D>(PackedScene.GenEditState.Disabled);
                line.ClearPoints();
                line.Position = Vector2.Zero;
                return line;
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"Instantiate map line scene failed, using fallback: {e.Message}");
            }
        }

        return CreateFallbackLine(erase);
    }

    private Line2D CreateFallbackLine(bool erase)
    {
        var line = new Line2D
        {
            Antialiased = false,
            JointMode = Line2D.LineJointMode.Round,
            BeginCapMode = Line2D.LineCapMode.Round,
            EndCapMode = Line2D.LineCapMode.Round,
            SharpLimit = 4f,
            TextureMode = Line2D.LineTextureMode.Tile,
            Width = erase ? 12f : 4f,
            DefaultColor = Colors.White,
        };

        Texture2D? trail = erase ? _eraseTrail : _drawTrail;
        if (trail != null)
            line.Texture = trail;

        Material? mat = erase ? _eraseMaterialTemplate : _drawMaterialTemplate;
        if (mat != null)
            line.Material = (Material)mat.Duplicate();

        return line;
    }

    private void EnsureAssets()
    {
        if (_assetsReady)
            return;
        _assetsReady = true;

        _drawScene = TryLoadScene(DrawScenePath);
        _eraseScene = TryLoadScene(EraseScenePath);

        // Cache materials from scenes (or shaders) for fallback construction.
        if (_eraseScene != null)
        {
            try
            {
                Line2D probe = _eraseScene.Instantiate<Line2D>(PackedScene.GenEditState.Disabled);
                _eraseMaterialTemplate = probe.Material;
                probe.QueueFree();
            }
            catch
            {
                // ignore
            }
        }

        if (_drawScene != null)
        {
            try
            {
                Line2D probe = _drawScene.Instantiate<Line2D>(PackedScene.GenEditState.Disabled);
                _drawMaterialTemplate = probe.Material;
                probe.QueueFree();
            }
            catch
            {
                // ignore
            }
        }

        _drawTrail = TryLoadTex(DrawTrailPath);
        _eraseTrail = TryLoadTex(EraseTrailPath);

        if (_drawMaterialTemplate == null)
            _drawMaterialTemplate = BuildShaderMaterial(DrawShaderPath);
        if (_eraseMaterialTemplate == null)
            _eraseMaterialTemplate = BuildShaderMaterial(EraseShaderPath);

        if (_drawScene == null && _drawMaterialTemplate == null)
            MainFile.Logger.Warn("Map line draw assets missing — combat pen may look plain.");
        if (_eraseScene == null && _eraseMaterialTemplate == null)
            MainFile.Logger.Warn("Map line erase assets missing — combat eraser may leave residuals.");
        else
            MainFile.Logger.Info("Combat ink using map Line2D scenes/shaders (SubViewport half-res + PremultAlpha).");
    }

    private static PackedScene? TryLoadScene(string path)
    {
        try
        {
            if (ResourceLoader.Exists(path))
                return ResourceLoader.Load<PackedScene>(path);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Load scene {path}: {e.Message}");
        }

        return null;
    }

    private static Texture2D? TryLoadTex(string path)
    {
        try
        {
            if (ResourceLoader.Exists(path))
                return ResourceLoader.Load<Texture2D>(path);
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static Material? BuildShaderMaterial(string shaderPath)
    {
        try
        {
            if (!ResourceLoader.Exists(shaderPath))
                return null;
            var shader = ResourceLoader.Load<Shader>(shaderPath);
            if (shader == null)
                return null;
            return new ShaderMaterial { Shader = shader };
        }
        catch
        {
            return null;
        }
    }
}
