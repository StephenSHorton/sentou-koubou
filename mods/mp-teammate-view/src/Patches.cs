using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using MpTeammateView.Utils;

namespace MpTeammateView;

/// <summary>
/// Reliability fix vs upstream ShowPlayerHandCards: attach on player-state ready
/// (and combat events), not only SetUpCombat — that race often left hands empty.
/// Also restores EnergyVar.ColorPrefix after description generation (MP-safe).
/// </summary>
[HarmonyPatch(typeof(NMultiplayerPlayerState), nameof(NMultiplayerPlayerState._Ready))]
public static class PlayerStateReadyPatch
{
    public static void Postfix(NMultiplayerPlayerState __instance)
    {
        try
        {
            TeammateViewHost.Attach(__instance);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Attach teammate view failed: {e}");
        }
    }
}

[HarmonyPatch(typeof(NMultiplayerPlayerState), nameof(NMultiplayerPlayerState._ExitTree))]
public static class PlayerStateExitPatch
{
    public static void Prefix(NMultiplayerPlayerState __instance)
    {
        try
        {
            TeammateViewHost.Detach(__instance);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Detach teammate view: {e.Message}");
        }
    }
}

[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.SetUpCombat))]
public static class CombatSetupPatch
{
    private static bool _hooks;

    public static void Postfix()
    {
        EnsureCombatHooks();
        try
        {
            var tree = NRun.Instance?.GetTree();
            if (tree != null)
                Callable.From(() => RefreshAllHosts(combatRefresh: true)).CallDeferred();
            else
                RefreshAllHosts(combatRefresh: true);
        }
        catch
        {
            RefreshAllHosts(combatRefresh: true);
        }
    }

    private static void EnsureCombatHooks()
    {
        if (_hooks)
            return;
        try
        {
            var cm = CombatManager.Instance;
            cm.TurnStarted += OnTurnStarted;
            cm.CombatEnded += OnCombatEnded;
            _hooks = true;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Combat hooks failed: {e.Message}");
        }
    }

    private static void OnTurnStarted(object? _) => RefreshAllHosts(combatRefresh: true);

    private static void OnCombatEnded(object? _)
    {
        foreach (var host in TeammateViewHost.EnumerateHosts())
            host.OnCombatEnded();
    }

    internal static void RefreshAllHosts(bool combatRefresh)
    {
        foreach (var host in TeammateViewHost.EnumerateHosts())
        {
            try
            {
                if (combatRefresh)
                    host.OnCombatRefresh();
                else
                    host.RefreshPotions();
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"Host refresh: {e.Message}");
            }
        }
    }
}

[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.AfterCombatRoomLoaded))]
public static class AfterCombatRoomLoadedPatch
{
    public static void Postfix()
    {
        Callable.From(() => CombatSetupPatch.RefreshAllHosts(combatRefresh: true)).CallDeferred();
    }
}

/// <summary>
/// Restores EnergyVar.ColorPrefix after GetDescription* so mini-card UI text
/// building does not leak into multiplayer checksum state.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForPile),
    new[] { typeof(PileType), typeof(Creature) })]
public static class CardDescriptionForPileEnergyVarPatch
{
    public static void Prefix(CardModel __instance, ref List<(EnergyVar Var, string Prefix)>? __state)
    {
        __state = EnergyVarColorPrefixSnapshot.Capture(__instance);
    }

    public static Exception? Finalizer(ref List<(EnergyVar Var, string Prefix)>? __state)
    {
        EnergyVarColorPrefixSnapshot.Restore(__state);
        __state = null;
        return null;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForUpgradePreview))]
public static class CardDescriptionUpgradePreviewEnergyVarPatch
{
    public static void Prefix(CardModel __instance, ref List<(EnergyVar Var, string Prefix)>? __state)
    {
        __state = EnergyVarColorPrefixSnapshot.Capture(__instance);
    }

    public static Exception? Finalizer(ref List<(EnergyVar Var, string Prefix)>? __state)
    {
        EnergyVarColorPrefixSnapshot.Restore(__state);
        __state = null;
        return null;
    }
}
