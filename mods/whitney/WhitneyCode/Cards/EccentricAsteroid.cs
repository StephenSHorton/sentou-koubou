using Whitney.WhitneyCode.PatchesNModels;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Whitney.WhitneyCode.Cards;

public class EccentricAsteroid : AbstractWhitneyCard
{
    public EccentricAsteroid() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    //public override string PortraitPath => "res://Whitney/images/cards/whitney-test_whitney_card.png";

    public override bool GainsBlock => true;

    protected override bool ShouldGlowGoldInternal => Sparked(this);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        // [
        //     new CalculationBaseVar(6),
        //     new CalculationExtraVar(2),
        //     new CalculatedBlockVar(ValueProp.Move).WithMultiplier((card, _) =>
        //         CombatManager.Instance.History.CardPlaysFinished.Count(e =>
        //             e.HappenedThisTurn(card.CombatState) && e.CardPlay.Card.Tags.Contains(WhitneyCardTags.Spark) && e.CardPlay.Card.Owner == card.Owner))
        // ];
        [new BlockVar(6, ValueProp.Move)];

    protected override void OnUpgrade()
    {
        // DynamicVars.CalculationBase.UpgradeValueBy(2);
        // DynamicVars.CalculationExtra.UpgradeValueBy(1);
        DynamicVars.Block.UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card == this && Sparked(card))
        {
            return playCount + 1;
        }
        return base.ModifyCardPlayCount(card, target, playCount);
    }

    private bool Sparked(CardModel card)
    {
        return CombatManager.Instance.History.CardPlaysFinished.Any(e => e.HappenedThisTurn(card.CombatState) &&
                                                                         e.CardPlay.Card.Tags.Contains(WhitneyCardTags.Spark) &&
                                                                         e.CardPlay.Card.Owner == card.Owner);
    }
}