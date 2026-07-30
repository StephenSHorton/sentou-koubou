using Godot;

namespace BattleDraw;

/// <summary>
/// Paint-bucket fill of <b>enclosed</b> empty regions bounded by existing ink.
/// Rejects fills that touch the canvas border (would paint open space / whole screen).
/// </summary>
public static class FloodFill
{
    /// <summary>Max filled half-res pixels (~safety against huge accidental regions).</summary>
    public const int MaxFillPixels = 120_000;

    /// <summary>Alpha above this counts as a drawn wall.</summary>
    public const float WallAlpha = 0.12f;

    public sealed class Result
    {
        public required bool Ok { get; init; }
        public required string Reason { get; init; }
        /// <summary>Screen-space points densified as scanlines (for stroke commit).</summary>
        public required List<Vector2> ScreenPoints { get; init; }
        public int PixelCount { get; init; }
    }

    /// <param name="wallMask">Row-major width×height; true = ink / wall.</param>
    /// <param name="seedScreen">Click in full screen space (viewport coords).</param>
    /// <param name="brushWidth">Screen-space brush for scanline spacing.</param>
    public static Result TryFillEnclosed(
        bool[] wallMask,
        int width,
        int height,
        Vector2 seedScreen,
        float brushWidth,
        float resScale)
    {
        if (width <= 2 || height <= 2 || wallMask.Length < width * height)
        {
            return Fail("bad mask");
        }

        int sx = (int)Math.Floor(seedScreen.X * resScale);
        int sy = (int)Math.Floor(seedScreen.Y * resScale);
        if (sx < 0 || sy < 0 || sx >= width || sy >= height)
            return Fail("seed out of bounds");

        // If seed is on ink, search a small ring for empty interior.
        if (wallMask[sy * width + sx])
        {
            if (!TryFindNearbyEmpty(wallMask, width, height, sx, sy, out sx, out sy))
                return Fail("click is on a line (move inside a closed shape)");
        }

        // BFS flood; abort if we touch image border.
        var visited = new bool[width * height];
        var queue = new Queue<int>();
        var filled = new List<int>(4096);

        void Enqueue(int x, int y)
        {
            int i = y * width + x;
            if (visited[i] || wallMask[i])
                return;
            visited[i] = true;
            queue.Enqueue(i);
        }

        Enqueue(sx, sy);
        bool hitBorder = false;

        while (queue.Count > 0)
        {
            int i = queue.Dequeue();
            int x = i % width;
            int y = i / width;

            if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
            {
                hitBorder = true;
                break;
            }

            filled.Add(i);
            if (filled.Count > MaxFillPixels)
                return Fail("region too large (not a small enclosed shape)");

            Enqueue(x - 1, y);
            Enqueue(x + 1, y);
            Enqueue(x, y - 1);
            Enqueue(x, y + 1);
        }

        if (hitBorder)
            return Fail("not enclosed — fill would leak to open canvas");

        if (filled.Count < 4)
            return Fail("nothing to fill");

        // Horizontal scanline runs → densified screen points.
        filled.Sort(); // by index ≈ y then x
        float step = Math.Max(2f, brushWidth * 0.55f);
        float invScale = 1f / Math.Max(0.001f, resScale);
        var runs = new List<(int y, int x0, int x1)>();

        int runY = -1, runX0 = 0, runX1 = 0;
        foreach (int i in filled)
        {
            int x = i % width;
            int y = i / width;
            if (y != runY)
            {
                if (runY >= 0)
                    runs.Add((runY, runX0, runX1));
                runY = y;
                runX0 = x;
                runX1 = x;
            }
            else if (x == runX1 + 1)
            {
                runX1 = x;
            }
            else
            {
                runs.Add((runY, runX0, runX1));
                runX0 = x;
                runX1 = x;
            }
        }

        if (runY >= 0)
            runs.Add((runY, runX0, runX1));

        // Subsample scanlines by step in screen space.
        var points = new List<Vector2>(filled.Count / 2);
        int lastEmittedY = int.MinValue;
        int yStepHalf = Math.Max(1, (int)Math.Round(step * resScale));

        foreach ((int y, int x0, int x1) in runs)
        {
            if (lastEmittedY >= 0 && y - lastEmittedY < yStepHalf)
                continue;
            lastEmittedY = y;

            float syScreen = (y + 0.5f) * invScale;
            float x0s = x0 * invScale;
            float x1s = (x1 + 1) * invScale;
            // Horizontal densify
            float dist = Math.Abs(x1s - x0s);
            int n = Math.Max(2, (int)(dist / 4f) + 1);
            for (int k = 0; k < n; k++)
            {
                float t = n == 1 ? 0f : k / (float)(n - 1);
                points.Add(new Vector2(Mathf.Lerp(x0s, x1s, t), syScreen));
            }
        }

        if (points.Count < 2)
            return Fail("fill produced no stroke");

        return new Result
        {
            Ok = true,
            Reason = "ok",
            ScreenPoints = points,
            PixelCount = filled.Count,
        };
    }

    private static Result Fail(string reason) => new()
    {
        Ok = false,
        Reason = reason,
        ScreenPoints = [],
        PixelCount = 0,
    };

    private static bool TryFindNearbyEmpty(
        bool[] wall,
        int w,
        int h,
        int sx,
        int sy,
        out int ex,
        out int ey)
    {
        for (int r = 1; r <= 6; r++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Math.Abs(dx) != r && Math.Abs(dy) != r)
                        continue;
                    int x = sx + dx;
                    int y = sy + dy;
                    if (x <= 0 || y <= 0 || x >= w - 1 || y >= h - 1)
                        continue;
                    if (!wall[y * w + x])
                    {
                        ex = x;
                        ey = y;
                        return true;
                    }
                }
            }
        }

        ex = sx;
        ey = sy;
        return false;
    }

    /// <summary>1-pixel dilate so thin freehand gaps don't always leak.</summary>
    public static void DilateWalls(bool[] wall, int width, int height)
    {
        var src = (bool[])wall.Clone();
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int i = y * width + x;
                if (src[i])
                    continue;
                if (src[i - 1] || src[i + 1] || src[i - width] || src[i + width])
                    wall[i] = true;
            }
        }
    }

    public static bool[] BuildWallMask(Image? local, Image? remote, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (local == null && remote == null)
            return [];

        Image baseImg = local ?? remote!;
        width = baseImg.GetWidth();
        height = baseImg.GetHeight();
        var mask = new bool[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float a = 0f;
                if (local != null)
                    a = Math.Max(a, local.GetPixel(x, y).A);
                if (remote != null
                    && remote.GetWidth() == width
                    && remote.GetHeight() == height)
                    a = Math.Max(a, remote.GetPixel(x, y).A);
                mask[y * width + x] = a >= WallAlpha;
            }
        }

        return mask;
    }
}
