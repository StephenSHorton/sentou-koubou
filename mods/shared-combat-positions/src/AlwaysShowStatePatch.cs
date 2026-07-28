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
/// Also lifts ally state displays above creature sprites so back-row HP is not
/// occluded by front-row bodies (shared multi-row lineup).
///
/// Important: unfocus during combat teardown must NOT re-show bars, or they leak onto the map.
/// </summary>
[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
public static class CreatureReadyAlwaysShowPatch
{
    public static void Postfix(NCreature __instance)
    {
        AlwaysShowState.EnsureVisible(__instance, spawnAnim: true);
        AlwaysShowState.LiftStateDisplayAboveCreatures(__instance);
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
        AlwaysShowState.LiftStateDisplayAboveCreatures(__instance);
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
/// After layout (including multi-row host order), re-lift ally HP so draw order
/// matches shared positions — tree MoveChild alone would bury back-row bars.
/// </summary>
[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.PositionPlayersAndPets))]
public static class PositionPlayersLiftStatePatch
{
    public static void Postfix(List<NCreature> creatureNodes)
    {
        if (creatureNodes == null)
            return;

        foreach (var creature in creatureNodes)
            AlwaysShowState.LiftStateDisplayAboveCreatures(creature);
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
    /// Absolute CanvasItem z so ally HP/status draws above other creature sprites.
    /// Stays on the combat canvas (below CanvasLayer UI such as hand / menus).
    /// </summary>
    private const int AllyStateDisplayZIndex = 50;

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
            LiftStateDisplay(display);
            // Nameplate still hover-only (vanilla dimming of powers on nameplate show is fine).
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Always-show state UI failed: {e.Message}");
        }
    }

    /// <summary>
    /// Raise ally (player/pet) state UI above creature bodies so multi-row overlap
    /// does not hide HP/block/powers. Local players in a back row need this too.
    /// </summary>
    public static void LiftStateDisplayAboveCreatures(NCreature creature)
    {
        if (creature == null || !GodotObject.IsInstanceValid(creature))
            return;

        if (!IsCombatUiActive())
            return;

        try
        {
            var entity = creature.Entity;
            if (entity == null || entity.IsDead)
                return;

            // Players and their pets only — leave enemy HP draw order alone.
            if (!entity.IsPlayer && entity.PetOwner == null)
                return;

            var display = creature._stateDisplay;
            if (display == null || !GodotObject.IsInstanceValid(display))
                return;

            LiftStateDisplay(display);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Lift state UI z-order failed: {e.Message}");
        }
    }

    private static void LiftStateDisplay(NCreatureStateDisplay display)
    {
        // Absolute z so this Control draws above other NCreature trees regardless
        // of MoveChild sibling order used for multi-row lineup.
        display.ZAsRelative = false;
        display.ZIndex = AllyStateDisplayZIndex;
    }

    private static void ResetStateDisplayZ(NCreatureStateDisplay display)
    {
        display.ZAsRelative = true;
        display.ZIndex = 0;
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
                    var display = creature._stateDisplay;
                    if (display == null || !GodotObject.IsInstanceValid(display))
                        continue;

                    // Reset z for every ally we lifted (local + remote).
                    var entity = creature.Entity;
                    if (entity != null && (entity.IsPlayer || entity.PetOwner != null))
                        ResetStateDisplayZ(display);

                    if (!creature._isRemotePlayerOrPet)
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
