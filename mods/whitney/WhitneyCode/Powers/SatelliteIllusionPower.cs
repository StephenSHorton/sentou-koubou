using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Whitney.WhitneyCode.Powers
{
    public class SatelliteIllusionPower : AbstractWhitneyPower
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        private int _rec;

        public void SetStartingRec(int count)
        {
            _rec = count;
        }

        public override Task BeforeCardPlayed(CardPlay cardPlay)
        {
            if (cardPlay.Card.Owner == Owner.Player)
            {
                _rec = Owner.Player.PlayerCombatState!.DrawPile.Cards.Count;
            }

            return Task.CompletedTask;
        }

        public override async Task AfterCardPlayedLate(PlayerChoiceContext context, CardPlay cardPlay)
        {
            if (cardPlay.Card.Owner == Owner.Player)
            {
                var cnt = Owner.Player.PlayerCombatState!.DrawPile.Cards.Count;
                if (cnt > _rec && _rec > 0)
                {
                    await PlayerCmd.GainEnergy(Amount, Owner.Player);
                }
            }
        }

        public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
        {
            if (shuffler == Owner.Player)
            {
                await PlayerCmd.GainEnergy(Amount, Owner.Player);
                _rec = -1;
            }
        }
    }
}