using Whitney.WhitneyCode.Cards.Colorless;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Whitney.WhitneyCode.Powers
{
    public class CasketOfStarPower : AbstractWhitneyPower
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public const int Threshold = 3, DeadlockLimit = 5;

        public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props,
            CardModel? cardSource)
        {
            if (amount >= Threshold && creature == Owner && Owner.Player != null && _deadlockCounter < DeadlockLimit)
            {
                _deadlockCounter++;
                await Spark.CreateInHand(Owner.Player, Amount, CombatState);
            }
        }

        private int _deadlockCounter = 0;

        public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Card.Owner == Owner.Player)
            {
                _deadlockCounter = 0;
            }

            return base.AfterCardPlayed(choiceContext, cardPlay);
        }

        // public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
        // {
        //     foreach (var allCard in Owner.Player!.PlayerCombatState!.AllCards.Where(x => x is Spark))
        //     {
        //         await ApplyRetain(allCard);
        //     }
        // }
        //
        // public override async Task AfterCardEnteredCombat(CardModel card)
        // {
        //     if (card.Owner == Owner.Player && card is Spark)
        //     {
        //         await ApplyRetain(card);
        //     }
        // }

        // public static Task ApplyRetain(CardModel card)
        // {
        //     if (!card.Keywords.Contains(CardKeyword.Retain))
        //     {
        //         CardCmd.ApplyKeyword(card, CardKeyword.Retain);
        //     }
        //
        //     return Task.CompletedTask;
        // }
    }
}