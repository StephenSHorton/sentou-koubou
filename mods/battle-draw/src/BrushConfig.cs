using BaseLib.Config;
using Godot;

namespace BattleDraw;

/// <summary>
/// Shared brush settings for combat doodles and vanilla map drawing.
/// BaseLib settings UI lists static props; the combat palette uses a free ColorPicker
/// that writes <see cref="CustomColor"/>.
/// </summary>
public sealed class BrushConfig : SimpleModConfig
{
    /// <summary>Stroke thickness in pixels (combat + map Line2D width).</summary>
    public static float BrushSize { get; set; } = 3.5f;

    private static BrushColorPreset _colorPreset = BrushColorPreset.Yellow;

    /// <summary>Fallback preset when not using the free color picker.</summary>
    public static BrushColorPreset ColorPreset
    {
        get => _colorPreset;
        set
        {
            _colorPreset = value;
            // Settings menu presets take effect again after picking a free color.
            UseCustomColor = false;
        }
    }

    /// <summary>Free-form color from the ColorPicker (preferred while UseCustomColor).</summary>
    public static Color CustomColor { get; set; } = new(1f, 0.92f, 0.35f, 0.9f);

    public static bool UseCustomColor { get; set; } = true;

    public static float ClampedSize => Math.Clamp(BrushSize, 1f, 24f);

    public static Color CurrentColor => UseCustomColor ? CustomColor : ColorOf(ColorPreset);

    /// <summary>Raised when color or size changes (map + combat toolbars listen).</summary>
    public static event Action? SettingsChanged;

    public static void SetColor(Color c)
    {
        CustomColor = c;
        UseCustomColor = true;
        SettingsChanged?.Invoke();
    }

    public static void SetSize(float size)
    {
        BrushSize = Math.Clamp(size, 1f, 24f);
        SettingsChanged?.Invoke();
    }

    public static Color ColorOf(BrushColorPreset preset) => preset switch
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

    public static void NudgeSize(float delta)
    {
        SetSize(BrushSize + delta);
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
