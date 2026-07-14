using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace CardRanks;

/// <summary>
/// Post-combine feedback: auto tier bonus + brief card showcase (no dialogs).
/// </summary>
public static class RankUi
{
    /// <summary>
    /// Auto-roll a tier bonus (no skip prompt) and flash the card like a reward pick.
    /// Returns the bonus applied (or None if pool empty / disabled).
    /// </summary>
    public static async Task<TierBonus> AutoGrantBonusAndShowcaseAsync(
        CardModel card, CardRankLevel newTier)
    {
        TierBonus granted = TierBonus.None;

        if (CardRanksConfig.OfferTierBonusRolls
            && newTier is CardRankLevel.Tier1 or CardRankLevel.Tier2 or CardRankLevel.Tier3)
        {
            TierBonus? picked = TierBonusService.RollNew(card);
            if (picked != null)
            {
                TierBonusService.Apply(card, picked.Value);
                granted = picked.Value;
                MainFile.Logger.Info(
                    $"Auto tier bonus {TierBonusService.DisplayName(granted)} on {card.Id} " +
                    $"(Tier {RankMath.TierRoman(newTier)})");
            }
            else
            {
                MainFile.Logger.Info($"No remaining tier bonuses for {card.Id}");
            }
        }

        await ShowcaseCardAsync(card);
        return granted;
    }

    /// <summary>
    /// Same visual language as buying / picking a card: big preview, then it settles.
    /// </summary>
    public static async Task ShowcaseCardAsync(CardModel card)
    {
        try
        {
            SpawnEnchantVfx(card);

            // Reward-style card flash (duration seconds).
            TaskCompletionSource? tcs = CardCmd.Preview(
                card, 2.2f, CardPreviewStyle.HorizontalLayout);

            if (tcs != null)
            {
                Task finished = tcs.Task;
                Task timeout = Task.Delay(2800);
                await Task.WhenAny(finished, timeout);
            }
            else
            {
                await Task.Delay(2200);
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Card showcase failed: {e.Message}");
            await Task.Delay(400);
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
