using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace UncappedChapterFix;

/// <summary>
/// Compat patches for UncappedSpire multiplayer chapter transitions and
/// host choice-ID drift during boss-reward relic AfterObtained paths.
/// Soft-depends on UncappedSpire — chapter patch no-ops if the type is missing.
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "UncappedChapterFix";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        var harmony = new Harmony(ModId);

        int applied = 0;
        applied += ClosingTheChapterPatches.TryApply(harmony) ? 1 : 0;
        applied += CardRewardChoicePatches.TryApply(harmony) ? 1 : 0;
        applied += DeckChoiceHardenPatches.TryApply(harmony) ? 1 : 0;
        applied += ChooseACardScreenSkipPatches.TryApply(harmony) ? 1 : 0;

        if (applied == 0)
        {
            Logger.Warn(
                "Uncapped Chapter Fix loaded but applied 0 patches " +
                "(API changed?). Safe to leave enabled.");
        }
        else
        {
            Logger.Info(
                $"Uncapped Chapter Fix loaded — {applied} patch group(s). " +
                "Finishes Closing the Chapter on all peers; hardens remote " +
                "card/relic choices (skip index + wrong-type DeckCard) to prevent " +
                "PlayerChoice ID drift after boss rewards.");
        }
    }
}
