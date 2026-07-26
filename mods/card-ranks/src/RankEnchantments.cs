using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
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
        // Replay extras must be multiplayer-visible state only:
        // - Vanilla Spiral leaf: MultiEnchantment composes leaf EnchantPlayCount.
        // - BaseReplayCount: already passed in as `playCount` by GetEnchantedReplayCount.
        //
        // CWT ReplayBonus is NOT serialized and desynced peers after campfire combine
        // (host applied 1 play, client 2 — FOGMOG HP/strength/CrushUnder power mismatch).
        // Never add it here.
        return playCount;
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

    /// <summary>
    /// Fallback for Imbued when we only have a CWT flag (no stackable vanilla leaf).
    /// Vanilla <see cref="MegaCrit.Sts2.Core.Models.Enchantments.Imbued"/> also hooks
    /// AutoPrePlay but only on <c>TurnNumber == 1</c>. Without that gate, a ranked card
    /// with the Imbued bonus auto-played every turn.
    /// </summary>
    public override async Task AfterAutoPrePlayPhaseEntered(
        PlayerChoiceContext context, Player player)
    {
        if (Card == null || !TierBonusService.HasImbued(Card))
            return;
        if (Card.Owner != player)
            return;

        // Real Imbued leaf (Uncapped multi-stack) already auto-plays on turn 1 — do not double.
        if (Card.Enchantment is Imbued
            || MultiEnchantCompat.EnumerateLeafEnchantments(Card).Any(e => e is Imbued))
            return;

        // Match vanilla Imbued: only the first player turn of combat.
        PlayerCombatState? combat = player.PlayerCombatState;
        if (combat == null || combat.TurnNumber != 1)
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
