using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Whitney.WhitneyCode.Powers;

namespace Whitney.WhitneyCode.Cards.Basic;

/// <summary>Venerate analogue — fill the ink pot.</summary>
public sealed class ChannelInk() : WhitneyCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<InkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Ink", 2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await Ink.Gain(choiceContext, Owner, DynamicVars["Ink"].IntValue, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Ink"].UpgradeValueBy(1m);
    }
}
