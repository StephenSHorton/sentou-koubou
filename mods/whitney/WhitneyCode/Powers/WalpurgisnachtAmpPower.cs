using Whitney.WhitneyCode.Character;
using Whitney.WhitneyCode.Enchantments;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Whitney.WhitneyCode.Powers;

public class WalpurgisnachtAmpPower : AbstractWhitneyPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        var enchant = ModelDb.Enchantment<InkboundEnchantment>().ToMutable();
        var cards = player.PlayerCombatState!.Hand.Cards.Where(x => enchant.CanEnchant(x)).ToList();
        if (cards.Count > Amount)
        {
            cards = cards.TakeRandom(Amount, Owner.Player.RunState.Rng.CombatCardSelection).ToList();
        }

        foreach (var card in cards)
        {
            Flash();
            //Whitney.Enchant(enchant, card);
            CardCmd.Enchant(enchant, card, 1);
            await Cmd.Wait(0.2f);
            enchant = ModelDb.Enchantment<InkboundEnchantment>().ToMutable();
        }
    }

    // public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    // {
    //     if (cardPlay.Card.Owner == Owner.Player && cardPlay.Card.Enchantment != null)
    //     {
    //         await PowerCmd.Apply<EnergyNextTurnPower>(Owner, 1, Owner, null);
    //     }
    // }
}