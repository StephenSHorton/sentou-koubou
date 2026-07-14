using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace CardRanks;

/// <summary>
/// Post-combine feedback: auto bonus + sacrifice burn + survivor ribbon showcase.
/// Never dual-card "before/after" for the two combine picks — one burns, one gains the ribbon.
/// </summary>
public static class RankUi
{
    /// <summary>
    /// Auto-grant a random tier bonus (no dialog), then play combine VFX:
    /// sacrifice burns away, survivor alone gains the tier ribbon.
    /// </summary>
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
        else
        {
            MainFile.Logger.Info(
                $"Auto tier bonus disabled or not a tier-up (tier={newTier}, " +
                $"setting={CardRanksConfig.OfferTierBonusRolls})");
        }

        await PlayCombineRevealAsync(sacrifice, survivor, removeSacrificeAsync);
        return granted;
    }

    /// <summary>
    /// 1) Burn/remove the sacrifice alone (deck remove with preview = exhaust feel).
    /// 2) Flash only the survivor gaining the tier ribbon + enchant VFX.
    /// </summary>
    public static async Task PlayCombineRevealAsync(
        CardModel sacrifice,
        CardModel survivor,
        Func<Task>? removeSacrificeAsync)
    {
        try
        {
            // Burn first — only the sacrificed card leaves the deck with preview.
            if (removeSacrificeAsync != null)
                await removeSacrificeAsync();
            else
                await CardPileCmd.RemoveFromDeck(sacrifice, showPreview: true);

            // Readable beat between burn and ribbon.
            await Task.Delay(500);

            SpawnEnchantVfx(survivor);

            // Single-card reward flash — never pass both combine picks.
            TaskCompletionSource? tcs = CardCmd.Preview(
                survivor, 2.2f, CardPreviewStyle.HorizontalLayout);

            if (tcs != null)
                await Task.WhenAny(tcs.Task, Task.Delay(2800));
            else
                await Task.Delay(2200);
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
