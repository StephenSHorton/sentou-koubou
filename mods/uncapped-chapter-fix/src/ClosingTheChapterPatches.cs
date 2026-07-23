using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace UncappedChapterFix;

/// <summary>
/// UncappedSpire <c>ClosingTheChapter.StartANewChapter</c> early-returns for non-local
/// owners without finishing the shared event instance. EventSynchronizer then warns
/// "not yet finished" when Neow begins after the chapter transition, and peers can desync.
///
/// Fix: after StartANewChapter runs (for every player instance of the shared event),
/// force the event model finished + cleanup.
/// </summary>
public static class ClosingTheChapterPatches
{
    private const string ClosingTypeName =
        "UncappedSpire.UncappedSpireCode.UncappedActs.ClosingTheChapter";

    private static readonly FieldInfo? IsFinishedField =
        AccessTools.Field(typeof(EventModel), "_isFinished");

    private static readonly FieldInfo? CurrentOptionsField =
        AccessTools.Field(typeof(EventModel), "_currentOptions");

    public static bool TryApply(Harmony harmony)
    {
        Type? closingType = AccessTools.TypeByName(ClosingTypeName);
        if (closingType == null)
        {
            MainFile.Logger.Info(
                "UncappedSpire ClosingTheChapter type not found — chapter finish patch skipped.");
            return false;
        }

        MethodInfo? start = AccessTools.Method(closingType, "StartANewChapter");
        if (start == null)
        {
            MainFile.Logger.Warn(
                "Found ClosingTheChapter but not StartANewChapter — API changed?");
            return false;
        }

        harmony.Patch(
            start,
            postfix: new HarmonyMethod(typeof(ClosingTheChapterPatches), nameof(Postfix)));

        MainFile.Logger.Info(
            "Patched ClosingTheChapter.StartANewChapter → always finish event instance.");
        return true;
    }

    /// <summary>
    /// Runs for every shared-event player instance (host and clients).
    /// Safe if the original already finished the event.
    /// </summary>
    public static void Postfix(object __instance)
    {
        if (__instance is not EventModel eventModel)
            return;

        try
        {
            if (eventModel.IsFinished)
                return;

            // IsFinished has a private setter with AssertMutable; set the backing field.
            IsFinishedField?.SetValue(eventModel, true);

            // Clear remaining options so UI/sync state matches a finished event.
            if (CurrentOptionsField?.GetValue(eventModel) is System.Collections.IList options)
                options.Clear();

            eventModel.EnsureCleanup();

            MainFile.Logger.Info(
                $"Finished ClosingTheChapter for player {eventModel.Owner?.NetId} " +
                "(compat: prevent unfinished shared event before next act/Neow).");
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Failed to finish ClosingTheChapter: {e.Message}");
        }
    }
}
