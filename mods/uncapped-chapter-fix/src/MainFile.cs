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
        applied += SafeApply("ClosingTheChapter", () => ClosingTheChapterPatches.TryApply(harmony));
        applied += SafeApply("CardRewardChoice", () => CardRewardChoicePatches.TryApply(harmony));
        applied += SafeApply("DeckChoiceHarden", () => DeckChoiceHardenPatches.TryApply(harmony));
        applied += SafeApply("ChooseACardScreenSkip", () => ChooseACardScreenSkipPatches.TryApply(harmony));
        applied += SafeApply("PlayerRngSeed", () => PlayerRngSeedCompatPatches.TryApply(harmony));
        // Must not patch DoSeedChange directly (Harmony IL read throws MissingMethodException
        // on the dead UInt64 GetDeterministicHashCode token). Callers are patched instead.
        applied += SafeApply("ChapterSeed", () => ChapterSeedCompatPatches.TryApply(harmony));

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
                "ChapterChange seed via DoLocalSeedChange/HandleChapterChangeMessage " +
                "(Mysterious Door, bypass UInt64 hash).");
        }
    }

    private static int SafeApply(string name, Func<bool> apply)
    {
        try
        {
            return apply() ? 1 : 0;
        }
        catch (Exception e)
        {
            Logger.Error($"Patch group '{name}' failed (continuing): {e}");
            return 0;
        }
    }
}
