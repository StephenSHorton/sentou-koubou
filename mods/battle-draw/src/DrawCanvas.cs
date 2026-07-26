using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace BattleDraw;

/// <summary>
/// Combat doodle surface — event-driven <see cref="_Input"/> (zero idle ticks).
/// Ink path matches vanilla map drawing: half-res SubViewport + Line2D pen/eraser
/// (subtractive eraser material), PremultAlpha composite.
/// Cursor matches map: native quill / eraser via <see cref="NCursorManager"/>.
/// </summary>
public partial class DrawCanvas : Control
{
    public static DrawCanvas? Instance { get; private set; }

    private const float MinPointDistance = 2f; // map uses DistanceSquared < 4 → dist 2
    public const float HandNoDrawBandFrac = 0.30f;

    private readonly InkSurface _ink = new();
    private readonly Dictionary<(ulong Owner, int Id), Line2D> _openRemote = new();

    private Line2D? _activeLine;
    private Vector2 _lastPoint;
    private Color _activeColor = Colors.White;
    private float _activeWidth = 3.5f;
    private bool _drawing;
    private DrawTool _tool = DrawTool.None;
    private DrawTool _strokeTool;
    private int _activeStrokeId;
    private bool _cursorOverridden;
    private DrawTool _cursorTool = DrawTool.None;
    private int _strokeLogBudget = 3;

    public bool HideRemoteStrokes { get; private set; }

    public static void AttachTo(NCombatRoom room)
    {
        Instance?.Teardown();
        BrushToolbar.DetachCombat();

        var layer = new CanvasLayer
        {
            Name = "BattleDrawUiLayer",
            Layer = 100,
            ProcessMode = ProcessModeEnum.Inherit,
        };
        room.AddChild(layer);

        var inkRoot = new Node2D
        {
            Name = "BattleDrawInk",
            ZIndex = 0,
            ProcessMode = ProcessModeEnum.Inherit,
        };
        layer.AddChild(inkRoot);

        var canvas = new DrawCanvas
        {
            Name = "BattleDrawCanvas",
            MouseFilter = MouseFilterEnum.Ignore,
            ProcessMode = ProcessModeEnum.Inherit,
            CustomMinimumSize = Vector2.Zero,
        };
        layer.AddChild(canvas);
        canvas.SetProcess(false);
        canvas.SetProcessInput(true);
        canvas._ink.Attach(inkRoot);

        Viewport? vp = room.GetViewport();
        if (vp != null)
            canvas._ink.EnsureSize(vp.GetVisibleRect().Size);

        BrushToolbar.AttachCombat(layer);

        Instance = canvas;
        MainFile.Logger.Info(
            "Battle Draw surface ready — map quill/eraser cursor + SubViewport ink (v0.6.3).");
    }

    public override void _Input(InputEvent e)
    {
        // Combat canvas uses global _Input. If it keeps listening while the map is open
        // (or combat room is hidden), RMB draws a second screen-space stroke that does not
        // scroll with the map — ghost duplicate of the real map mark.
        if (!IsCombatDrawingActive())
        {
            if (_drawing)
                EndStroke();
            return;
        }

        switch (e)
        {
            case InputEventKey { Pressed: true, Echo: false } key:
                HandleHotkey(key.Keycode);
                return;
            case InputEventMouseButton mb:
                HandleMouseButton(mb);
                return;
            case InputEventMouseMotion mm:
                if (!_drawing)
                {
                    MouseButtonMask mask = mm.ButtonMask;
                    if (mask == 0)
                        return;
                    if (mask == MouseButtonMask.Left && _tool == DrawTool.None)
                        return;
                    if (IntentFromMask(mask) == null)
                        return;
                }

                HandleMouseMotion(mm);
                return;
        }
    }

    /// <summary>
    /// Only draw in combat when the combat room is the live surface and the map is not up.
    /// </summary>
    private bool IsCombatDrawingActive()
    {
        if (!IsInsideTree() || !GodotObject.IsInstanceValid(this))
            return false;

        try
        {
            NMapScreen? map = NMapScreen.Instance;
            if (map != null && GodotObject.IsInstanceValid(map)
                && map.IsVisibleInTree() && map.Visible)
                return false;
        }
        catch
        {
            // ignore map probe failures
        }

        try
        {
            NCombatRoom? room = NCombatRoom.Instance;
            if (room == null || !GodotObject.IsInstanceValid(room) || !room.IsInsideTree())
                return false;
            if (!room.IsVisibleInTree())
                return false;
        }
        catch
        {
            return false;
        }

        // Parent layer must still be live (not mid-teardown).
        Node? parent = GetParent();
        return parent != null && GodotObject.IsInstanceValid(parent) && parent.IsInsideTree();
    }

