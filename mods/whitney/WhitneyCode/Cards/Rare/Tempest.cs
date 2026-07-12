using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Whitney.WhitneyCode;
using Whitney.WhitneyCode.Powers;

namespace Whitney.WhitneyCode.Cards.Rare;

/// <summary>
/// Energy X-cost: deal damage to a random enemy X times (Whirlwind-style).
/// </summary>
public sealed class Tempest() : WhitneyCard(-1, CardType.Attack, CardRarity.Rare, TargetType.RandomEnemy)
{
    public override WhitneyElement Element => WhitneyElement.Wind;

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hits = EnergyCost.CapturedXValue;
        if (hits <= 0)
            hits = Owner.PlayerCombatState?.Energy ?? 0;

        for (var i = 0; i < hits; i++)
        {
            var combat = Owner.Creature?.CombatState;
            if (combat is null)
                break;
            var enemies = combat.HittableEnemies.ToList();
            if (enemies.Count == 0)
                break;
            var target = enemies[System.Random.Shared.Next(enemies.Count)];
            await CreatureCmd.Damage(
                choiceContext,
                target,
                DynamicVars.Damage.BaseValue,
                ValueProp.Move,
                Owner.Creature,
                this);
        }
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}
