using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace BattleDraw;

/// <summary>Spawn the doodle layer when a combat room comes up.</summary>
[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
public static class CombatRoomReadyPatch
{
    public static void Postfix(NCombatRoom __instance)
    {
        try
        {
            DrawCanvas.AttachTo(__instance);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Failed to attach draw canvas: {e}");
        }
    }
}

/// <summary>Wipe ink as soon as combat is won (before rewards settle in).</summary>
[HarmonyPatch(typeof(CombatManager), "EndCombatInternal")]
public static class CombatEndClearPatch
{
    public static void Prefix()
    {
        DrawCanvas.Instance?.ClearAll();
    }
}

/// <summary>
/// Belt-and-suspenders: UI cleanup path also wipes strokes if EndCombatInternal
/// was skipped or the room is torn down another way.
/// </summary>
[HarmonyPatch(typeof(NCombatUi), "PostCombatCleanUp")]
public static class PostCombatUiCleanPatch
{
    public static void Prefix()
    {
        DrawCanvas.Instance?.ClearAll();
    }
}

[HarmonyPatch(typeof(NCombatRoom), "_ExitTree")]
public static class CombatRoomExitPatch
{
    public static void Prefix()
    {
        DrawCanvas.Instance?.ClearAll();
        if (DrawCanvas.Instance != null)
        {
            DrawCanvas.Instance.QueueFree();
        }
    }
}
