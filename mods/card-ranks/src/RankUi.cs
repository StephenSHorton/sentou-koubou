using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace CardRanks;

/// <summary>
/// Post-combine: auto bonus (visible vanilla enchant when possible) + ribbon VFX.
/// Silent remove of both sacrifices; single NCardEnchantVfx on the survivor.
/// </summary>
public static class RankUi
{
    public static async Task<TierBonus> AutoGrantBonusAndShowcaseAsync(
        CardModel sacrifice1,
        CardModel sacrifice2,
        CardModel survivor,
        CardRankLevel newTier,
        Func<Task>? removeSacrificesAsync)
    {
        TierBonus granted = TierBonus.None;

        if (CardRanksConfig.OfferTierBonusRolls
            && newTier is CardRankLevel.Tier1 or CardRankLevel.Tier2 or CardRankLevel.Tier3)
        {
            try
            {
                TierBonus? picked = TierBonusService.RollNew(survivor);
                if (picked != null)
                {
                    TierBonusService.Apply(survivor, picked.Value);
                    granted = picked.Value;
                    MainFile.Logger.Info(
                        $"Auto tier bonus GRANTED: {TierBonusService.DisplayName(granted)} " +
                        $"on {survivor.Id} (Tier {RankMath.TierRoman(newTier)}) " +
                        $"| {CombineService.Describe(survivor)}");
                }
                else
                {
                    MainFile.Logger.Info(
                        $"Auto tier bonus: pool exhausted for {survivor.Id}");
                }
            }
            catch (Exception e)
            {
                MainFile.Logger.Error($"Auto tier bonus failed: {e}");
            }
        }

        await PlayCombineRevealAsync(sacrifice1, sacrifice2, survivor, removeSacrificesAsync);
        return granted;
    }

    public static async Task PlayCombineRevealAsync(
        CardModel sacrifice1,
        CardModel sacrifice2,
        CardModel survivor,
        Func<Task>? removeSacrificesAsync)
    {
        try
        {
            if (removeSacrificesAsync != null)
                await removeSacrificesAsync();
            else
                await CombineService.RemoveSacrificesAsync(sacrifice1, sacrifice2);

            SpawnEnchantVfx(survivor);
            await Task.Delay(750);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Combine reveal failed: {e.Message}");
            try
            {
                if (removeSacrificesAsync != null)
                    await removeSacrificesAsync();
            }
            catch
            {
                // ignore
            }
        }
    }

    private static void SpawnEnchantVfx(CardModel card)
    {
        try
        {
            var vfx = NCardEnchantVfx.Create(card);
            if (vfx == null)
                return;
            NRun? run = NRun.Instance;
            run?.GlobalUi?.CardPreviewContainer?.AddChildSafely(vfx);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Enchant VFX failed: {e.Message}");
        }
    }
}
