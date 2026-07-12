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

namespace Whitney.WhitneyCode.Cards.Basic;

public sealed class ChannelInk() : WhitneyCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Wind;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<InkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Ink", 2), new CardsVar(0)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await Ink.Gain(choiceContext, Owner, DynamicVars["Ink"].IntValue, this);
        if (DynamicVars.Cards.BaseValue > 0)
            await CardPileCmd.Draw(choiceContext, (int)DynamicVars.Cards.BaseValue, Owner);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
