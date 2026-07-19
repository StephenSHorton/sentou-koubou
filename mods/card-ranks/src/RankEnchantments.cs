using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CardRanks;

/// <summary>
/// Rank lives in the enchantment slot (one ribbon + multiplier).
/// Amount is always 1 so the UI never stacks multiple ribbons.
/// </summary>
public abstract class RankEnchantment : CustomEnchantmentModel
{
    public abstract CardRankLevel Rank { get; }

    public abstract decimal Multiplier { get; }

    public override bool ShowAmount => false;

    /// <summary>Never paint stacked ribbons.</summary>
    public override int DisplayAmount => 1;

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
        // If a vanilla Spiral leaf is already on the multi-enchant stack, it applies
        // its own play-count hook — do not also add our CWT ReplayBonus.
        if (Card != null
            && MultiEnchantCompat.EnumerateLeafEnchantments(Card)
                .Any(e => e.GetType().Name.Equals("Spiral", StringComparison.OrdinalIgnoreCase)))
        {
            return playCount;
        }

        return playCount + TierBonusService.ReplayBonus(Card);
    }

    public override void ModifyShuffleOrder(Player player, List<CardModel> cards, bool isCombatStartShuffle)
    {
        if (Card == null || !TierBonusService.Has(Card, TierBonus.PerfectFit))
            return;
        if (isCombatStartShuffle)
            return;
        if (!cards.Contains(Card))
            return;
        cards.Remove(Card);
        cards.Insert(0, Card);
    }

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
