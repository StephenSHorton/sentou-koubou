using BaseLib.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace CharacterCursors;

/// <summary>
/// Tints STS2 cursors with each character's primary color (NameColor), or a player-picked color.
/// Local cursor: recolored Images via NCursorManager.OverrideCursor.
/// Remote cursors: desaturate+tint shader on the TextureRect (character color; LemonSpire may override).
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "CharacterCursors";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static CursorConfig Config { get; private set; } = null!;

    public static void Initialize()
    {
        Config = new CursorConfig();
        ModConfigRegistry.Register(ModId, Config);
        CursorConfig.SettingsChanged += () =>
        {
            CursorTint.ClearAppliedCache();
            CursorTint.ApplyLocalCursor();
        };

        var harmony = new Harmony(ModId);
        harmony.PatchAll();
        Logger.Info(
            "Character Cursors loaded — NameColor or custom tint (BaseLib settings + in-run " +
            "color chip bottom-left). Peers see your custom color via net sync.");
    }
}
