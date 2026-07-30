using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace MpDropOut;

/// <summary>
/// Harmony patches: react to disconnect, and treat disconnected peers as non-participants
/// for shared multiplayer waits (end turn, map, event, act, treasure).
/// </summary>
public static class Patches
{
    // ── Disconnect / host migration ───────────────────────────────────────

    [HarmonyPatch(typeof(RunManager), "RemotePlayerDisconnected")]
    public static class RunManagerRemoteDisconnectPatch
    {
        public static void Postfix(ulong playerId) =>
            DropOutService.OnRemoteDisconnected(playerId);
    }

    /// <summary>
    /// When we lose the host connection, try host migration instead of kicking to main menu.
    /// </summary>
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.LocalPlayerDisconnected))]
    public static class LocalPlayerDisconnectedPatch
    {
        public static bool Prefix(RunManager __instance, NetErrorInfo info)
        {
            try
            {
                HostMigration.NoteHostFromService(__instance.NetService);

                // Still clear peer-input for everyone else (vanilla does this).
                if (__instance.State != null)
                {
                    foreach (Player player in __instance.State.Players)
                    {
                        if (!MegaCrit.Sts2.Core.Context.LocalContext.IsMe(player))
                            __instance.InputSynchronizer.OnPlayerDisconnected(player.NetId);
                    }
                }

                if (HostMigration.TryBeginOnLocalDisconnect(info))
                {
                    MainFile.Logger.Info(
                        "Suppressed return-to-menu; host migration in progress.");
                    return false; // skip vanilla ReturnToMainMenuWithError
                }

                // Fall through to vanilla for abandon / no remaining players / host side.
                return true;
            }
            catch (Exception e)
            {
                MainFile.Logger.Error($"LocalPlayerDisconnected patch failed: {e}");
                return true;
            }
        }
    }

    /// <summary>Keep host id fresh while the session is healthy.</summary>
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.NRun), "_Process")]
    public static class NoteHostIdPatch
    {
        private static int _counter;

        public static void Postfix()
        {
            // Throttle: every ~60 frames
            if ((++_counter % 60) != 0)
                return;
            try
            {
                HostMigration.NoteHostFromService(RunManager.Instance?.NetService);
            }
            catch
            {
                // ignore
            }
        }
    }

    // ── Combat: all ready to end turn ─────────────────────────────────────

    [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.AllPlayersReadyToEndTurn))]
    public static class AllPlayersReadyToEndTurnPatch
    {
        public static bool Prefix(CombatManager __instance, ref bool __result)
        {
            try
            {
                if (RunManager.Instance.IsSingleplayerOrFakeMultiplayer)
                {
                    __result = true;
                    return false;
                }

                CombatState? state = __instance._state;
                if (state == null)
                {
                    __result = false;
                    return false;
                }

                HashSet<Player> ready = __instance._playersReadyToEndTurn;
                int participants = 0;
                foreach (Player player in state.Players)
                {
                    if (!DropOutUtil.IsParticipating(player))
                        continue;
                    participants++;
                    if (ready.Contains(player) || player.Creature.IsDead)
                        continue;
                    __result = false;
                    return false;
                }

                __result = participants > 0 && state.CurrentSide == CombatSide.Player;
                return false;
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"AllPlayersReadyToEndTurn patch failed: {e.Message}");
                return true;
            }
        }
    }

    // ── Combat: ready to begin enemy turn ─────────────────────────────────

    [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.SetReadyToBeginEnemyTurn))]
    public static class SetReadyToBeginEnemyTurnPatch
    {
        public static bool Prefix(
            CombatManager __instance,
            Player player,
            Func<Task>? actionDuringEnemyTurn)
        {
            try
            {
                if (!__instance.IsInProgress)
                    MainFile.Logger.Error(
                        "Trying to set player ready to begin enemy turn, but combat is over!");

                bool proceed;
                using (__instance._playerReadyLock.EnterScope())
                {
                    __instance._playersReadyToBeginEnemyTurn.Add(player);
                    proceed = ParticipantsReadyToBeginEnemyTurn(__instance)
                              && __instance._state?.CurrentSide == CombatSide.Player;
                }

                if (proceed
                    || RunManager.Instance.NetService.Type == NetGameType.Singleplayer)
                {
                    MethodInfo? after = AccessTools.Method(
                        typeof(CombatManager),
                        "AfterAllPlayersReadyToBeginEnemyTurn");
                    if (after?.Invoke(__instance, [actionDuringEnemyTurn]) is Task task)
                        MegaCrit.Sts2.Core.Helpers.TaskHelper.RunSafely(task);
                }

                return false;
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"SetReadyToBeginEnemyTurn patch failed: {e.Message}");
                return true;
            }
        }

        private static bool ParticipantsReadyToBeginEnemyTurn(CombatManager combat)
        {
            CombatState? state = combat._state;
            if (state == null)
                return false;

            HashSet<Player> ready = combat._playersReadyToBeginEnemyTurn;
            int participants = 0;
            foreach (Player p in state.Players)
            {
                if (!DropOutUtil.IsParticipating(p))
                    continue;
                participants++;
                if (ready.Contains(p) || p.Creature.IsDead)
                    continue;
                return false;
            }

            return participants > 0;
        }
    }

    // ── Map votes ─────────────────────────────────────────────────────────

    [HarmonyPatch(
        typeof(MapSelectionSynchronizer),
        nameof(MapSelectionSynchronizer.PlayerVotedForMapCoord))]
    public static class MapVotePatch
    {
        public static void Postfix(MapSelectionSynchronizer __instance)
        {
            try
            {
                TryHostMoveIfConnectedVotesComplete(__instance);
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"MapVotePatch: {e.Message}");
            }
        }

        public static void TryHostMoveIfConnectedVotesComplete(MapSelectionSynchronizer sync)
        {
            if (RunManager.Instance.NetService.Type == NetGameType.Client)
                return;

            RunState? state = RunManager.Instance.State;
            if (state == null)
                return;

            List<MapVote?> votes = sync._votes;
            int gen = sync.MapGenerationCount;

            bool anyParticipant = false;
            foreach (Player p in state.Players)
            {
                if (!DropOutUtil.IsParticipating(p))
                    continue;
                anyParticipant = true;
                int slot = state.GetPlayerSlotIndex(p);
                if (slot < 0 || slot >= votes.Count)
                    return;
                MapVote? v = votes[slot];
                if (!v.HasValue || v.Value.mapGenerationCount != gen)
                    return;
            }

            if (!anyParticipant)
                return;

            bool vanillaBlocked = false;
            for (int i = 0; i < votes.Count; i++)
            {
                MapVote? v = votes[i];
                if (!v.HasValue || v.Value.mapGenerationCount != gen)
                {
                    vanillaBlocked = true;
                    break;
                }
            }

            if (!vanillaBlocked)
                return;

            MapVote? sample = null;
            foreach (Player p in DropOutUtil.ParticipatingPlayers(state))
            {
                int slot = state.GetPlayerSlotIndex(p);
                if (slot >= 0 && slot < votes.Count && votes[slot].HasValue)
                {
                    sample = votes[slot];
                    break;
                }
            }

            if (!sample.HasValue)
                return;

            for (int i = 0; i < votes.Count && i < state.Players.Count; i++)
            {
                if (!DropOutUtil.IsParticipating(state.Players[i]))
                    votes[i] = sample;
            }

            MethodInfo? move = AccessTools.Method(
                typeof(MapSelectionSynchronizer),
                "MoveToMapCoord");
            if (move == null)
                return;

            MainFile.Logger.Info(
                "Map votes complete among connected players (ignoring leavers); host moving.");
            move.Invoke(sync, null);
        }
    }

    // ── Shared event votes ────────────────────────────────────────────────

    [HarmonyPatch(typeof(EventSynchronizer), "PlayerVotedForSharedOptionIndex")]
    public static class EventVotePatch
    {
        public static void Postfix(EventSynchronizer __instance, uint pageIndex)
        {
            try
            {
                TryHostChooseIfConnectedVotesComplete(__instance, pageIndex);
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"EventVotePatch: {e.Message}");
            }
        }

        public static void TryHostChooseIfConnectedVotesComplete(
            EventSynchronizer sync,
            uint pageIndex)
        {
            if (RunManager.Instance.NetService.Type == NetGameType.Client)
                return;

            uint currentPage = (uint)AccessTools.Field(typeof(EventSynchronizer), "_pageIndex")
                .GetValue(sync)!;
            if (pageIndex != currentPage)
                return;

            IPlayerCollection players = (IPlayerCollection)AccessTools
                .Field(typeof(EventSynchronizer), "_playerCollection")
                .GetValue(sync)!;
            List<uint?> votes = (List<uint?>)AccessTools
                .Field(typeof(EventSynchronizer), "_playerVotes")
                .GetValue(sync)!;

            bool any = false;
            foreach (Player p in players.Players)
            {
                if (!DropOutUtil.IsParticipating(p))
                    continue;
                any = true;
                int slot = players.GetPlayerSlotIndex(p);
                if (slot < 0 || slot >= votes.Count || !votes[slot].HasValue)
                    return;
            }

            if (!any)
                return;

            bool vanillaBlocked = votes.Any(v => !v.HasValue);
            if (!vanillaBlocked)
                return;

            uint? sample = null;
            foreach (Player p in players.Players)
            {
                if (!DropOutUtil.IsParticipating(p))
                    continue;
                int slot = players.GetPlayerSlotIndex(p);
                if (slot >= 0 && slot < votes.Count && votes[slot].HasValue)
                {
                    sample = votes[slot];
                    break;
                }
            }

            if (!sample.HasValue)
                return;

            for (int i = 0; i < votes.Count && i < players.Players.Count; i++)
            {
                if (!DropOutUtil.IsParticipating(players.Players[i]))
                    votes[i] = sample;
            }

            MethodInfo? choose = AccessTools.Method(
                typeof(EventSynchronizer),
                "ChooseSharedEventOption");
            if (choose == null)
                return;

            MainFile.Logger.Info(
                "Shared event votes complete among connected players; host choosing option.");
            choose.Invoke(sync, null);
        }
    }

    // ── Act change ────────────────────────────────────────────────────────

    [HarmonyPatch(typeof(ActChangeSynchronizer), nameof(ActChangeSynchronizer.OnPlayerReady))]
    public static class ActChangeReadyPatch
    {
        public static bool Prefix(ActChangeSynchronizer __instance, Player player)
        {
            try
            {
                MainFile.Logger.Debug($"Player {player.NetId} ready to move to next act");
                int slot = __instance._runState.GetPlayerSlotIndex(player);
                __instance._readyPlayers[slot] = true;

                if (AllParticipatingActReady(__instance))
                {
                    AccessTools.Method(typeof(ActChangeSynchronizer), "MoveToNextAct")
                        ?.Invoke(__instance, null);
                }

                return false;
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"ActChangeReadyPatch: {e.Message}");
                return true;
            }
        }

        private static bool AllParticipatingActReady(ActChangeSynchronizer sync)
        {
            RunState state = sync._runState;
            int n = 0;
            for (int i = 0; i < state.Players.Count; i++)
            {
                if (!DropOutUtil.IsParticipating(state.Players[i]))
                    continue;
                n++;
                if (!sync._readyPlayers[i])
                    return false;
            }

            return n > 0;
        }
    }

    [HarmonyPatch(
        typeof(ActChangeSynchronizer),
        nameof(ActChangeSynchronizer.IsWaitingForOtherPlayers))]
    public static class ActChangeWaitingPatch
    {
        public static bool Prefix(ActChangeSynchronizer __instance, ref bool __result)
        {
            try
            {
                RunState state = __instance._runState;
                ulong me = MegaCrit.Sts2.Core.Context.LocalContext.NetId
                           ?? throw new InvalidOperationException("No local net id");
                int mySlot = state.GetPlayerSlotIndex(me);
                for (int i = 0; i < __instance._readyPlayers.Count; i++)
                {
                    if (i == mySlot)
                        continue;
                    if (i < state.Players.Count
                        && !DropOutUtil.IsParticipating(state.Players[i]))
                        continue;
                    if (!__instance._readyPlayers[i])
                    {
                        __result = true;
                        return false;
                    }
                }

                __result = false;
                return false;
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"ActChangeWaitingPatch: {e.Message}");
                return true;
            }
        }
    }

    // ── Treasure relic picks ──────────────────────────────────────────────

    [HarmonyPatch(typeof(TreasureRoomRelicSynchronizer), nameof(TreasureRoomRelicSynchronizer.OnPicked))]
    public static class TreasureOnPickedPatch
    {
        public static void Postfix(TreasureRoomRelicSynchronizer __instance, Player player)
        {
            try
            {
                TryAwardIfConnectedComplete(__instance);
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"TreasureOnPickedPatch: {e.Message}");
            }
        }

        internal static void TryAwardIfConnectedComplete(TreasureRoomRelicSynchronizer sync)
        {
            IPlayerCollection players = (IPlayerCollection)AccessTools
                .Field(typeof(TreasureRoomRelicSynchronizer), "_playerCollection")
                .GetValue(sync)!;
            // PlayerVote is a private nested class — use dynamic list via reflection.
            object votesObj = AccessTools
                .Field(typeof(TreasureRoomRelicSynchronizer), "_votes")
                .GetValue(sync)!;
            if (votesObj is not System.Collections.IList votes)
                return;

            FieldInfo? voteReceivedField = null;
            bool any = false;
            for (int i = 0; i < players.Players.Count; i++)
            {
                Player p = players.Players[i];
                if (!DropOutUtil.IsParticipating(p))
                    continue;
                any = true;
                int slot = players.GetPlayerSlotIndex(p);
                if (slot < 0 || slot >= votes.Count)
                    return;
                object vote = votes[slot]!;
                voteReceivedField ??= vote.GetType().GetField("voteReceived");
                if (voteReceivedField == null)
                    return;
                if (!(bool)voteReceivedField.GetValue(vote)!)
                    return;
            }

            if (!any)
                return;

            // If vanilla All(voteReceived) already true, AwardRelics already ran.
            bool vanillaBlocked = false;
            for (int i = 0; i < votes.Count; i++)
            {
                object vote = votes[i]!;
                voteReceivedField ??= vote.GetType().GetField("voteReceived");
                if (!(bool)voteReceivedField!.GetValue(vote)!)
                {
                    vanillaBlocked = true;
                    break;
                }
            }

            if (!vanillaBlocked)
                return;

            // Mark leavers as skip (voteReceived, null index).
            FieldInfo? indexField = null;
            for (int i = 0; i < players.Players.Count && i < votes.Count; i++)
            {
                if (DropOutUtil.IsParticipating(players.Players[i]))
                    continue;
                object vote = votes[i]!;
                voteReceivedField ??= vote.GetType().GetField("voteReceived");
                indexField ??= vote.GetType().GetField("index");
                voteReceivedField?.SetValue(vote, true);
                indexField?.SetValue(vote, null);
            }

            MethodInfo? award = AccessTools.Method(
                typeof(TreasureRoomRelicSynchronizer),
                "AwardRelics");
            MethodInfo? end = AccessTools.Method(
                typeof(TreasureRoomRelicSynchronizer),
                "EndRelicVoting");
            if (award == null || end == null)
                return;

            MainFile.Logger.Info(
                "Treasure votes complete among connected players; awarding relics.");
            award.Invoke(sync, null);
            end.Invoke(sync, null);
        }
    }

}
