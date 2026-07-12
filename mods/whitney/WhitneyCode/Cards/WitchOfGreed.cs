using Whitney.WhitneyCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Whitney.WhitneyCode.Cards
{
    public class WitchOfGreed : AbstractAmplifiedCard
    {
        public WitchOfGreed() : base(1, 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Concat([
            new GoldVar(15),
            new DynamicVar("Potion", 1)
        ]);

        public override bool CanBeGeneratedInCombat => false;

        protected override void OnUpgrade()
        {
            DynamicVars.Gold.UpgradeValueBy(10);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await base.OnPlay(choiceContext, cardPlay);
            await PowerCmd.Apply<RoyaltiesPower>(choiceContext, Owner.Creature, DynamicVars.Gold.BaseValue, Owner.Creature, this);
            if (AmplifiedInPlay)
            {
                await PowerCmd.Apply<WitchOfGreedPotionPower>(choiceContext, Owner.Creature, DynamicVars["Potion"].BaseValue, Owner.Creature, this);
            }
        }
    }
}