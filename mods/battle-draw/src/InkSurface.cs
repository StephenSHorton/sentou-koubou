using Godot;

namespace BattleDraw;

/// <summary>
/// Near-full-res baked ink: two TextureRects (local + remote). Hard stamps + hard erase
/// (no soft residual alpha). Slightly denser than half-res to reduce blur when scaling width.
/// </summary>
public sealed class InkSurface
{
    /// <summary>Ink resolution vs viewport. 0.75 balances sharpness vs cost.</summary>
    public const float ResScale = 0.75f;

    private Image? _localImg;
    private Image? _remoteImg;
    private ImageTexture? _localTex;
    private ImageTexture? _remoteTex;
    private TextureRect? _localRect;
    private TextureRect? _remoteRect;
    private Vector2I _inkSize;
    private Vector2 _viewportSize = new(1920, 1080);
    private bool _localDirty;
    private bool _remoteDirty;

    public Node2D? Root { get; private set; }

    public void Attach(Node2D parent)
    {
        Root = parent;
        _localRect = MakeRect("LocalInk");
        _remoteRect = MakeRect("RemoteInk");
        parent.AddChild(_remoteRect);
        parent.AddChild(_localRect);
        EnsureSize(new Vector2(1920, 1080));
    }

    private static TextureRect MakeRect(string name) => new()
    {
        Name = name,
        MouseFilter = Control.MouseFilterEnum.Ignore,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.Scale,
        // Linear is fine at 0.75×; hard stamps avoid mushy edges.
        TextureFilter = CanvasItem.TextureFilterEnum.Linear,
    };

    public void EnsureSize(Vector2 viewportSize)
    {
        _viewportSize = viewportSize;
        int w = Math.Max(64, (int)(viewportSize.X * ResScale));
        int h = Math.Max(64, (int)(viewportSize.Y * ResScale));
        if (_inkSize.X == w && _inkSize.Y == h && _localImg != null)
            return;

        _inkSize = new Vector2I(w, h);
        _localImg = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        _remoteImg = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        _localImg.Fill(Colors.Transparent);
        _remoteImg.Fill(Colors.Transparent);
        _localTex = ImageTexture.CreateFromImage(_localImg);
        _remoteTex = ImageTexture.CreateFromImage(_remoteImg);

        if (_localRect != null)
        {
            _localRect.Texture = _localTex;
            _localRect.Position = Vector2.Zero;
            _localRect.Size = viewportSize;
        }

        if (_remoteRect != null)
        {
            _remoteRect.Texture = _remoteTex;
            _remoteRect.Position = Vector2.Zero;
            _remoteRect.Size = viewportSize;
        }

        _localDirty = _remoteDirty = false;
    }

    public void SetRemoteVisible(bool visible)
    {
        if (_remoteRect != null && GodotObject.IsInstanceValid(_remoteRect))
            _remoteRect.Visible = visible;
    }

    public void ClearLocal()
    {
        _localImg?.Fill(Colors.Transparent);
        _localDirty = true;
        Flush();
    }

    public void ClearRemote()
    {
        _remoteImg?.Fill(Colors.Transparent);
        _remoteDirty = true;
        Flush();
    }

    public void ClearAll()
    {
        ClearLocal();
        ClearRemote();
    }

    public void StampPolyline(IReadOnlyList<Vector2> screenPoints, Color color, float widthScreen, bool remote)
    {
        if (screenPoints.Count == 0 || _localImg == null || _remoteImg == null)
            return;

        Image img = remote ? _remoteImg! : _localImg!;
        float r = Math.Max(0.85f, widthScreen * ResScale * 0.5f);
        Color ink = color;
        if (ink.A < 0.85f)
            ink.A = 1f;

        if (screenPoints.Count == 1)
            StampDisk(img, ToInk(screenPoints[0]), r, ink, erase: false);
        else
        {
            for (int i = 1; i < screenPoints.Count; i++)
                StampSegment(img, ToInk(screenPoints[i - 1]), ToInk(screenPoints[i]), r, ink, erase: false);
        }

        if (remote) _remoteDirty = true;
        else _localDirty = true;
        Flush();
    }

    public void StampSegmentScreen(Vector2 a, Vector2 b, Color color, float widthScreen, bool remote)
    {
        if (_localImg == null || _remoteImg == null)
            return;
        Image img = remote ? _remoteImg! : _localImg!;
        float r = Math.Max(0.85f, widthScreen * ResScale * 0.5f);
        Color ink = color;
        if (ink.A < 0.85f) ink.A = 1f;
        StampSegment(img, ToInk(a), ToInk(b), r, ink, erase: false);
        if (remote) _remoteDirty = true;
        else _localDirty = true;
        Flush();
    }

