using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CardRanks;

/// <summary>
/// Rank lives in the enchantment slot (multiplier + icon). Tier bonuses use keywords/Replay
/// and hooks here so rank is never cleared when a bonus is rolled.
/// </summary>
public abstract class RankEnchantment : CustomEnchantmentModel
{
    public abstract CardRankLevel Rank { get; }

    public abstract decimal Multiplier { get; }

    public override bool ShowAmount => false;

    public override decimal EnchantDamageMultiplicative(decimal originalDamage, ValueProp props)
    {
        return !props.IsPoweredAttack() ? 1m : Multiplier;
    }

    public override decimal EnchantBlockMultiplicative(decimal originalBlock)
    {
        return Multiplier;
    }

    public override int EnchantPlayCount(int playCount)
    {
        return playCount + TierBonusService.ReplayBonus(Card);
    }

    /// <summary>Perfect Fit: move this card to the front of the shuffled draw order.</summary>
    public override void ModifyShuffleOrder(Player player, List<CardModel> cards, bool isCombatStartShuffle)
    {
        if (Card == null || !TierBonusService.Has(Card, TierBonus.PerfectFit))
            return;
        if (isCombatStartShuffle)
            return; // vanilla Perfect Fit skips opening shuffle
        if (!cards.Contains(Card))
            return;
        cards.Remove(Card);
        cards.Insert(0, Card);
    }

    /// <summary>Imbued: free auto-play at combat start.</summary>
    public override async Task AfterAutoPrePlayPhaseEntered(
        PlayerChoiceContext context, Player player)
    {
        if (Card == null || !TierBonusService.HasImbued(Card))
            return;
        if (Card.Owner != player)
            return;
        try
        {
            await CardCmd.AutoPlay(
                context, Card, player.Creature, AutoPlayType.Default, false, false);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Imbued auto-play failed: {e.Message}");
        }
    }
}

/// <summary>Tier I — blue badge, ×1.5.</summary>
public sealed class FirstRank : RankEnchantment
{
    protected override string CustomIconPath => "res://card_ranks/rank1.png";

    public override CardRankLevel Rank => CardRankLevel.Tier1;

    public override decimal Multiplier => RankMath.Tier1Multiplier;
}

/// <summary>Tier II — ×2.</summary>
public sealed class SecondRank : RankEnchantment
{
    protected override string CustomIconPath => "res://card_ranks/rank2.png";

    public override CardRankLevel Rank => CardRankLevel.Tier2;

    public override decimal Multiplier => RankMath.Tier2Multiplier;
}

/// <summary>Tier III — ×3 (max).</summary>
public sealed class ThirdRank : RankEnchantment
{
    protected override string CustomIconPath => "res://card_ranks/rank3.png";

    public override CardRankLevel Rank => CardRankLevel.Tier3;

    public override decimal Multiplier => RankMath.Tier3Multiplier;
}
