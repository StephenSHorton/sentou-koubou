using BaseLib.Config;
using Godot;

namespace CharacterCursors;

/// <summary>
/// BaseLib settings: pick a custom cursor tint or keep character NameColor.
/// </summary>
public sealed class CursorConfig : SimpleModConfig
{
    /// <summary>When true, use <see cref="CustomColor"/> instead of character NameColor.</summary>
    public static bool UseCustomColor { get; set; }

    /// <summary>Free-form cursor tint (HSV-friendly ColorPicker in settings).</summary>
    public static Color CustomColor { get; set; } = new(1f, 0.35f, 0.3f, 1f);

    /// <summary>Master toggle — disable to leave vanilla / other mods alone.</summary>
    public static bool EnableTint { get; set; } = true;

    public static event Action? SettingsChanged;

    /// <summary>Called by UI / settings after a static prop write if BaseLib does not fire.</summary>
    public static void NotifyChanged() => SettingsChanged?.Invoke();
}
