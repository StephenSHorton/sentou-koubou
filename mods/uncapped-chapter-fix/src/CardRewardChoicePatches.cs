using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions;

namespace UncappedChapterFix;

/// <summary>
/// Softens <see cref="PlayerChoiceResult.AsIndexOrNull"/> when a peer sends a
/// non-Index payload (e.g. DeckCard) where an index was expected — common under
/// CardReward and relic AfterObtained apply paths.
///
/// Returning null avoids throwing after a choice ID was reserved (nextChoiceId drift).
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
            MainFile.Logger.Warn("PlayerChoiceResult.AsIndexOrNull not found — skip index-choice harden.");
            return false;
        }

        harmony.Patch(
            asIndex,
            finalizer: new HarmonyMethod(typeof(CardRewardChoicePatches), nameof(AsIndexFinalizer)));

        MainFile.Logger.Info(
            "Patched PlayerChoiceResult.AsIndexOrNull finalizer (wrong-type→null under reward/relic apply).");
        return true;
    }

    /// <summary>
    /// Harmony finalizer: on type-mismatch under reward/relic/card-select, swallow and return null.
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

        if (!StackIsSoftHardenContext())
            return __exception;

        MainFile.Logger.Warn(
            $"AsIndexOrNull expected Index but got {typeName} — " +
            "returning null instead of throwing (prevents host choice-ID drift). " +
            $"Detail: {__exception.Message}");
        __result = null;
        return null; // swallow
    }

    private static bool StackIsSoftHardenContext()
    {
        var st = new StackTrace(1, fNeedFileInfo: false);
        foreach (StackFrame frame in st.GetFrames() ?? Array.Empty<StackFrame>())
        {
            Type? t = frame.GetMethod()?.DeclaringType;
            while (t != null)
            {
                string name = t.Name;
                // Async state machines: CardReward+<OnSelect>d__49, Claws+<AfterObtained>d__*, etc.
                if (name.Contains("CardReward", StringComparison.Ordinal))
                    return true;
                if (name.Contains("AfterObtained", StringComparison.Ordinal))
                    return true;
                if (name.Contains("RelicReward", StringComparison.Ordinal))
                    return true;
                if (name.Contains("FromChooseACardScreen", StringComparison.Ordinal))
                    return true;
                if (name.Contains("FromDeckFor", StringComparison.Ordinal))
                    return true;
                if (name.Contains("FromSimpleGrid", StringComparison.Ordinal))
                    return true;
                t = t.DeclaringType;
            }
        }

        return false;
    }
}
