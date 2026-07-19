using BaseLib.Config;
using Godot;

namespace BattleDraw;

/// <summary>
/// Shared brush settings for combat doodles and vanilla map drawing.
/// BaseLib only lists <b>static</b> properties in the mod settings UI.
/// </summary>
public sealed class BrushConfig : SimpleModConfig
{
    /// <summary>Stroke thickness in pixels (combat + map Line2D width).</summary>
    public static float BrushSize { get; set; } = 3.5f;

    /// <summary>Preset ink color for your local brush.</summary>
    public static BrushColorPreset ColorPreset { get; set; } = BrushColorPreset.Yellow;

    public static float ClampedSize => Math.Clamp(BrushSize, 1f, 24f);

    public static Color CurrentColor => ColorPreset switch
    {
        BrushColorPreset.Red => new Color(1f, 0.25f, 0.22f, 0.9f),
        BrushColorPreset.Orange => new Color(1f, 0.55f, 0.15f, 0.9f),
        BrushColorPreset.Yellow => new Color(1f, 0.92f, 0.35f, 0.9f),
        BrushColorPreset.Green => new Color(0.35f, 0.95f, 0.4f, 0.9f),
        BrushColorPreset.Cyan => new Color(0.3f, 0.9f, 1f, 0.9f),
        BrushColorPreset.Blue => new Color(0.35f, 0.55f, 1f, 0.9f),
        BrushColorPreset.Purple => new Color(0.75f, 0.4f, 1f, 0.9f),
        BrushColorPreset.Pink => new Color(1f, 0.45f, 0.75f, 0.9f),
        BrushColorPreset.White => new Color(1f, 1f, 1f, 0.92f),
        BrushColorPreset.Black => new Color(0.08f, 0.08f, 0.1f, 0.92f),
        _ => new Color(1f, 0.92f, 0.35f, 0.9f),
    };

    /// <summary>Cycle presets with hotkeys without opening settings.</summary>
    public static void CycleColor(int delta)
    {
        int n = Enum.GetValues<BrushColorPreset>().Length;
        int i = ((int)ColorPreset + delta) % n;
        if (i < 0)
            i += n;
        ColorPreset = (BrushColorPreset)i;
    }

    public static void NudgeSize(float delta)
    {
        BrushSize = Math.Clamp(BrushSize + delta, 1f, 24f);
    }
}

public enum BrushColorPreset
{
    Yellow = 0,
    Red = 1,
    Orange = 2,
    Green = 3,
    Cyan = 4,
    Blue = 5,
    Purple = 6,
    Pink = 7,
    White = 8,
    Black = 9,
}
