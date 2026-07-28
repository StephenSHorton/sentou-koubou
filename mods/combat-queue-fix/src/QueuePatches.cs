using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Actions;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CombatQueueFix;

/// <summary>
/// Two layers:
/// 1) Before <see cref="ActionQueueSet.GetReadyAction"/>: cancel NonCombat heads while in combat
///    (so the existing Canceled-removal loop dequeues them).
/// 2) Prefix <see cref="ActionQueueSet.EnqueueWithoutSynchronizing"/>: refuse new NonCombat
///    enqueues while combat is active (blocks late-flushed map votes).
/// 3) Postfix <see cref="ActionQueueSet.CombatStarted"/>: sweep any NonCombat already queued.
/// </summary>
public static class QueuePatches
{
    [HarmonyPatch(typeof(ActionQueueSet), nameof(ActionQueueSet.GetReadyAction))]
    public static class GetReadyActionPatch
    {
        public static void Prefix(ActionQueueSet __instance)
        {
            DropBlockingNonCombat(__instance, "GetReady");
        }
    }

    [HarmonyPatch(typeof(ActionQueueSet), nameof(ActionQueueSet.EnqueueWithoutSynchronizing))]
    public static class EnqueuePatch
    {
        public static bool Prefix(ActionQueueSet __instance, GameAction gameAction)
        {
            try
            {
                if (!__instance._isInCombat || gameAction == null)
                    return true;

                if (gameAction.ActionType != GameActionType.NonCombat)
                    return true;

                // Map votes are the known softlock; also drop any other NonCombat that would
                // sit at the head and never run during combat.
                MainFile.Logger.Info(
                    $"Dropped NonCombat enqueue during combat: {gameAction.GetType().Name} " +
                    $"(owner={gameAction.OwnerId}).");
                try
                {
                    gameAction.Cancel();
                }
                catch
                {
                    // still refuse enqueue
                }

                return false;
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"Enqueue prefix failed: {e.Message}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(ActionQueueSet), nameof(ActionQueueSet.CombatStarted))]
    public static class CombatStartedPatch
    {
        public static void Postfix(ActionQueueSet __instance)
        {
            DropBlockingNonCombat(__instance, "CombatStarted");
        }
    }

    /// <summary>
    /// Cancel NonCombat actions that are waiting (not already executing) while combat is active.
    /// Vanilla removes Canceled heads at the start of GetReadyAction.
    /// </summary>
    private static void DropBlockingNonCombat(ActionQueueSet set, string reason)
    {
        try
        {
            if (set == null || !set._isInCombat)
                return;

            var queues = set._actionQueues;
            if (queues == null)
                return;

            int canceled = 0;
            foreach (var queue in queues)
            {
                if (queue?.actions == null || queue.actions.Count == 0)
                    continue;

                // Walk from front: cancel consecutive NonCombat waiters (map vote stacks).
                // Do not cancel something mid-execution.
                for (int i = 0; i < queue.actions.Count; i++)
                {
                    GameAction action = queue.actions[i];
                    if (action == null)
                        continue;
                    if (action.ActionType != GameActionType.NonCombat)
                        break;

                    var state = action.State;
                    if (state is GameActionState.Executing or GameActionState.GatheringPlayerChoice
                        or GameActionState.ReadyToResumeExecuting)
                    {
                        // Something odd is mid-flight — leave it alone.
                        break;
                    }

                    if (state == GameActionState.Canceled)
                        continue;

                    try
                    {
                        action.Cancel();
                        canceled++;
                        MainFile.Logger.Info(
                            $"[{reason}] Canceled NonCombat {action.GetType().Name} " +
                            $"for player {queue.ownerId} (was {state}).");
                    }
                    catch (Exception e)
                    {
                        MainFile.Logger.Warn(
                            $"[{reason}] Cancel failed for {action}: {e.Message}");
                    }
                }
            }

            if (canceled > 0)
                MainFile.Logger.Info($"[{reason}] Canceled {canceled} NonCombat action(s) during combat.");
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"DropBlockingNonCombat({reason}): {e.Message}");
        }
    }
}
