using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace CardRanks;

/// <summary>
/// Post-combine feedback. Matches RankUpCards2: silent sacrifice remove + single
/// NCardEnchantVfx on the survivor. No CardCmd.Preview — Preview + VFX on an
/// already-ranked card draws two copies (one with ribbon, one "gaining" it).
/// </summary>
public static class RankUi
{
    public static async Task<TierBonus> AutoGrantBonusAndShowcaseAsync(
        CardModel sacrifice,
        CardModel survivor,
        CardRankLevel newTier,
        Func<Task>? removeSacrificeAsync)
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

        await PlayCombineRevealAsync(sacrifice, survivor, removeSacrificeAsync);
        return granted;
    }

    /// <summary>
    /// Silent remove sacrifice, then one enchant-ribbon VFX on the survivor.
    /// </summary>
    public static async Task PlayCombineRevealAsync(
        CardModel sacrifice,
        CardModel survivor,
        Func<Task>? removeSacrificeAsync)
    {
        try
        {
            if (removeSacrificeAsync != null)
                await removeSacrificeAsync();
            else
                await CardPileCmd.RemoveFromDeck(sacrifice, showPreview: false);

            // Only the ribbon VFX — do NOT also CardCmd.Preview(survivor).
            // Rank is already applied; Preview would show a second static copy
            // that already has the ribbon while VFX animates another.
            SpawnEnchantVfx(survivor);

            // Just long enough for the VFX to be readable before rest-site UI resumes.
            await Task.Delay(750);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Combine reveal failed: {e.Message}");
            try
            {
                if (removeSacrificeAsync != null)
                    await removeSacrificeAsync();
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
