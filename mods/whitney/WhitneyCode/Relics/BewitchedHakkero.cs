using Whitney.WhitneyCode.Events;
using Whitney.WhitneyCode.PatchesNModels;
using Whitney.WhitneyCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace Whitney.WhitneyCode.Relics;

public class BewitchedHakkero : AbstractWhitneyRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Power", 1m),
        new DynamicVar("PowerAmp", 2)
    ];


    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner)
        {
            return;
        }

        var cnt = DynamicVars["Power"].IntValue;
        if (cardPlay.Card.Tags.Contains(WhitneyCardTags.Spark))
            cnt = DynamicVars["PowerAmp"].IntValue;
        // if (cardPlay.Card.Type == CardType.Attack) // && !MehModConfig.NerfHakkero)
        //     cnt = DynamicVars["PowerAmp"].IntValue;
        await PowerCmd.Apply<ChargeUpPower>(context, Owner.Creature, cnt, Owner.Creature, null);
        //return base.AfterCardPlayed(context, cardPlay);

        // var newRec = Owner.PlayerCombatState!.ExhaustPile.Cards.Count;
        // if (newRec > _rec)
        // {
        //     await PowerCmd.Apply<ChargeUpPower>(context, Owner.Creature, cnt, Owner.Creature, null);
        // }
    }

    // private int _rec;
    //
    // public override Task BeforeCardPlayed(CardPlay cardPlay)
    // {
    //     if (cardPlay.Card.Owner == Owner)
    //     {
    //         _rec = Owner.PlayerCombatState!.ExhaustPile.Cards.Count;
    //     }
    //
    //     return Task.CompletedTask;
    // }

    // public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    // {
    //     //return base.AfterCardExhausted(choiceContext, card, causedByEthereal);
    //     if (card.Owner == Owner)
    //     {
    //         var cnt = DynamicVars["Power"].IntValue;
    //         await PowerCmd.Apply<ChargeUpPower>(choiceContext, Owner.Creature, cnt, Owner.Creature, null);
    //     }
    // }

    public override EventModel ModifyNextEvent(EventModel currentEvent)
    {
        if (currentEvent is HungryForMushrooms)
        {
            return ModelDb.Event<HungryForMushroomsWhitney>();
        }

        return currentEvent;
    }
}