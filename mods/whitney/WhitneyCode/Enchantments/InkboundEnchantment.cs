using Godot;
using Whitney.WhitneyCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Whitney.WhitneyCode.Enchantments;

public class InkboundEnchantment : AbstractWhitneyEnchantment
{
    //public int AmplifyCost = 0;
    private const int SingleGainCeiling = 9999;

    public override bool CanEnchant(CardModel card)
    {
        return card.Enchantment == null && !card.Keywords.Contains(CardKeyword.Unplayable);
    }

    /*
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card == Card)
        {
            AmplifyCost = 0;
        }

        return Task.CompletedTask;
    }
*/
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var amt = cardPlay.Resources.EnergySpent;//+AmplifyCost
        var amtFin = Mathf.RoundToInt(Mathf.Pow(2, amt));
        if (amtFin > SingleGainCeiling)
            amtFin = SingleGainCeiling;
        if (cardPlay.Card == Card && amtFin > 0)
        {
            await PowerCmd.Apply<InkboundPower>(context, Card.Owner.Creature, amtFin, Card.Owner.Creature, Card);
        }
    }
}