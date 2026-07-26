using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace SharedCombatPositions;

/// <summary>
/// Vanilla hides remote player/pet health bars and powers until hover:
/// <c>_Ready</c> calls <c>HideImmediately</c>, <c>OnUnfocus</c> calls <c>AnimateOut</c>.
/// Keep teammate state UI (HP, block, powers/statuses) always visible.
/// </summary>
[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
public static class CreatureReadyAlwaysShowPatch
{
    public static void Postfix(NCreature __instance)
    {
        AlwaysShowState.EnsureVisible(__instance, spawnAnim: true);
    }
}

[HarmonyPatch(typeof(NCreature), "OnUnfocus")]
public static class CreatureUnfocusAlwaysShowPatch
{
    public static void Postfix(NCreature __instance)
    {
        // Vanilla AnimateOut after unhover — re-show immediately.
        AlwaysShowState.EnsureVisible(__instance, spawnAnim: false);
    }
}

/// <summary>
/// When hovering a teammate, vanilla temporarily hides the *local* HP bar.
/// Keep local bar visible too so everything stays on screen.
/// </summary>
[HarmonyPatch(typeof(NCreature), nameof(NCreature.SetRemotePlayerFocused))]
public static class LocalHpWhileHoveringTeammatePatch
{
    public static bool Prefix(NCreature __instance, bool remotePlayerFocused)
    {
        // Skip hide/show flip of local bar; teammate bars stay up via other patches.
        _ = __instance;
        _ = remotePlayerFocused;
        return false;
    }
}

internal static class AlwaysShowState
{
    public static void EnsureVisible(NCreature creature, bool spawnAnim)
    {
        if (creature == null || !GodotObject.IsInstanceValid(creature))
            return;

        try
        {
            // Publicized private fields.
            if (!creature._isRemotePlayerOrPet)
                return;

            var entity = creature.Entity;
            if (entity == null || entity.IsDead)
                return;

            var display = creature._stateDisplay;
            if (display == null || !GodotObject.IsInstanceValid(display))
                return;

            var mode = spawnAnim
                ? HealthBarAnimMode.SpawnedDuringCombat
                : HealthBarAnimMode.FromHidden;
            display.AnimateIn(mode);
            display.Visible = true;
            // Nameplate still hover-only (vanilla dimming of powers on nameplate show is fine).
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Always-show state UI failed: {e.Message}");
        }
    }
}
