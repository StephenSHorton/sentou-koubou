using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace SharedCombatPositions;

/// <summary>
/// Vanilla hides remote player/pet health bars and powers until hover:
/// <c>_Ready</c> calls <c>HideImmediately</c>, <c>OnUnfocus</c> calls <c>AnimateOut</c>.
/// Keep teammate state UI (HP, block, powers/statuses) always visible <b>during combat only</b>.
///
/// Important: unfocus during combat teardown must NOT re-show bars, or they leak onto the map.
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
        // Vanilla AnimateOut after unhover — re-show only while combat UI is live.
        // After combat ends, unfocus still fires; re-showing here was leaking HP onto the map.
        AlwaysShowState.EnsureVisible(__instance, spawnAnim: false);
    }
}

/// <summary>
/// When hovering a teammate, vanilla temporarily hides the *local* HP bar.
/// Keep local bar visible too so everything stays on screen — only mid-combat.
/// </summary>
[HarmonyPatch(typeof(NCreature), nameof(NCreature.SetRemotePlayerFocused))]
public static class LocalHpWhileHoveringTeammatePatch
{
    public static bool Prefix(NCreature __instance, bool remotePlayerFocused)
    {
        if (!AlwaysShowState.IsCombatUiActive())
            return true; // let vanilla hide/show after combat

        // Skip hide/show flip of local bar; teammate bars stay up via other patches.
        _ = __instance;
        _ = remotePlayerFocused;
        return false;
    }
}

/// <summary>
/// When combat ends, force remote state displays down so they cannot paint over the map.
/// </summary>
[HarmonyPatch(typeof(CombatManager), "EndCombatInternal")]
public static class CombatEndHideStatePatch
{
    public static void Prefix()
    {
        AlwaysShowState.HideAllRemoteStateDisplays();
    }
}

[HarmonyPatch(typeof(NCombatRoom), "_ExitTree")]
public static class CombatRoomExitHideStatePatch
{
    public static void Prefix()
    {
        AlwaysShowState.HideAllRemoteStateDisplays();
    }
}

internal static class AlwaysShowState
{
    /// <summary>
    /// True only while combat is setting up or actively running — not ending / not on map.
    /// </summary>
    public static bool IsCombatUiActive()
    {
        try
        {
            CombatManager? cm = CombatManager.Instance;
            if (cm == null)
                return false;
            // Active fight or brief setup window before IsInProgress flips true.
            if (cm.IsInProgress || cm.IsStarting)
                return true;
            return false;
        }
        catch
        {
            return false;
        }
    }

    public static void EnsureVisible(NCreature creature, bool spawnAnim)
    {
        if (creature == null || !GodotObject.IsInstanceValid(creature))
            return;

        if (!IsCombatUiActive())
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

    /// <summary>Collapse remote teammate HP/status UI (combat end / room teardown).</summary>
    public static void HideAllRemoteStateDisplays()
    {
        try
        {
            NCombatRoom? room = NCombatRoom.Instance;
            if (room == null || !GodotObject.IsInstanceValid(room))
                return;

            foreach (Node node in EnumerateDescendants(room))
            {
                if (node is not NCreature creature || !GodotObject.IsInstanceValid(creature))
                    continue;
                try
                {
                    if (!creature._isRemotePlayerOrPet)
                        continue;
                    var display = creature._stateDisplay;
                    if (display == null || !GodotObject.IsInstanceValid(display))
                        continue;
                    display.HideImmediately();
                    display.Visible = false;
                }
                catch
                {
                    // ignore per-creature failures during teardown
                }
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Hide remote state UI failed: {e.Message}");
        }
    }

    private static IEnumerable<Node> EnumerateDescendants(Node root)
    {
        var stack = new Stack<Node>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            Node n = stack.Pop();
            yield return n;
            foreach (Node child in n.GetChildren())
                stack.Push(child);
        }
    }
}
