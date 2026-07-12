using BaseLib.Utils;
using Whitney.WhitneyCode.Events;
using Whitney.WhitneyCode.Powers;
using Whitney.WhitneyCode.PatchesNModels;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace Whitney.WhitneyCode.Relics;

public class MiniHakkero : AbstractWhitneyRelic
{
    // 稀有度
    public override RelicRarity Rarity => RelicRarity.Starter;

    // public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    // {
    //     await CardPileCmd.Draw(choiceContext, 1, player);
    // }
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Power", 1m)
    ];

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner && ( //!MehModConfig.NerfHakkero ||
                Owner.Creature.GetPowerAmount<ChargeUpPower>() < 8))
            await PowerCmd.Apply<ChargeUpPower>(context, Owner.Creature, DynamicVars["Power"].BaseValue, Owner.Creature, null);
        //return base.AfterCardPlayed(context, cardPlay);
    }

    public override RelicModel? GetUpgradeReplacement()
    {
        return ModelDb.Relic<BewitchedHakkero>();
    }

    public override EventModel ModifyNextEvent(EventModel currentEvent)
    {
        if (currentEvent is HungryForMushrooms)
        {
            return ModelDb.Event<HungryForMushroomsWhitney>();
        }

        return currentEvent;
    }
}