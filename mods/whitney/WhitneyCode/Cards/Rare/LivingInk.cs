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

public sealed class LivingInk() : WhitneyCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Water;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<LivingInkPower>(),
        HoverTipFactory.FromPower<InkPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Ink", 2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner.Creature is not null)
            await PowerCmd.Apply<LivingInkPower>(
                choiceContext, Owner.Creature, DynamicVars["Ink"].IntValue, Owner.Creature, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars["Ink"].UpgradeValueBy(1m);
}
