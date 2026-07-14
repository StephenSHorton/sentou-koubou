using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace CardRanks;

/// <summary>
/// Optional tier-bonus roll UI. Rank enchantment is never cleared.
/// </summary>
public static class RankUi
{
    public static async Task NotifyAsync(string body)
    {
        NGenericPopup? popup = NGenericPopup.Create();
        NModalContainer? container = NModalContainer.Instance;
        if (popup == null || container == null)
            return;
        await WaitForModalClear(container);
        container.Add(popup);
        await popup.WaitForConfirmation(
            Loc.Dynamic(body),
            Loc.Dynamic("Card Ranks"),
            null,
            Loc.Dynamic("OK"));
    }

    public static async Task<bool> ConfirmAsync(string body, string accept = "Roll", string decline = "Skip")
    {
        NGenericPopup? popup = NGenericPopup.Create();
        NModalContainer? container = NModalContainer.Instance;
        if (popup == null || container == null)
            return false;
        await WaitForModalClear(container);
        container.Add(popup);
        return await popup.WaitForConfirmation(
            Loc.Dynamic(body),
            Loc.Dynamic("Card Ranks"),
            Loc.Dynamic(decline),
            Loc.Dynamic(accept));
    }

    /// <summary>
    /// After a card reaches a new tier: offer optional bonus roll (skippable).
    /// Returns the bonus applied (or None if skipped / exhausted).
    /// </summary>
    public static async Task<TierBonus> MaybeOfferBonusRollAsync(CardModel card, CardRankLevel newTier)
    {
        if (!CardRanksConfig.OfferTierBonusRolls)
            return TierBonus.None;

        if (newTier is not (CardRankLevel.Tier1 or CardRankLevel.Tier2 or CardRankLevel.Tier3))
            return TierBonus.None;

        string roman = RankMath.TierRoman(newTier);
        bool roll = await ConfirmAsync(
            $"Reached Tier {roman}!\n\n" +
            "Roll a bonus effect? Rank is kept either way.\n\n" +
            "Pool: Clone, Soul's Power, Steady, Spiral, Imbued, Perfect Fit, Royally Approved");

        if (!roll)
        {
            MainFile.Logger.Info($"Tier {roman} bonus skipped for {card.Id}");
            return TierBonus.None;
        }

        TierBonus? picked = TierBonusService.RollNew(card);
        if (picked == null)
        {
            await NotifyAsync("This card already has every bonus in the pool.");
            return TierBonus.None;
        }

        TierBonusService.Apply(card, picked.Value);
        await NotifyAsync(
            $"{TierBonusService.DisplayName(picked.Value)}\n\n" +
            $"{TierBonusService.Description(picked.Value)}\n\n" +
            $"(Tier {roman} kept.)");
        return picked.Value;
    }

    private static async Task WaitForModalClear(NModalContainer container)
    {
        for (int i = 0; i < 400 && container.OpenModal != null; i++)
            await Task.Delay(50);
    }
}
