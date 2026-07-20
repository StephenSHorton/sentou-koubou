using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace UncappedChapterFix;

/// <summary>
/// Compat patches for UncappedSpire multiplayer chapter transitions.
/// Soft-depends on UncappedSpire — patches no-op if the type is missing.
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

        if (applied == 0)
        {
            Logger.Warn(
                "Uncapped Chapter Fix loaded but applied 0 patches " +
                "(UncappedSpire missing or API changed). Safe to leave enabled.");
        }
        else
        {
            Logger.Info(
                $"Uncapped Chapter Fix loaded — {applied} patch group(s). " +
                "Finishes Closing the Chapter on all peers; hardens remote card-reward choices.");
        }
    }
}
