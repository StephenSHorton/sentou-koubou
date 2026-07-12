using Whitney.Scenes.Vfx.FinalSpark;
using Whitney.WhitneyCode.Character;
using Whitney.WhitneyCode.PatchesNModels;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace Whitney.WhitneyCode.Cards;

public class FinalSpark : AbstractWhitneyCard
{
    public FinalSpark() : base(7, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(40, ValueProp.Move),
        new EnergyVar(1)
    ];

    protected override HashSet<CardTag> CanonicalTags => [WhitneyCardTags.Spark];

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(10);
    }

    //private int _sparkCount;

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (!cardPlay.Card.Tags.Contains(WhitneyCardTags.Spark) || cardPlay.Card == this || cardPlay.Card.Owner != Owner)
            return Task.CompletedTask;
        //_sparkCount++;
        EnergyCost.AddThisCombat(-DynamicVars.Energy.IntValue);
        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(CombatState!)
            .WithHitFx("vfx/vfx_attack_slash").WithAttackerAnim(Owner.Character is WhitneyCharacter ? "Spark" : "Cast", 0.3f)
            .BeforeDamage(async delegate
            {
                List<Creature> enemies = CombatState!.Enemies.Where(e => e.IsAlive).ToList();
                var vfx = VfxFinalSpark.Create(Owner.Creature, enemies.Last());
                if (vfx != null)
                {
                    vfx.ApplySizeFromDamage((int)DynamicVars.Damage.PreviewValue,40);
                    vfx.SetRainbowRatio(0.05f);
                    NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx);
                    await Cmd.Wait(VfxFinalSpark.VfxTime);
                }

                foreach (Creature item in enemies)
                {
                    var nHyperbeamImpactVfx = NHyperbeamImpactVfx.Create(Owner.Creature, item);
                    if (nHyperbeamImpactVfx != null)
                    {
                        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(nHyperbeamImpactVfx);
                    }
                }
            })
            .Execute(choiceContext);
        //EnergyCost.AddThisCombat(_sparkCount);
        //_sparkCount = 0;
    }
}