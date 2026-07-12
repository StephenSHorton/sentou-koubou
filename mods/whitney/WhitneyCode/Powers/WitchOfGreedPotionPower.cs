using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace Whitney.WhitneyCode.Powers
{
    public class WitchOfGreedPotionPower : AbstractWhitneyPower
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override Task AfterCombatEnd(CombatRoom room)
        {
            if (Owner.Player != null)
                for (var i = 0; i < Amount; i++)
                    room.AddExtraReward(Owner.Player, new PotionReward(Owner.Player));
            return Task.CompletedTask;
        }
    }
}