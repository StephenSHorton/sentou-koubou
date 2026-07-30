using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace MpDropOut;

/// <summary>
/// Actively advances waits when a peer drops so remaining players are not stuck mid-gate.
/// Host enqueues combat end-turn (synced); wait patches also treat disconnected as satisfied.
/// </summary>
public static class DropOutService
{
    /// <summary>
    /// Called on every peer when the run lobby reports a remote disconnect.
    /// </summary>
    public static void OnRemoteDisconnected(ulong playerId)
    {
        try
        {
            RunState? state = DropOutUtil.State;
            if (state == null)
            {
                MainFile.Logger.Info($"Drop-out ignored (no run): {playerId}");
                return;
            }

            Player? player = state.GetPlayer(playerId);
            if (player == null)
            {
                MainFile.Logger.Warn($"Drop-out: unknown player id {playerId}");
                return;
            }

            MainFile.Logger.Info(
                $"Peer dropped out: {playerId} ({player.Character?.Id}). " +
                "Advancing combat/map/event/act/treasure waits for remaining players.");

            AdvanceCombat(player);
            TryNudgeSharedGates(player);
            NudgeWaitingGates();
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"OnRemoteDisconnected failed: {e}");
        }
    }

    private static void AdvanceCombat(Player player)
    {
        CombatManager combat = CombatManager.Instance;
        if (combat == null || !combat.IsInProgress)
            return;

        if (combat.IsPlayerReadyToEndTurn(player))
        {
            TryMarkReadyToBeginEnemyTurn(player);
            return;
        }

        RunManager run = RunManager.Instance;
        if (run == null)
            return;

        if (run.NetService.Type == NetGameType.Host)
        {
            int turn = player.PlayerCombatState?.TurnNumber ?? 0;
            try
            {
                run.ActionQueueSynchronizer.RequestEnqueue(
                    new EndPlayerTurnAction(player, turn));
                MainFile.Logger.Info(
                    $"Host enqueued EndPlayerTurnAction for dropped player {player.NetId} turn {turn}");
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn(
                    $"Host enqueue EndPlayerTurn failed ({e.Message}); falling back to local EndTurn");
                PlayerCmd.EndTurn(player, canBackOut: false);
            }
        }
        else
        {
            PlayerCmd.EndTurn(player, canBackOut: false);
            MainFile.Logger.Info($"Client local EndTurn for dropped player {player.NetId}");
        }

        TryMarkReadyToBeginEnemyTurn(player);
    }

    private static void TryMarkReadyToBeginEnemyTurn(Player player)
    {
        try
        {
            CombatManager.Instance.SetReadyToBeginEnemyTurn(player);
        }
        catch (Exception e)
        {
            MainFile.Logger.Debug(
                $"SetReadyToBeginEnemyTurn skip for {player.NetId}: {e.Message}");
        }
    }

    private static void TryNudgeSharedGates(Player dropped)
    {
        try
        {
            RunManager.Instance.ActChangeSynchronizer?.OnPlayerReady(dropped);
        }
        catch (Exception e)
        {
            MainFile.Logger.Debug($"ActChange nudge: {e.Message}");
        }
    }

    private static void NudgeWaitingGates()
    {
        try
        {
            MapSelectionSynchronizer? map = RunManager.Instance?.MapSelectionSynchronizer;
            if (map != null)
                Patches.MapVotePatch.TryHostMoveIfConnectedVotesComplete(map);
        }
        catch (Exception e)
        {
            MainFile.Logger.Debug($"Map nudge: {e.Message}");
        }

        try
        {
            EventSynchronizer? ev = RunManager.Instance?.EventSynchronizer;
            if (ev != null)
            {
                uint page = (uint)HarmonyLib.AccessTools
                    .Field(typeof(EventSynchronizer), "_pageIndex")
                    .GetValue(ev)!;
                Patches.EventVotePatch.TryHostChooseIfConnectedVotesComplete(ev, page);
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Debug($"Event nudge: {e.Message}");
        }

        try
        {
            TreasureRoomRelicSynchronizer? treasure =
                RunManager.Instance?.TreasureRoomRelicSynchronizer;
            if (treasure != null)
                Patches.TreasureOnPickedPatch.TryAwardIfConnectedComplete(treasure);
        }
        catch (Exception e)
        {
            MainFile.Logger.Debug($"Treasure nudge: {e.Message}");
        }
    }
}
