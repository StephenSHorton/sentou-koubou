using Whitney.WhitneyCode.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Whitney.WhitneyCode.Cards
{
    public class PolarisUnique : AbstractWhitneyCard
    {
        public PolarisUnique() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new EnergyVar(1),
            new CardsVar(1)
        ];

        protected override void OnUpgrade()
        {
            //DynamicVars.Energy.UpgradeValueBy(1);
            DynamicVars.Cards.UpgradeValueBy(1);
        }

        protected override bool IsPlayable =>
            !CombatManager.Instance.History.CardPlaysFinished.Any(e => e.HappenedThisTurn(CombatState) && e.CardPlay.Card is PolarisUnique && e.CardPlay.Card.Owner == Owner);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            var card = (await CardSelectCmd.FromHand(choiceContext,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1, 1),
                card => card.EnergyCost.Canonical != -1,
                this)).FirstOrDefault();
            card?.EnergyCost.AddThisCombat(1);
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
            //EnergyCost.AddThisCombat(1);
            // await PowerCmd.Apply<PolarisUniquePower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        }
    }
}