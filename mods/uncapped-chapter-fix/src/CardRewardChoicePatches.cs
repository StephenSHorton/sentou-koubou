using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions;

namespace UncappedChapterFix;

/// <summary>
/// During the same desync session we saw:
/// <c>Tried to get index from player choice result of type DeckCard!</c>
/// inside CardReward.OnSelect when applying a remote peer's choice.
/// That throws on the host, skips applying the reward, and drifts
/// PlayerChoiceSynchronizer next-choice IDs (host vs client off-by-one).
///
/// Soft fix: if AsIndexOrNull throws a type-mismatch while CardReward is on the
/// stack, return null so the reward path does not explode mid-sync.
/// </summary>
public static class CardRewardChoicePatches
{
    public static bool TryApply(Harmony harmony)
    {
        MethodInfo? asIndex = AccessTools.Method(
            typeof(PlayerChoiceResult),
            nameof(PlayerChoiceResult.AsIndexOrNull));

        if (asIndex == null)
        {
            MainFile.Logger.Warn("PlayerChoiceResult.AsIndexOrNull not found — skip card-reward harden.");
            return false;
        }

        harmony.Patch(
            asIndex,
            finalizer: new HarmonyMethod(typeof(CardRewardChoicePatches), nameof(AsIndexFinalizer)));

        MainFile.Logger.Info(
            "Patched PlayerChoiceResult.AsIndexOrNull finalizer (wrong-type→null under CardReward).");
        return true;
    }

    /// <summary>
    /// Harmony finalizer: on type-mismatch under CardReward, swallow and return null.
    /// </summary>
    public static Exception? AsIndexFinalizer(
        PlayerChoiceResult __instance,
        ref int? __result,
        Exception? __exception)
    {
        if (__exception is not InvalidOperationException)
            return __exception;

        // Avoid hard-coding PlayerChoiceType enum namespace (moved across game builds).
        string typeName = __instance.ChoiceType.ToString();
        if (typeName is "Index")
            return __exception;

        if (!StackHasCardReward())
            return __exception;

        MainFile.Logger.Warn(
            $"CardReward expected Index choice but got {typeName} — " +
            "returning null instead of throwing (prevents host choice-ID drift). " +
            $"Detail: {__exception.Message}");
        __result = null;
        return null; // swallow
    }

    private static bool StackHasCardReward()
    {
        var st = new StackTrace(1, fNeedFileInfo: false);
        foreach (StackFrame frame in st.GetFrames() ?? Array.Empty<StackFrame>())
        {
            Type? t = frame.GetMethod()?.DeclaringType;
            while (t != null)
            {
                string name = t.Name;
                // Async state machines: CardReward+<OnSelect>d__49
                if (name.Contains("CardReward", StringComparison.Ordinal))
                    return true;
                t = t.DeclaringType;
            }
        }

        return false;
    }
}
