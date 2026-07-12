using Whitney.WhitneyCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Whitney.WhitneyCode.Cards;

public class EnergyFlow : AbstractWhitneyCard
{
    public EnergyFlow() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<EnergyFlowPower>(2)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ChargeUpPower>()
    ];

    protected override void OnUpgrade()
    {
        DynamicVars["EnergyFlowPower"].UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<EnergyFlowPower>(choiceContext, Owner.Creature, DynamicVars["EnergyFlowPower"].BaseValue, Owner.Creature, this);
    }
}