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


namespace Whitney.WhitneyCode.Cards.Common;

public sealed class Zephyr() : WhitneyCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Wind;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [WhitneyTips.Blend, WhitneyTips.Ink];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new DynamicVar("Ink", 1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var blend = IsBlendActive;
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        if (blend)
            await Ink.Gain(choiceContext, Owner, DynamicVars["Ink"].IntValue, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars["Ink"].UpgradeValueBy(1m);
}
