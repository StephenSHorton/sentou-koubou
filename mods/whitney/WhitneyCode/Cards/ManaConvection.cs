using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Whitney.WhitneyCode.Cards
{
    public class ManaConvection : AbstractWhitneyCard
    {
        public ManaConvection() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new CardsVar(2),
            new EnergyVar(1),
            new DynamicVar("Draw", 1)
        ];

        public override IEnumerable<CardKeyword> CanonicalKeywords => base.CanonicalKeywords.Concat([
            CardKeyword.Exhaust
        ]);

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [
            HoverTipFactory.FromCard<Burn>()
        ];

        protected override void OnUpgrade()
        {
            //DynamicVars.Cards.UpgradeValueBy(1);
            EnergyCost.UpgradeBy(-1);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            var draw = 0; //false;
            foreach (CardModel item in
                     await CardSelectCmd.FromHand(prefs: new CardSelectorPrefs(SelectionScreenPrompt, 0, DynamicVars.Cards.IntValue), context: choiceContext, player: Owner, filter: null, source: this))
            {
                await CardCmd.Exhaust(choiceContext, item);
                await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
                //if (item.DeckVersion == null)
                if (item is Burn)
                {
                    draw += DynamicVars["Draw"].IntValue;
                }
            }

            if (draw > 0)
                await CardPileCmd.Draw(choiceContext, draw, Owner);
        }
    }
}