using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Whitney.WhitneyCode.Powers;

namespace Whitney.WhitneyCode.Relics;

/// <summary>When you spend Ink, gain Block.</summary>
public sealed class MossCharm : WhitneyRelic
{
    private int _lastInk;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(3, ValueProp.Unpowered)];

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Detect ink spent by comparing power amount before/after is hard;
        // simpler: when card has InkCost via name convention is unavailable.
        // Hook AfterDamageReceived unused — instead: after any card by owner, if ink decreased.
        await Task.CompletedTask;
    }

    public override async Task AfterSideTurnStart(
        MegaCrit.Sts2.Core.Combat.CombatSide side,
        IReadOnlyList<Creature> participants,
        MegaCrit.Sts2.Core.Combat.ICombatState combatState)
    {
        if (Owner?.Creature is null) return;
        if (!participants.Contains(Owner.Creature)) return;
        _lastInk = Owner.Creature.GetPower<InkPower>()?.Amount ?? 0;
        await Task.CompletedTask;
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner) return;
        if (Owner?.Creature is null) return;
        var ink = Owner.Creature.GetPower<InkPower>()?.Amount ?? 0;
        if (ink < _lastInk)
        {
            Flash();
            await CreatureCmd.GainBlock(
                Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Unpowered, null);
        }
        _lastInk = ink;
    }
}