    private void HandleHotkey(Key key)
    {
        switch (key)
        {
            case Key.Bracketleft:
                BrushConfig.NudgeSize(-0.5f);
                BrushToolbar.SyncAllSizeSliders();
                break;
            case Key.Bracketright:
                BrushConfig.NudgeSize(0.5f);
                BrushToolbar.SyncAllSizeSliders();
                break;
            case Key.B:
                BrushToolbar.CombatInstance?.SetTool(DrawTool.Brush);
                break;
            // No click-arm eraser (E): MMB always erases; armed LMB eraser removed.
        }
    }

    private void HandleMouseButton(InputEventMouseButton mb)
    {
        Viewport? vp = GetViewport();
        if (vp == null)
            return;

        if (!mb.Pressed)
        {
            if (mb.ButtonIndex is MouseButton.Left or MouseButton.Right or MouseButton.Middle)
            {
                if (_drawing)
                    EndStroke();
            }

            return;
        }

        DrawTool? intent = IntentFromButton(mb.ButtonIndex);
        if (intent == null)
            return;

        if (GetBlockReason() != null)
        {
            if (_drawing)
                EndStroke();
            return;
        }

        StartStroke(intent.Value, GetInkMousePos(vp));
    }

    private void HandleMouseMotion(InputEventMouseMotion mm)
    {
        Viewport? vp = GetViewport();
        if (vp == null)
            return;

        DrawTool? intent = IntentFromMask(mm.ButtonMask);
        if (intent == null)
        {
            if (_drawing)
                EndStroke();
            return;
        }

        if (GetBlockReason() != null)
        {
            if (_drawing)
                EndStroke();
            return;
        }

        Vector2 pos = GetInkMousePos(vp);
        if (!_drawing || _strokeTool != intent.Value)
            StartStroke(intent.Value, pos);
        else
            ContinueStroke(pos);
    }

    private DrawTool? IntentFromButton(MouseButton button) => button switch
    {
        MouseButton.Middle => DrawTool.Eraser,
        MouseButton.Right => DrawTool.Brush,
        // LMB only arms pen (optional). Eraser is MMB-only — no click-to-arm erase tool.
        MouseButton.Left when _tool == DrawTool.Brush => DrawTool.Brush,
        _ => null,
    };

    private DrawTool? IntentFromMask(MouseButtonMask mask)
    {
        if ((mask & MouseButtonMask.Middle) != 0)
            return DrawTool.Eraser;
        if ((mask & MouseButtonMask.Right) != 0)
            return DrawTool.Brush;
        if ((mask & MouseButtonMask.Left) != 0 && _tool == DrawTool.Brush)
            return DrawTool.Brush;
        return null;
    }

    private static Vector2 GetInkMousePos(Viewport vp)
    {
        Rect2 vis = vp.GetVisibleRect();
        return vp.GetMousePosition() - vis.Position;
    }

    public void OnToolChanged(DrawTool tool)
    {
        _tool = tool;
        EndStroke();
        RefreshCursor();
    }

    /// <summary>
    /// Armed tool cursor when idle; active stroke tool (RMB pen / MMB erase) while drawing.
    /// </summary>
    public void RefreshCursor()
    {
        DrawTool shown = _drawing ? _strokeTool : _tool;
        ApplyCursorForTool(shown);
    }

    public void SetHideRemoteStrokes(bool hide)
    {
        HideRemoteStrokes = hide;
        _ink.SetRemoteVisible(!hide);
        MainFile.Logger.Info(hide ? "Hiding peer combat doodles." : "Showing peer combat doodles.");
    }

    private void ContinueStroke(Vector2 local)
    {
        Viewport? vp = GetViewport();
        if (vp != null)
            _ink.EnsureSize(vp.GetVisibleRect().Size);

        if (_activeLine == null || !GodotObject.IsInstanceValid(_activeLine))
            return;

        // Map: DistanceSquaredTo < 4 → min distance 2
        if (_lastPoint.DistanceSquaredTo(local) < MinPointDistance * MinPointDistance)
            return;

        _lastPoint = local;
        _ink.AddPointScreen(_activeLine, local);
        DrawSync.Instance?.SendPoint(_activeStrokeId, local);
    }

