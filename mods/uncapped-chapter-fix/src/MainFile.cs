using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace UncappedChapterFix;

/// <summary>
/// Compat patches for UncappedSpire multiplayer:
/// chapter transitions, host choice-ID drift during boss-reward relic AfterObtained,
/// PlayerRngSet Seed UInt64→uint crash on MP embark, and ChapterChange seed rehash
/// (GetDeterministicHashCode UInt64→int) that hangs "Through the Mysterious Door".
/// Soft-depends on UncappedSpire — no-op if types are missing.
/// Must load after UncappedSpire so broken patches can be replaced.
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
        applied += PlayerRngSeedCompatPatches.TryApply(harmony) ? 1 : 0;
        applied += ChapterSeedCompatPatches.TryApply(harmony) ? 1 : 0;

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
                "Chapter finish; choice harden; PlayerRngSet Seed uint; " +
                "ChapterChange GetDeterministicHashCode int (Mysterious Door).");
        }
    }
}
