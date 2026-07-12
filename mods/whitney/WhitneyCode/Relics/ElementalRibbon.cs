using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Whitney.WhitneyCode;

namespace Whitney.WhitneyCode.Relics;

/// <summary>Play a Power → gain Ink.</summary>
public sealed class ElementalRibbon : WhitneyRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Ink", 1)];

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner) return;
        if (cardPlay.Card.Type != CardType.Power) return;
        Flash();
        await Ink.Gain(choiceContext, Owner, DynamicVars["Ink"].IntValue);
    }
}
