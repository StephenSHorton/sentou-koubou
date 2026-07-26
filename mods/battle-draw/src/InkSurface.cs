using Godot;

namespace BattleDraw;

/// <summary>
/// Half-resolution baked ink (map-style): at most two TextureRects (local + remote)
/// instead of unbounded antialiased Line2D nodes. Stamping is O(brush footprint).
/// </summary>
public sealed class InkSurface
{
    /// <summary>Ink resolution as fraction of viewport (0.5 = half-res like vanilla map).</summary>
    public const float ResScale = 0.5f;

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

    /// <summary>Stamp a poly-line (screen-space points) into local or remote layer.</summary>
    public void StampPolyline(IReadOnlyList<Vector2> screenPoints, Color color, float widthScreen, bool remote)
    {
        if (screenPoints.Count == 0 || _localImg == null || _remoteImg == null)
            return;

        Image img = remote ? _remoteImg! : _localImg!;
        float r = Math.Max(0.75f, widthScreen * ResScale * 0.5f);
        Color ink = color;
        if (ink.A < 0.5f)
            ink.A = 1f;

        if (screenPoints.Count == 1)
        {
            StampCircle(img, ToInk(screenPoints[0]), r, ink, erase: false);
        }
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
        float r = Math.Max(0.75f, widthScreen * ResScale * 0.5f);
        Color ink = color;
        if (ink.A < 0.5f) ink.A = 1f;
        StampSegment(img, ToInk(a), ToInk(b), r, ink, erase: false);
        if (remote) _remoteDirty = true;
        else _localDirty = true;
        Flush();
    }

    /// <summary>Soft eraser: clear alpha in a circle (both layers — eraser removes everyone's ink).</summary>
    public void EraseCircleScreen(Vector2 center, float radiusScreen)
    {
        if (_localImg == null || _remoteImg == null)
            return;
        Vector2 c = ToInk(center);
        float r = Math.Max(1f, radiusScreen * ResScale);
        StampCircle(_localImg, c, r, Colors.Transparent, erase: true);
        StampCircle(_remoteImg, c, r, Colors.Transparent, erase: true);
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
        int steps = Math.Max(1, (int)Math.Ceiling(dist / Math.Max(0.5f, radius * 0.45f)));
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            StampCircle(img, a.Lerp(b, t), radius, color, erase);
        }
    }

    private static void StampCircle(Image img, Vector2 center, float radius, Color color, bool erase)
    {
        int w = img.GetWidth();
        int h = img.GetHeight();
        int r = Math.Max(1, (int)Math.Ceiling(radius));
        int cx = (int)Math.Round(center.X);
        int cy = (int)Math.Round(center.Y);
        float r2 = radius * radius;

        int x0 = Math.Max(0, cx - r - 1);
        int y0 = Math.Max(0, cy - r - 1);
        int x1 = Math.Min(w - 1, cx + r + 1);
        int y1 = Math.Min(h - 1, cy + r + 1);

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float dx = x + 0.5f - center.X;
                float dy = y + 0.5f - center.Y;
                float d2 = dx * dx + dy * dy;
                if (d2 > r2)
                    continue;

                if (erase)
                {
                    // Soft edge erase
                    float edge = 1f - Math.Clamp(MathF.Sqrt(d2) / radius, 0f, 1f);
                    Color prev = img.GetPixel(x, y);
                    prev.A *= 1f - edge;
                    if (prev.A < 0.02f)
                        prev = Colors.Transparent;
                    img.SetPixel(x, y, prev);
                }
                else
                {
                    // Soft edge alpha
                    float edge = 1f - Math.Clamp(MathF.Sqrt(d2) / radius, 0f, 1f);
                    Color src = color;
                    src.A *= 0.35f + 0.65f * edge;
                    Color dst = img.GetPixel(x, y);
                    img.SetPixel(x, y, AlphaOver(dst, src));
                }
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
