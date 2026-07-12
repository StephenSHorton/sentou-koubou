using BaseLib.Utils;
using Whitney.WhitneyCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Whitney.WhitneyCode.Cards.Colorless
{

    [Pool(typeof(TokenCardPool))]
    public class Exhaustion : AbstractWhitneyCard
    {
        public Exhaustion() : base(-1, CardType.Status, CardRarity.Status, TargetType.None)
        {
        }

        public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

        public override int MaxUpgradeLevel => 0;


        public override decimal ModifyPowerAmountGivenMultiplicative(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
        {
            if (power is ChargeUpPower && target == Owner.Creature && Owner.PlayerCombatState != null && Owner.PlayerCombatState.Hand.Cards.Contains(this))
            {
                return 0;
            }
            return base.ModifyPowerAmountGivenMultiplicative(power, giver, amount, target, cardSource);
        }
    }
}