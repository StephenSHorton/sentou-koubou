using Whitney.WhitneyCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Whitney.WhitneyCode.Cards
{
    public class PulseMagic : AbstractAmplifiedCard
    {
        public PulseMagic() : base(0, 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            base.CanonicalVars.Concat([
                new EnergyVar("GainEnergy", 1)
            ]);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await base.OnPlay(choiceContext, cardPlay);
            await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, Owner.Creature, DynamicVars["GainEnergy"].BaseValue, Owner.Creature, this);
            if (AmplifiedInPlay)
            {
                await PowerCmd.Apply<PulseMagicPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars["GainEnergy"].UpgradeValueBy(1);
        }
    }
}