    /// <summary>Full-strength eraser (hard disk) — no residual dim ink.</summary>
    public void EraseCircleScreen(Vector2 center, float radiusScreen)
    {
        if (_localImg == null || _remoteImg == null)
            return;
        Vector2 c = ToInk(center);
        // Match pen weight: radius is half-width of eraser in screen space.
        float r = Math.Max(1.25f, radiusScreen * ResScale);
        StampDisk(_localImg, c, r, Colors.Transparent, erase: true);
        StampDisk(_remoteImg, c, r, Colors.Transparent, erase: true);
        _localDirty = _remoteDirty = true;
        Flush();
    }

    public void Flush()
    {
        if (_localDirty && _localImg != null && _localTex != null)
        {
            _localTex.Update(_localImg);
            _localDirty = false;
        }

        if (_remoteDirty && _remoteImg != null && _remoteTex != null)
        {
            _remoteTex.Update(_remoteImg);
            _remoteDirty = false;
        }
    }

    private Vector2 ToInk(Vector2 screen)
    {
        if (_viewportSize.X < 1f || _viewportSize.Y < 1f)
            return screen * ResScale;
        return new Vector2(
            screen.X / _viewportSize.X * _inkSize.X,
            screen.Y / _viewportSize.Y * _inkSize.Y);
    }

    private static void StampSegment(Image img, Vector2 a, Vector2 b, float radius, Color color, bool erase)
    {
        float dist = a.DistanceTo(b);
        // Dense stamps so large brushes don't look gappy/blurry.
        float step = Math.Max(0.35f, radius * 0.28f);
        int steps = Math.Max(1, (int)Math.Ceiling(dist / step));
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            StampDisk(img, a.Lerp(b, t), radius, color, erase);
        }
    }

    /// <summary>
    /// Hard disk stamp. Draw: solid coverage with a tiny 1px anti-alias rim.
    /// Erase: full clear inside radius (no soft residual).
    /// </summary>
    private static void StampDisk(Image img, Vector2 center, float radius, Color color, bool erase)
    {
        int w = img.GetWidth();
        int h = img.GetHeight();
        int rCeil = Math.Max(1, (int)Math.Ceiling(radius + 1f));
        float r2 = radius * radius;
        // AA rim only for draw, ~0.75 ink px
        float aa = erase ? 0f : 0.75f;
        float rOuter = radius + aa;
        float rOuter2 = rOuter * rOuter;

        int x0 = Math.Max(0, (int)Math.Floor(center.X - rOuter) - 1);
        int y0 = Math.Max(0, (int)Math.Floor(center.Y - rOuter) - 1);
        int x1 = Math.Min(w - 1, (int)Math.Ceiling(center.X + rOuter) + 1);
        int y1 = Math.Min(h - 1, (int)Math.Ceiling(center.Y + rOuter) + 1);

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float dx = x + 0.5f - center.X;
                float dy = y + 0.5f - center.Y;
                float d2 = dx * dx + dy * dy;
                if (d2 > rOuter2)
                    continue;

                if (erase)
                {
                    // Full weight erase — anything under the disk is gone.
                    if (d2 <= r2)
                        img.SetPixel(x, y, Colors.Transparent);
                    continue;
                }

                float cover = 1f;
                if (d2 > r2 && aa > 0f)
                {
                    float d = MathF.Sqrt(d2);
                    cover = 1f - Math.Clamp((d - radius) / aa, 0f, 1f);
                }

                if (cover <= 0.01f)
                    continue;

                Color src = color;
                src.A = Math.Clamp(color.A * cover, 0f, 1f);
                // Prefer solid overwrite when fully covered so strokes stay opaque at any size.
                if (src.A >= 0.98f)
                    img.SetPixel(x, y, new Color(color.R, color.G, color.B, 1f));
                else
                    img.SetPixel(x, y, AlphaOver(img.GetPixel(x, y), src));
            }
        }
    }

    private static Color AlphaOver(Color dst, Color src)
    {
        float a = src.A + dst.A * (1f - src.A);
        if (a < 1e-5f)
            return Colors.Transparent;
        float r = (src.R * src.A + dst.R * dst.A * (1f - src.A)) / a;
        float g = (src.G * src.A + dst.G * dst.A * (1f - src.A)) / a;
        float b = (src.B * src.A + dst.B * dst.A * (1f - src.A)) / a;
        return new Color(r, g, b, a);
    }
}
