using Godot;

namespace BattleDraw;

/// <summary>Polyline densification for combat shape tools (synced as normal strokes).</summary>
public static class ShapeGeometry
{
    public static bool IsShapeTool(DrawTool tool) => tool is
        DrawTool.Line or DrawTool.Rect or DrawTool.Ellipse
        or DrawTool.FillRect or DrawTool.FillEllipse or DrawTool.Stamp;

    public static bool IsFilledTool(DrawTool tool) => tool is
        DrawTool.FillRect or DrawTool.FillEllipse or DrawTool.Stamp;

    /// <summary>
    /// Sample points in screen space for the given tool between <paramref name="a"/> and <paramref name="b"/>.
    /// Filled shapes are densified as scanlines so existing stroke sync works.
    /// </summary>
    public static List<Vector2> BuildPoints(DrawTool tool, Vector2 a, Vector2 b, float brushWidth)
    {
        return tool switch
        {
            DrawTool.Line => BuildLine(a, b),
            DrawTool.Rect => BuildRectOutline(a, b),
            DrawTool.Ellipse => BuildEllipseOutline(a, b),
            DrawTool.FillRect => BuildFilledRect(a, b, brushWidth),
            DrawTool.FillEllipse => BuildFilledEllipse(a, b, brushWidth),
            DrawTool.Stamp => BuildStamp(a, brushWidth),
            _ => BuildLine(a, b),
        };
    }

    private static List<Vector2> BuildLine(Vector2 a, Vector2 b)
    {
        float dist = a.DistanceTo(b);
        int n = Math.Max(2, (int)(dist / 4f) + 1);
        var pts = new List<Vector2>(n);
        for (int i = 0; i < n; i++)
        {
            float t = n == 1 ? 0f : i / (float)(n - 1);
            pts.Add(a.Lerp(b, t));
        }

        return pts;
    }

    private static List<Vector2> BuildRectOutline(Vector2 a, Vector2 b)
    {
        float x0 = Math.Min(a.X, b.X);
        float x1 = Math.Max(a.X, b.X);
        float y0 = Math.Min(a.Y, b.Y);
        float y1 = Math.Max(a.Y, b.Y);
        var corners = new[]
        {
            new Vector2(x0, y0),
            new Vector2(x1, y0),
            new Vector2(x1, y1),
            new Vector2(x0, y1),
            new Vector2(x0, y0),
        };
        var pts = new List<Vector2>();
        for (int i = 0; i < corners.Length - 1; i++)
            pts.AddRange(BuildLine(corners[i], corners[i + 1]));
        return pts;
    }

    private static List<Vector2> BuildEllipseOutline(Vector2 a, Vector2 b, int segments = 48)
    {
        Vector2 c = (a + b) * 0.5f;
        float rx = Math.Abs(b.X - a.X) * 0.5f;
        float ry = Math.Abs(b.Y - a.Y) * 0.5f;
        rx = Math.Max(rx, 1f);
        ry = Math.Max(ry, 1f);
        var pts = new List<Vector2>(segments + 1);
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments * Mathf.Tau;
            pts.Add(new Vector2(c.X + Mathf.Cos(t) * rx, c.Y + Mathf.Sin(t) * ry));
        }

        return pts;
    }

    private static List<Vector2> BuildFilledRect(Vector2 a, Vector2 b, float brushWidth)
    {
        float x0 = Math.Min(a.X, b.X);
        float x1 = Math.Max(a.X, b.X);
        float y0 = Math.Min(a.Y, b.Y);
        float y1 = Math.Max(a.Y, b.Y);
        float step = Math.Max(2f, brushWidth * 0.55f);
        var pts = new List<Vector2>();
        // Outline first for a clean edge
        pts.AddRange(BuildRectOutline(a, b));
        for (float y = y0; y <= y1; y += step)
        {
            pts.AddRange(BuildLine(new Vector2(x0, y), new Vector2(x1, y)));
        }

        return pts;
    }

    private static List<Vector2> BuildFilledEllipse(Vector2 a, Vector2 b, float brushWidth)
    {
        Vector2 c = (a + b) * 0.5f;
        float rx = Math.Max(1f, Math.Abs(b.X - a.X) * 0.5f);
        float ry = Math.Max(1f, Math.Abs(b.Y - a.Y) * 0.5f);
        float step = Math.Max(2f, brushWidth * 0.55f);
        var pts = new List<Vector2>();
        pts.AddRange(BuildEllipseOutline(a, b));
        for (float y = c.Y - ry; y <= c.Y + ry; y += step)
        {
            float dy = (y - c.Y) / ry;
            float half = rx * Mathf.Sqrt(Math.Max(0f, 1f - dy * dy));
            if (half < 0.5f)
                continue;
            pts.AddRange(BuildLine(new Vector2(c.X - half, y), new Vector2(c.X + half, y)));
        }

        return pts;
    }

    private static List<Vector2> BuildStamp(Vector2 center, float brushWidth)
    {
        float r = Math.Max(4f, brushWidth * 2.2f);
        var a = center - new Vector2(r, r);
        var b = center + new Vector2(r, r);
        return BuildFilledEllipse(a, b, brushWidth);
    }
}
