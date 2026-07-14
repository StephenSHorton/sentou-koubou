using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CardRanks;

public abstract class RankEnchantment : CustomEnchantmentModel
{
    public abstract CardRankLevel Rank { get; }

    public abstract decimal Multiplier { get; }

    /// <summary>Ranks are exclusive tiers, never numeric stacks on the card face.</summary>
    public override bool ShowAmount => false;

    public override decimal EnchantDamageMultiplicative(decimal originalDamage, ValueProp props)
    {
        return !props.IsPoweredAttack() ? 1m : Multiplier;
    }

    public override decimal EnchantBlockMultiplicative(decimal originalBlock)
    {
        return Multiplier;
    }
}

public sealed class SecondRank : RankEnchantment
{
    // Packaged via PckPacker (card_ranks/*.png). Avoid broken res:// paths — missing
    // enchantment textures have been observed to scramble deck-select layout after rank-up.
    protected override string CustomIconPath => "res://card_ranks/rank2.png";

    public override CardRankLevel Rank => CardRankLevel.Rank2;

    public override decimal Multiplier => RankMath.Rank2Multiplier;
}

public sealed class ThirdRank : RankEnchantment
{
    protected override string CustomIconPath => "res://card_ranks/rank3.png";

    public override CardRankLevel Rank => CardRankLevel.Rank3;

    public override decimal Multiplier => RankMath.Rank3Multiplier;
}
