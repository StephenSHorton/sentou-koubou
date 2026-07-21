using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;

namespace UncappedChapterFix;

/// <summary>
/// Host throws during remote relic AfterObtained when the peer sends an Index-typed
/// choice (e.g. skip/cancel encoded as indexes) to a path that expects DeckCard:
/// <c>Tried to get deck cards from player choice result of type Index!</c>
/// (seen on Claws → FromDeckForTransformation → AsDeckCards).
///
/// Soft fix: wrong-type AsDeckCards returns empty instead of throwing so the apply
/// path completes after the choice ID was already reserved (prevents nextChoiceId drift).
/// </summary>
public static class DeckChoiceHardenPatches
{
    public static bool TryApply(Harmony harmony)
    {
        MethodInfo? asDeckCards = AccessTools.Method(
            typeof(PlayerChoiceResult),
            nameof(PlayerChoiceResult.AsDeckCards));

        if (asDeckCards == null)
        {
            MainFile.Logger.Warn("PlayerChoiceResult.AsDeckCards not found — skip deck-choice harden.");
            return false;
        }

        harmony.Patch(
            asDeckCards,
            finalizer: new HarmonyMethod(typeof(DeckChoiceHardenPatches), nameof(AsDeckCardsFinalizer)));

        MainFile.Logger.Info(
            "Patched PlayerChoiceResult.AsDeckCards finalizer (wrong-type→empty under relic/deck select).");
        return true;
    }

    /// <summary>
    /// On type-mismatch (esp. Index skip/cancel), return empty cards and swallow.
    /// </summary>
    public static Exception? AsDeckCardsFinalizer(
        PlayerChoiceResult __instance,
        ref IEnumerable<CardModel> __result,
        Exception? __exception)
    {
        if (__exception is not InvalidOperationException)
            return __exception;

        string typeName = __instance.ChoiceType.ToString();

        // Index is never a valid DeckCard payload (skip/cancel or misrouted reward).
        // Other wrong types only soft-empty under relic/deck-select stacks.
        if (typeName is not "Index" && !StackLooksLikeRelicOrDeckSelect())
            return __exception;

        MainFile.Logger.Warn(
            $"AsDeckCards expected DeckCard but got {typeName} — " +
            "returning empty (prevents host choice-ID drift). " +
            $"Detail: {__exception.Message}");

        __result = Array.Empty<CardModel>();
        return null; // swallow
    }

    private static bool StackLooksLikeRelicOrDeckSelect()
    {
        var st = new StackTrace(1, fNeedFileInfo: false);
        foreach (StackFrame frame in st.GetFrames() ?? Array.Empty<StackFrame>())
        {
            Type? t = frame.GetMethod()?.DeclaringType;
            while (t != null)
            {
                string name = t.Name;
                if (name.Contains("AfterObtained", StringComparison.Ordinal))
                    return true;
                if (name.Contains("FromDeckFor", StringComparison.Ordinal))
                    return true;
                if (name.Contains("RelicReward", StringComparison.Ordinal))
                    return true;
                if (name.Contains("RelicCmd", StringComparison.Ordinal))
                    return true;
                // Async state machines: Claws+<AfterObtained>d__*, CardSelectCmd+<FromDeck...>d__*
                if (name.Contains("Claws", StringComparison.Ordinal))
                    return true;
                if (name.Contains("HeftyTablet", StringComparison.Ordinal))
                    return true;
                t = t.DeclaringType;
            }
        }

        return false;
    }
}
