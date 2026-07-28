using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Potions;

namespace UncappedChapterFix;

/// <summary>
/// After chapter transitions / Neow rewards, potion rewards can still be clickable when the
/// belt is full. Vanilla then throws <c>InvalidOperationException: Slot already contains a potion</c>
/// inside <see cref="NPotionHolder.AddPotion"/>. Soft-fail instead of error spam / failed reward.
/// </summary>
public static class PotionSlotHardenPatches
{
    public static bool TryApply(Harmony harmony)
    {
        int n = 0;

        // Prefer early out at procure so game state never adds a phantom potion.
        // Game builds may return PotionModel? or Task<PotionModel?> — pick a matching prefix.
        MethodInfo? tryProcure = AccessTools.Method(
            typeof(PotionCmd),
            nameof(PotionCmd.TryToProcure),
            [typeof(PotionModel), typeof(Player), typeof(int)]);
        if (tryProcure != null)
        {
            string prefixName = tryProcure.ReturnType.Name.StartsWith("Task", StringComparison.Ordinal)
                ? nameof(TryToProcureTaskPrefix)
                : nameof(TryToProcurePrefix);
            try
            {
                harmony.Patch(
                    tryProcure,
                    prefix: new HarmonyMethod(typeof(PotionSlotHardenPatches), prefixName));
                n++;
                MainFile.Logger.Info(
                    $"Patched PotionCmd.TryToProcure via {prefixName} " +
                    $"(return {tryProcure.ReturnType.Name}; skip when no open potion slots).");
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn(
                    $"PotionCmd.TryToProcure patch skipped ({tryProcure.ReturnType.Name}): {e.Message}");
            }
        }

        // Belt-and-suspenders: never throw from UI holder if slot already filled.
        MethodInfo? addPotion = AccessTools.Method(typeof(NPotionHolder), nameof(NPotionHolder.AddPotion));
        if (addPotion != null)
        {
            harmony.Patch(
                addPotion,
                prefix: new HarmonyMethod(typeof(PotionSlotHardenPatches), nameof(AddPotionPrefix)));
            n++;
            MainFile.Logger.Info(
                "Patched NPotionHolder.AddPotion (no-throw when slot already full).");
        }

        return n > 0;
    }

    /// <summary>
    /// Returns false (skip original) when the player has no free potion slot and no explicit
    /// empty slot index was requested. Sets <paramref name="__result"/> to null to match
    /// "procure failed" semantics.
    /// </summary>
    public static bool TryToProcurePrefix(
        PotionModel potion,
        Player player,
        int slotIndex,
        ref PotionModel? __result)
    {
        if (!ShouldBlockProcure(potion, player, slotIndex, out _))
            return true;
        __result = null;
        return false;
    }

    /// <summary>Async overload when TryToProcure returns Task&lt;PotionModel?&gt;.</summary>
    public static bool TryToProcureTaskPrefix(
        PotionModel potion,
        Player player,
        int slotIndex,
        ref Task<PotionModel?> __result)
    {
        if (!ShouldBlockProcure(potion, player, slotIndex, out _))
            return true;
        __result = Task.FromResult<PotionModel?>(null);
        return false;
    }

    private static bool ShouldBlockProcure(
        PotionModel? potion,
        Player? player,
        int slotIndex,
        out string reason)
    {
        reason = "";
        try
        {
            if (player == null)
                return false;

            if (slotIndex >= 0)
            {
                PotionModel? existing = null;
                try
                {
                    existing = player.GetPotionAtSlotIndex(slotIndex);
                }
                catch
                {
                    return false;
                }

                if (existing != null)
                {
                    reason =
                        $"TryToProcure blocked: slot {slotIndex} already has {existing.Id} " +
                        $"(wanted {potion?.Id}).";
                    MainFile.Logger.Warn(reason);
                    return true;
                }

                return false;
            }

            if (!player.HasOpenPotionSlots)
            {
                reason = $"TryToProcure blocked: no open potion slots (wanted {potion?.Id}).";
                MainFile.Logger.Warn(reason);
                return true;
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"ShouldBlockProcure: {e.Message}");
        }

        return false;
    }

    public static bool AddPotionPrefix(NPotionHolder __instance)
    {
        try
        {
            if (__instance != null && __instance.HasPotion)
            {
                MainFile.Logger.Warn(
                    "NPotionHolder.AddPotion skipped — slot already contains a potion.");
                return false;
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"AddPotionPrefix: {e.Message}");
        }
        return true;
    }
}
