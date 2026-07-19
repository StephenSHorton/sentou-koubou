using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace BattleDraw;

/// <summary>
/// Vanilla map pen uses <c>player.Character.MapDrawingColor</c> and the packed
/// Line2D scene width. After a line is created for the local player, restyle it
/// with the shared brush settings.
/// </summary>
/// <remarks>
/// Multiplayer: each client styles only their own lines on their machine. Friends
/// still see your character's default map color unless they also run this mod with
/// the same presets (network packets do not carry brush color/size).
/// </remarks>
[HarmonyPatch(typeof(NMapDrawings), nameof(NMapDrawings.CreateLineForPlayer))]
public static class MapCreateLinePatch
{
    public static void Postfix(Player player, bool isErasing, Line2D __result)
    {
        if (isErasing || __result == null)
            return;

        // Only restyle the local player's pen so other characters keep their identity colors.
        if (!LocalContext.IsMe(player))
            return;

        try
        {
            __result.DefaultColor = BrushConfig.CurrentColor;
            __result.Width = BrushConfig.ClampedSize;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Map brush style failed: {e.Message}");
        }
    }
}
