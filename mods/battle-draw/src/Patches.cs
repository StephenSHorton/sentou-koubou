using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace BattleDraw;

/// <summary>Register multiplayer draw sync with the run's net bus (like map pen).</summary>
[HarmonyPatch(typeof(RunManager), "InitializeShared")]
public static class RunManagerInitializePatch
{
    public static void Postfix(RunManager __instance)
    {
        try
        {
            DrawSync.Instance?.Dispose();
            DrawSync.Instance = new DrawSync(
                __instance.RunLocationTargetedBuffer,
                __instance.NetService,
                __instance.NetService.NetId);
            BrushToolbar.EnsureGlobal();
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"DrawSync init failed: {e}");
        }
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
public static class RunManagerCleanUpPatch
{
    public static void Prefix()
    {
        DrawSync.Instance?.Dispose();
        DrawSync.Instance = null;
        BrushToolbar.Detach();
    }
}

/// <summary>Spawn the doodle layer when a combat room comes up.</summary>
[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
public static class CombatRoomReadyPatch
{
    public static void Postfix(NCombatRoom __instance)
    {
        try
        {
            // Attach creates under-UI ink host + surface + toolbar.
            DrawCanvas.AttachTo(__instance);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Failed to attach draw canvas: {e}");
        }
    }
}

/// <summary>
/// Tear down combat ink as soon as combat ends so RMB cannot keep drawing a
/// screen-space ghost that double-marks the map after the fight.
/// </summary>
[HarmonyPatch(typeof(CombatManager), "EndCombatInternal")]
public static class CombatEndClearPatch
{
    public static void Prefix()
    {
        DrawCanvas.Instance?.Teardown();
        BrushToolbar.DetachCombat();
    }
}

/// <summary>
/// Belt-and-suspenders: UI cleanup path also tears down if EndCombatInternal
/// was skipped or the room is torn down another way.
/// </summary>
[HarmonyPatch(typeof(NCombatUi), "PostCombatCleanUp")]
public static class PostCombatUiCleanPatch
{
    public static void Prefix()
    {
        DrawCanvas.Instance?.Teardown();
        BrushToolbar.DetachCombat();
    }
}

[HarmonyPatch(typeof(NCombatRoom), "_ExitTree")]
public static class CombatRoomExitPatch
{
    public static void Prefix()
    {
        DrawCanvas.Instance?.Teardown();
        BrushToolbar.DetachCombat();
    }
}