    private void StartStroke(DrawTool tool, Vector2 localPos)
    {
        Viewport? vp = GetViewport();
        if (vp != null)
            _ink.EnsureSize(vp.GetVisibleRect().Size);

        // End any previous stroke cleanly (tool switch mid-drag).
        if (_drawing)
            EndStroke();

        _drawing = true;
        _strokeTool = tool;
        RefreshCursor();

        bool erase = tool == DrawTool.Eraser;
        _activeStrokeId = DrawSync.Instance?.AllocStrokeId() ?? (int)Time.GetTicksMsec();
        _activeColor = erase ? Colors.White : MakeInkColor(BrushConfig.CurrentColor);
        _activeWidth = Math.Max(2f, BrushConfig.ClampedSize);
        _lastPoint = localPos;

        _activeLine = _ink.BeginStroke(erase, _activeColor, _activeWidth, remote: false);
        _ink.SeedStroke(_activeLine, localPos);

        DrawSync.Instance?.SendBegin(_activeStrokeId, localPos, _activeColor, _activeWidth, erase);

        if (_strokeLogBudget > 0)
        {
            _strokeLogBudget--;
            MainFile.Logger.Info(
                $"Stroke begin id={_activeStrokeId} tool={tool} w={_activeWidth:0.#}");
        }
    }

    private void EndStroke()
    {
        if (_drawing && _activeStrokeId != 0)
            DrawSync.Instance?.SendEnd(_activeStrokeId);

        // Leave Line2D in the SubViewport (map keeps finished strokes as nodes).
        _activeLine = null;
        _drawing = false;
        _activeStrokeId = 0;
        _strokeTool = DrawTool.None;
        _ink.EndStrokeActivity();
        RefreshCursor();
    }

    public void ClearAll(bool network = true)
    {
        _activeLine = null;
        _openRemote.Clear();
        _ink.ClearAll();
        _drawing = false;
        _activeStrokeId = 0;
        _strokeTool = DrawTool.None;
        if (network)
            DrawSync.Instance?.SendClear();
    }

    public void Teardown()
    {
        SetProcess(false);
        SetProcessInput(false);
        ClearAll(network: false);
        RestoreCursor();
        if (GodotObject.IsInstanceValid(this) && IsInsideTree())
        {
            Node? uiLayer = GetParent();
            if (uiLayer != null && uiLayer.Name == "BattleDrawUiLayer")
                uiLayer.QueueFree();
            else
                QueueFree();
        }

        if (Instance == this)
            Instance = null;
    }

    // --- multiplayer: reconstruct peer strokes as map-style Line2Ds ---

    public void RemoteBegin(ulong ownerId, int strokeId, Vector2 pos, Color color, float width, bool erase)
    {
        FreeRemoteLine(ownerId, strokeId);

        Color ink = erase ? Colors.White : MakeInkColor(color);
        float w = Math.Max(2f, width);
        Line2D line = _ink.BeginStroke(erase, ink, w, remote: true);
        _ink.SeedStroke(line, pos);
        _openRemote[(ownerId, strokeId)] = line;
    }

    public void RemotePoint(ulong ownerId, int strokeId, Vector2 pos)
    {
        if (!_openRemote.TryGetValue((ownerId, strokeId), out Line2D? line)
            || line == null
            || !GodotObject.IsInstanceValid(line))
        {
            // Unreliable mid-stream: start a thin continuation stroke so ink still appears.
            line = _ink.BeginStroke(erase: false, Colors.White, 3f, remote: true);
            _ink.SeedStroke(line, pos);
            _openRemote[(ownerId, strokeId)] = line;
            return;
        }

        _ink.AddPointScreen(line, pos);
    }

    public void RemoteEnd(ulong ownerId, int strokeId)
    {
        _openRemote.Remove((ownerId, strokeId));
        _ink.EndStrokeActivity();
    }

    public void RemoteClear()
    {
        _openRemote.Clear();
        _ink.ClearRemote();
    }

    private void FreeRemoteLine(ulong ownerId, int strokeId)
    {
        if (!_openRemote.Remove((ownerId, strokeId), out Line2D? line))
            return;
        if (line != null && GodotObject.IsInstanceValid(line))
            line.QueueFree();
    }

    private static Color MakeInkColor(Color c)
    {
        if (c.A < 0.5f)
            c.A = 1f;
        return c;
    }

    private string? GetBlockReason()
    {
        if (BrushToolbar.HitsPointer())
            return "toolbar";
        if (IsCardInteractionBusy())
            return "card-busy";

        Viewport? vp = GetViewport();
        if (vp == null)
            return null;

        Vector2 inkPos = GetInkMousePos(vp);
        Vector2 size = vp.GetVisibleRect().Size;
        float handTop = size.Y * (1f - HandNoDrawBandFrac);
        if (inkPos.Y >= handTop)
            return "hand-band";
        return null;
    }

