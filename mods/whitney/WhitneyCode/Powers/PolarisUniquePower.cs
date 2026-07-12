using Whitney.WhitneyCode.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Whitney.WhitneyCode.Powers
{
    public class PolarisUniquePower : AbstractWhitneyPower
    {
        public override PowerType Type => PowerType.Debuff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
        {
            if (card is not PolarisUnique || card.Owner != Owner.Player)
            {
                modifiedCost = originalCost;
                return false;
            }

            modifiedCost = originalCost + Amount;
            return true;
        }
    }
}