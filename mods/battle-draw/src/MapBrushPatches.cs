using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace BattleDraw;

/// <summary>
/// Restyle local map pen with shared brush color/size.
/// Toolbar is global (one instance) — not parented under the map screen.
/// </summary>
[HarmonyPatch(typeof(NMapDrawings), nameof(NMapDrawings.CreateLineForPlayer))]
public static class MapCreateLinePatch
{
    public static void Postfix(Player player, bool isErasing, Line2D __result)
    {
        if (__result == null)
            return;
        if (!LocalContext.IsMe(player))
            return;

        try
        {
            if (isErasing)
            {
                // Full-weight eraser: match pen width so thick strokes wipe clean.
                float w = Math.Max(BrushConfig.ClampedSize * 2.5f, BrushConfig.ClampedSize + 4f);
                __result.Width = Math.Clamp(w, 4f, 48f);
                return;
            }

            __result.DefaultColor = BrushConfig.CurrentColor;
            __result.Width = BrushConfig.ClampedSize;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Map brush style failed: {e.Message}");
        }
    }
}

[HarmonyPatch(typeof(NMapScreen), "_Ready")]
public static class MapScreenReadyPatch
{
    public static void Postfix(NMapScreen __instance)
    {
        try
        {
            BrushToolbar.EnsureGlobal();
            BrushToolbar.Instance?.SetCombatContext(false);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Map toolbar ensure failed: {e.Message}");
        }
    }
}