    private static bool IsCardInteractionBusy()
    {
        try
        {
            NPlayerHand? hand = NCombatRoom.Instance?.Ui?.Hand;
            if (hand == null || !GodotObject.IsInstanceValid(hand))
                return false;
            if (hand.HasDraggedHolder)
                return true;
            if (hand.InCardPlay)
                return true;
        }
        catch
        {
            // ignore
        }

        return false;
    }

    /// <summary>
    /// Same path as <c>NMapDrawings.UpdateLocalCursor</c>: game cursor manager + native
    /// quill/eraser images (tilted while mouse down). <see cref="Input.SetCustomMouseCursor"/>
    /// alone is overwritten by STS2's cursor system every frame.
    /// </summary>
    private void ApplyCursorForTool(DrawTool tool)
    {
        if (tool == _cursorTool && _cursorOverridden && tool != DrawTool.None)
            return;
        if (tool == DrawTool.None && !_cursorOverridden)
            return;

        try
        {
            NCursorManager? cm = NGame.Instance?.CursorManager;
            if (cm != null && GodotObject.IsInstanceValid(cm))
            {
                if (tool == DrawTool.Brush)
                {
                    Image? upright = LoadCursorImage(NMapDrawings.drawingCursorPath);
                    Image? tilted = LoadCursorImage(NMapDrawings.drawingCursorTiltedPath);
                    if (upright != null && tilted != null)
                    {
                        cm.OverrideCursor(tilted, upright, NMapDrawings.drawingCursorHotspot);
                        _cursorOverridden = true;
                        _cursorTool = DrawTool.Brush;
                        return;
                    }
                }
                else if (tool == DrawTool.Eraser)
                {
                    Image? upright = LoadCursorImage(NMapDrawings.erasingCursorPath);
                    Image? tilted = LoadCursorImage(NMapDrawings.erasingCursorTiltedPath);
                    if (upright != null && tilted != null)
                    {
                        cm.OverrideCursor(tilted, upright, NMapDrawings.erasingCursorHotspot);
                        _cursorOverridden = true;
                        _cursorTool = DrawTool.Eraser;
                        return;
                    }
                }
                else
                {
                    cm.StopOverridingCursor();
                    _cursorOverridden = false;
                    _cursorTool = DrawTool.None;
                    return;
                }
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Map-style cursor override failed: {e.Message}");
        }

        // Fallback if NCursorManager / preload unavailable.
        ApplyCursorFallback(tool);
    }

    private void ApplyCursorFallback(DrawTool tool)
    {
        Texture2D? tex = tool switch
        {
            DrawTool.Brush => LoadTex(NMapDrawings.drawingCursorPath)
                             ?? LoadTex(NMapDrawings.drawingCursorTiltedPath),
            DrawTool.Eraser => LoadTex(NMapDrawings.erasingCursorPath)
                              ?? LoadTex(NMapDrawings.erasingCursorTiltedPath),
            _ => null,
        };
        if (tex == null)
        {
            RestoreCursor();
            return;
        }

        Vector2 hot = tool == DrawTool.Eraser
            ? NMapDrawings.erasingCursorHotspot
            : NMapDrawings.drawingCursorHotspot;
        Input.SetCustomMouseCursor(tex, Input.CursorShape.Arrow, hot);
        _cursorOverridden = true;
        _cursorTool = tool;
    }

    private void RestoreCursor()
    {
        if (!_cursorOverridden && _cursorTool == DrawTool.None)
            return;
        try
        {
            NGame.Instance?.CursorManager?.StopOverridingCursor();
        }
        catch
        {
            // ignore
        }

        try
        {
            Input.SetCustomMouseCursor(null);
        }
        catch
        {
            // ignore
        }

        _cursorOverridden = false;
        _cursorTool = DrawTool.None;
    }

    private static Image? LoadCursorImage(string path)
    {
        try
        {
            // Prefer the same preload cache the map uses.
            Image? fromCache = PreloadManager.Cache?.GetAsset<Image>(path);
            if (fromCache != null)
                return fromCache;
        }
        catch
        {
            // fall through
        }

        try
        {
            if (ResourceLoader.Exists(path))
            {
                // Some assets load as Texture2D; convert to Image for OverrideCursor.
                var img = ResourceLoader.Load<Image>(path);
                if (img != null)
                    return img;
                var tex = ResourceLoader.Load<Texture2D>(path);
                return tex?.GetImage();
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static Texture2D? LoadTex(string path)
    {
        try
        {
            return ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
        }
        catch
        {
            return null;
        }
    }
}
