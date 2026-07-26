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
                // Subtractive erase (map line_erase shader: blend_sub, RGB *= texture.a).
                // Vanilla sets DefaultColor to Character.MapDrawingColor for BOTH pen and
                // eraser. That only fully clears when pen used that same color. Our pen
                // uses BrushConfig.CurrentColor, so character-colored erase leaves residual
                // / "negative" ink. White subtracts every channel at full weight.
                __result.DefaultColor = Colors.White;
                // Wider than pen (vanilla erase default 12 vs pen 4 ≈ 3×) so soft trail
                // edges and half-res upscale still wipe solid coverage.
                float pen = BrushConfig.ClampedSize;
                float w = Math.Max(pen * 3f, pen + 6f);
                __result.Width = Math.Clamp(w, 8f, 64f);
                return;
            }

            Color ink = BrushConfig.CurrentColor;
            if (ink.A < 0.95f)
                ink.A = 1f;
            __result.DefaultColor = ink;
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
