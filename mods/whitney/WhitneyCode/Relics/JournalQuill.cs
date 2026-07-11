using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Whitney.WhitneyCode.Powers;

namespace Whitney.WhitneyCode.Relics;

/// <summary>Once per turn: when you gain Ink, draw 1.</summary>
public sealed class JournalQuill : WhitneyRelic
{
    private int _lastInk;
    private bool _drewThisTurn;

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(1)];

    public override async Task AfterSideTurnStart(
        MegaCrit.Sts2.Core.Combat.CombatSide side,
        IReadOnlyList<MegaCrit.Sts2.Core.Entities.Creatures.Creature> participants,
        MegaCrit.Sts2.Core.Combat.ICombatState combatState)
    {
        if (Owner?.Creature is null) return;
        if (!participants.Contains(Owner.Creature)) return;
        _lastInk = Owner.Creature.GetPower<InkPower>()?.Amount ?? 0;
        _drewThisTurn = false;
        await Task.CompletedTask;
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_drewThisTurn) return;
        if (cardPlay.Card.Owner != Owner) return;
        if (Owner?.Creature is null) return;
        var ink = Owner.Creature.GetPower<InkPower>()?.Amount ?? 0;
        if (ink > _lastInk)
        {
            _drewThisTurn = true;
            Flash();
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        }
        _lastInk = ink;
    }
}
