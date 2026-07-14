using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace CardRanks;

/// <summary>
/// Post-combine feedback: auto bonus + sacrifice burn, then survivor ribbon only.
/// Avoid stacking remove-preview + CardCmd.Preview + enchant VFX (that showed 3 cards).
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
    /// Phase 1: sacrifice burns alone (deck remove with preview).
    /// Phase 2: only after that settles, survivor gets ribbon enchant VFX (one card).
    /// </summary>
    public static async Task PlayCombineRevealAsync(
        CardModel sacrifice,
        CardModel survivor,
        Func<Task>? removeSacrificeAsync)
    {
        try
        {
            // --- Burn only the sacrifice (one card on screen) ---
            if (removeSacrificeAsync != null)
                await removeSacrificeAsync();
            else
                await CardPileCmd.RemoveFromDeck(sacrifice, showPreview: true);

            // RemoveFromDeck's Task can complete while the fly-away preview is still
            // visible. Wait long enough that it fully leaves before we show the survivor,
            // otherwise the player sees 2–3 cards stacked in the preview container.
            await Task.Delay(1400);

            // --- Survivor alone: ribbon enchant VFX (no second CardCmd.Preview) ---
            // NCardEnchantVfx is the "gaining the ribbon" animation. Adding CardCmd.Preview
            // on top of remove-preview + VFX was showing three cards.
            SpawnEnchantVfx(survivor);
            await Task.Delay(1600);
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
