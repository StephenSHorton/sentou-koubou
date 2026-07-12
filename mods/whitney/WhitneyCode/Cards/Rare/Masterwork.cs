using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Whitney.WhitneyCode;
using Whitney.WhitneyCode.Powers;


namespace Whitney.WhitneyCode.Cards.Rare;

public sealed class Masterwork() : WhitneyCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Earth;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<MasterworkPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner.Creature is not null)
            await PowerCmd.Apply<MasterworkPower>(
                choiceContext, Owner.Creature, 1, Owner.Creature, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
