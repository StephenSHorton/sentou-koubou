using Whitney.WhitneyCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Whitney.WhitneyCode.Cards
{
    public class OrrerysSun : AbstractWhitneyCard
    {
        public OrrerysSun() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new DynamicVar("Power", 6)
        ];

        protected override void OnUpgrade()
        {
            DynamicVars["Power"].UpgradeValueBy(3);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<OrrerysSunPower>(choiceContext, Owner.Creature, DynamicVars["Power"].IntValue, Owner.Creature, this);
        }
    }
}