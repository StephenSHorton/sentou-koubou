using Whitney.WhitneyCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Whitney.WhitneyCode.Cards
{
    public class SatelliteIllusion : AbstractWhitneyCard
    {
        public SatelliteIllusion() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new EnergyVar(1)
        ];

        protected override void OnUpgrade()
        {
            AddKeyword(CardKeyword.Innate);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            var pow = await PowerCmd.Apply<SatelliteIllusionPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
            pow?.SetStartingRec(Owner.PlayerCombatState!.DrawPile.Cards.Count);
        }
    }
}