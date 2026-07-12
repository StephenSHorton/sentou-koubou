using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using Whitney.WhitneyCode.Cards.Colorless;
using Whitney.WhitneyCode.Powers;

namespace Whitney.WhitneyCode.Cards
{
    public class CasketOfStar : AbstractWhitneyCard
    {
        public CasketOfStar() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
        {
        }

        //public override string PortraitPath => $"res://img/cards/CasketOfStar_p.png";

        protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Spark>(IsUpgraded)];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (IsUpgraded)
            {
                await PowerCmd.Apply<CasketOfStarPlusPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
            }
            else
            {
                await PowerCmd.Apply<CasketOfStarPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
            }
        }
    }
}