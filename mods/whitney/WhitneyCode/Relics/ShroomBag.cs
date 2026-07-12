using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Whitney.WhitneyCode.Relics;

public class ShroomBag : AbstractWhitneyRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2),
        new EnergyVar(1),
        new DynamicVar("Draw", 1),
        new HealVar(2)
    ];

    public override async Task AfterObtained()
    {
        var list = new List<CardPileAddResult>();
        for (var i = 0; i < DynamicVars.Cards.IntValue; i++)
            list.Add(await CardPileCmd.Add(base.Owner.RunState.CreateCard(ModelDb.Card<SporeMind>(), Owner), PileType.Deck));
        CardCmd.PreviewCardPileAdd(list, 2f);
    }

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