using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Whitney.WhitneyCode.Relics;

public class BigShroomBag : AbstractWhitneyRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2),
        new EnergyVar(1),
        new DynamicVar("Draw", 2),
        new HealVar(3)
    ];
    

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card is SporeMind && cardPlay.Card.Owner == Owner)
        {
            await CardPileCmd.Draw(context, DynamicVars["Draw"].BaseValue, Owner);
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
            await CreatureCmd.Heal(Owner.Creature,DynamicVars.Heal.IntValue);
        }
    }
}