using Whitney.WhitneyCode.Enchantments;
using Whitney.WhitneyCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Whitney.WhitneyCode.Cards;

public class Walpurgisnacht : AbstractAmplifiedCard //AbstractWhitneyCard
{
    public Walpurgisnacht() : base(3, 1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    //public override string PortraitPath => "res://Whitney/images/cards/whitney-test_whitney_card.png";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Concat(
        [
            new DynamicVar("Power", 1),
            new DynamicVar("PowerAmp", 2)
        ]);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromEnchantment<InkboundEnchantment>().Concat([HoverTipFactory.FromPower<InkboundPower>()]);

    protected override void OnUpgrade()
    {
        //DynamicVars["PowerAmp"].UpgradeValueBy(1);
        AddKeyword(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.OnPlay(choiceContext, cardPlay);
        await PowerCmd.Apply<WalpurgisnachtPower>(choiceContext, Owner.Creature, DynamicVars["Power"].IntValue, Owner.Creature, this);
        if (AmplifiedInPlay)
        {
            await PowerCmd.Apply<WalpurgisnachtAmpPower>(choiceContext, Owner.Creature, DynamicVars["PowerAmp"].IntValue, Owner.Creature, this);
        }
    }
}