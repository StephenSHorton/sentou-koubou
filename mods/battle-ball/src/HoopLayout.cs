using System.Text.Json;
using Godot;

namespace BattleBall;

/// <summary>
/// Tunable hoop collision / placement. Loaded from hoop_layout.json next to the DLL
/// (edit with tools/hoop-calibrator.html). Fractions are of sprite half-width / half-height.
/// </summary>
public sealed class HoopLayout
{
    public float HoopAboveFloor { get; set; } = 300f;
    /// <summary>Rim center X offset from sprite center, as fraction of half-width (negative = left when FlipH).</summary>
    public float RimOffsetX { get; set; } = -0.42f;
    public float RimOffsetY { get; set; } = 0.18f;
    /// <summary>Half-width of the open mouth (fraction of half-width). Ball passes between side posts.</summary>
    public float OpeningHalfW { get; set; } = 0.40f;
    /// <summary>
    /// Rotation of the rim opening in degrees (0 = horizontal posts left/right of center).
    /// </summary>
    public float RimAngleDeg { get; set; } = 0f;
    /// <summary>Collision radius of each rim side post (fraction of half-width).</summary>
    public float SideRadius { get; set; } = 0.13f;

    /// <summary>
    /// Backboard bounce line — just a segment the ball rebounds from (not a filled box).
    /// Endpoints are offsets from sprite center, as fractions of half-width / half-height.
    /// </summary>
    public float BoardAX { get; set; } = 0.12f;
    public float BoardAY { get; set; } = -0.55f;
    public float BoardBX { get; set; } = 0.12f;
    public float BoardBY { get; set; } = 0.40f;

    public static HoopLayout Instance { get; private set; } = Load();

    public static HoopLayout Load()
    {
        var layout = new HoopLayout();
        try
        {
            string dir = Path.GetDirectoryName(typeof(MainFile).Assembly.Location)!;
            string path = Path.Combine(dir, "hoop_layout.json");
            if (!File.Exists(path))
            {
                MainFile.Logger.Info("hoop_layout.json missing — using built-in defaults.");
                Instance = layout;
                return layout;
            }

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement r = doc.RootElement;
            if (r.TryGetProperty("hoopAboveFloor", out var v)) layout.HoopAboveFloor = v.GetSingle();
            if (r.TryGetProperty("rimOffsetX", out v)) layout.RimOffsetX = v.GetSingle();
            if (r.TryGetProperty("rimOffsetY", out v)) layout.RimOffsetY = v.GetSingle();
            if (r.TryGetProperty("openingHalfW", out v)) layout.OpeningHalfW = v.GetSingle();
            if (r.TryGetProperty("rimAngleDeg", out v)) layout.RimAngleDeg = v.GetSingle();
            if (r.TryGetProperty("sideRadius", out v)) layout.SideRadius = v.GetSingle();

            // New line endpoints
            if (r.TryGetProperty("boardAX", out v)) layout.BoardAX = v.GetSingle();
            if (r.TryGetProperty("boardAY", out v)) layout.BoardAY = v.GetSingle();
            if (r.TryGetProperty("boardBX", out v)) layout.BoardBX = v.GetSingle();
            if (r.TryGetProperty("boardBY", out v)) layout.BoardBY = v.GetSingle();

            // Legacy rect → approximate a vertical line down the left edge of the old box
            if (!r.TryGetProperty("boardAX", out _) && r.TryGetProperty("boardLeft", out var bl))
            {
                float left = bl.GetSingle();
                float top = r.TryGetProperty("boardTop", out var bt) ? bt.GetSingle() : -0.9f;
                float h = r.TryGetProperty("boardH", out var bh) ? bh.GetSingle() : 1.55f;
                layout.BoardAX = left;
                layout.BoardAY = top;
                layout.BoardBX = left;
                layout.BoardBY = top + h;
                MainFile.Logger.Info("hoop_layout: converted legacy board rect → bounce line.");
            }

            MainFile.Logger.Info($"hoop_layout.json loaded from {path}");
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"hoop_layout.json load failed: {e.Message}");
        }

        Instance = layout;
        return layout;
    }
}
