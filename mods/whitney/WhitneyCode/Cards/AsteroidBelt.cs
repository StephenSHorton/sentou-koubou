using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Whitney.WhitneyCode.Cards
{
    public class AsteroidBelt : AbstractAmplifiedCard
    {
        public AsteroidBelt() : base(1, 1, CardType.Skill, CardRarity.Common, TargetType.Self)
        {
        }

        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            base.CanonicalVars.Concat(
            [
                new BlockVar(9m, ValueProp.Move)
            ]);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await base.OnPlay(choiceContext, cardPlay);
            var amount = await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            if (AmplifiedInPlay)
                await PowerCmd.Apply<BlockNextTurnPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(3m);
        }
    }
}