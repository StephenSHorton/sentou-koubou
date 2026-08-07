using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace BattleBall;

[HarmonyPatch(typeof(RunManager), "InitializeShared")]
public static class RunManagerInitializePatch
{
    public static void Postfix(RunManager __instance)
    {
        try
        {
            BallSync.Instance?.Dispose();
            BallSync.Instance = new BallSync(
                __instance.RunLocationTargetedBuffer,
                __instance.NetService,
                __instance.NetService.NetId);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"BallSync init failed: {e}");
        }
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
public static class RunManagerCleanUpPatch
{
    public static void Prefix()
    {
        BallSync.Instance?.Dispose();
        BallSync.Instance = null;
    }
}

[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
public static class CombatRoomReadyPatch
{
    public static void Postfix(NCombatRoom __instance)
    {
        try
        {
            BallWorld.AttachTo(__instance);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Failed to attach Battle Ball: {e}");
        }
    }
}

/// <summary>
/// Combat room is fully live — re-attach if _Ready raced or Mode was VisualOnly first.
/// </summary>
[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.AfterCombatRoomLoaded))]
public static class AfterCombatRoomLoadedBallPatch
{
    public static void Postfix()
    {
        try
        {
            NCombatRoom? room = NCombatRoom.Instance;
            if (room == null || !GodotObject.IsInstanceValid(room))
                return;
            // Only attach if missing — do not ResetBall here (wipes an in-flight toss).
            if (BallWorld.Instance == null || !GodotObject.IsInstanceValid(BallWorld.Instance))
                BallWorld.AttachTo(room);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Battle Ball after-room-load: {e.Message}");
        }
    }
}

[HarmonyPatch(typeof(CombatManager), "EndCombatInternal")]
public static class CombatEndPatch
{
    public static void Prefix() => BallWorld.Instance?.Teardown();
}

[HarmonyPatch(typeof(NCombatUi), "PostCombatCleanUp")]
public static class PostCombatUiCleanPatch
{
    public static void Prefix() => BallWorld.Instance?.Teardown();
}

[HarmonyPatch(typeof(NCombatRoom), "_ExitTree")]
public static class CombatRoomExitPatch
{
    public static void Prefix() => BallWorld.Instance?.Teardown();
}
