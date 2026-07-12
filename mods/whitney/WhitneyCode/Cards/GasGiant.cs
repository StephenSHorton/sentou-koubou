using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace Whitney.WhitneyCode.Cards;

public class GasGiant : AbstractWhitneyCard
{
    public GasGiant() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }
    

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(14,ValueProp.Move)
    ];

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await CardPileCmd.AddGeneratedCardToCombat(CombatState!.CreateCard<Burn>(Owner), PileType.Hand, Owner);
    }
}