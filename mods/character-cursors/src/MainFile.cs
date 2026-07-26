using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace CharacterCursors;

/// <summary>
/// Tints STS2 cursors with each character's primary color (NameColor).
/// Local cursor: recolored Images via NCursorManager.OverrideCursor.
/// Remote cursors: desaturate+tint shader on the TextureRect (same idea as LemonSpire color tint).
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "CharacterCursors";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        var harmony = new Harmony(ModId);
        harmony.PatchAll();
        Logger.Info(
            "Character Cursors loaded — local + remote cursors tint to character NameColor. " +
            "If LemonSpire custom player colors are also enabled, that mod may override remote tints.");
    }
}
