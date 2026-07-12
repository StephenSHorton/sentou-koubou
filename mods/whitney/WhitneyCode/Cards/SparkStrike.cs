using Whitney.Scenes.Vfx.HitVfx;
using Whitney.WhitneyCode.PatchesNModels;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace Whitney.WhitneyCode.Cards
{
    public class SparkStrike : AbstractWhitneyCard
    {
        public SparkStrike() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
        {
        }

        protected override HashSet<CardTag> CanonicalTags =>
        [
            CardTag.Strike,
            WhitneyCardTags.Spark
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
                //.WithHitFx("vfx/vfx_attack_slash")
                .WithHitVfxNode((Creature t) => SparkHitVfx.Create(NCombatRoom.Instance?.GetCreatureNode(t)!,"BurstSpark"))
                .Execute(choiceContext);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(3m);
        }
    }
}