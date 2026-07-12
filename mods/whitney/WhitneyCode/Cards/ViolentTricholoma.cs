using Whitney.WhitneyCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Whitney.WhitneyCode.Cards;

public class ViolentTricholoma : AbstractWhitneyCard
{
    public ViolentTricholoma() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    //public override string PortraitPath => "res://Whitney/images/cards/whitney-test_whitney_card.png";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2),
        new PowerVar<ChargeUpPower>(2)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ChargeUpPower>()
    ];

    protected override void OnUpgrade()
    {
        //DynamicVars["ChargeUpPower"].UpgradeValueBy(2);
        DynamicVars.Cards.UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        await PowerCmd.Apply<ChargeUpPower>(choiceContext, Owner.Creature, DynamicVars["ChargeUpPower"].BaseValue, Owner.Creature, this);
    }
}