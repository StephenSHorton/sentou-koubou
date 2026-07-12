using Whitney.WhitneyCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Whitney.WhitneyCode.Cards;

public class OrrerysUniverse : AbstractWhitneyCard
{
    // public OrrerysGalaxy() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    // {
    // }
    //
    // protected override IEnumerable<DynamicVar> CanonicalVars =>
    // [
    //     new DynamicVar("Mult", 2)
    // ];
    //
    // //public override string PortraitPath => "res://Whitney/images/cards/whitney-test_whitney_card.png";
    //
    // protected override void OnUpgrade()
    // {
    //     DynamicVars["Mult"].UpgradeValueBy(1);
    // }
    //
    // protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    // {
    //     var amt = Owner.Creature.GetPowerAmount<InkboundPower>();
    //     if (amt > 0)
    //     {
    //         amt *= DynamicVars["Mult"].IntValue - 1;
    //         await PowerCmd.Apply<InkboundPower>(Owner.Creature, amt, Owner.Creature, this);
    //     }
    // }
    public OrrerysUniverse() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        // new("PowerDamage", 1),
        // new("PowerBlock", 1),
        new("Power",1)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<InkboundPower>()
    ];

    protected override void OnUpgrade()
    {
        //DynamicVars["PowerDamage"].UpgradeValueBy(1);
        //DynamicVars["Power"].UpgradeValueBy(1);
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        //await PowerCmd.Apply<OrrerysGalaxyPower>(choiceContext, Owner.Creature, DynamicVars["Power"].IntValue, Owner.Creature, this);
         await PowerCmd.Apply<OrrerysUniversePower>(choiceContext, Owner.Creature, DynamicVars["Power"].IntValue, Owner.Creature, this);
        // await PowerCmd.Apply<OrrerysGalaxyBlockPower>(choiceContext, Owner.Creature, DynamicVars["PowerBlock"].IntValue, Owner.Creature, this);
        // await PowerCmd.Apply<OrrerysGalaxyDamagePower>(choiceContext, Owner.Creature, DynamicVars["PowerDamage"].IntValue, Owner.Creature, this);
    }
}