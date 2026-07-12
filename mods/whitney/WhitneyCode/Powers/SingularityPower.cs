using Whitney.WhitneyCode.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Whitney.WhitneyCode.Powers
{
    public class SingularityPower : AbstractWhitneyPower
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
        {
            if (cardPlay.Card.Owner == Owner.Player && cardPlay.Resources.EnergySpent == 0)
            {
                var card = Owner.Player.PlayerCombatState!.Hand.Cards.Where(x => x.Type == CardType.Attack).TakeRandom(1, Owner.Player.RunState.Rng.CombatCardSelection).FirstOrDefault();
                if (card != null)
                {
                    Flash();
                    // if (card is AbstractWhitneyCard whitneyCard)
                    //     _ = whitneyCard.DoFlash();
                    // if (card.DynamicVars.ContainsKey("CalculatedDamage"))
                    // {
                    //     card.DynamicVars.CalculationBase.UpgradeValueBy(Amount);
                    // }
                    // else if (card.DynamicVars.ContainsKey("Damage"))
                    // {
                    //     card.DynamicVars.Damage.UpgradeValueBy(Amount);
                    // }
                    PowerUp.UpgradeCardDamage(card, Amount);
                }
            }

            return Task.CompletedTask;
        }
    }
}