using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Whitney.WhitneyCode.Cards
{
    public class PowerUp : AbstractWhitneyCard
    {
        public PowerUp() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new DynamicVar("Add", 2)
        ];

        protected override void OnUpgrade()
        {
            DynamicVars["Add"].UpgradeValueBy(1);
        }

        protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            foreach (CardModel card in Owner.PlayerCombatState!.Hand.Cards.Where(x => x.Type == CardType.Attack).ToArray())
            {
                UpgradeCardDamage(card, DynamicVars["Add"].BaseValue);
            }

            return Task.CompletedTask;
        }

        public static void UpgradeCardDamage(CardModel card, decimal amount)
        {
            if (card.DynamicVars.ContainsKey("CalculatedDamage"))
            {
                card.DynamicVars.CalculationBase.UpgradeValueBy(amount);
            }
            // else if (card.DynamicVars.ContainsKey("Damage"))
            // {
            //     card.DynamicVars.Damage.UpgradeValueBy(amount);
            // }

            foreach (var dynamicVar in card.DynamicVars)
            {
                if (dynamicVar.Value is DamageVar)
                {
                    dynamicVar.Value.UpgradeValueBy(amount);
                }
            }

            if (card is AbstractWhitneyCard mCard)
            {
                mCard.DoFlash();
            }
            // if (card.DynamicVars.ContainsKey("DamageAmplified"))
            // {
            //     card.DynamicVars["DamageAmplified"].UpgradeValueBy(amount);
            // }
        }
    }
}