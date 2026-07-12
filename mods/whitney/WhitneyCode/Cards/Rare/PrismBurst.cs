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
/// Energy X-cost: gain X Attunement, then deal damage equal to (Damage + Attunement) to ALL enemies.
/// Spend everything — paint the room in one stroke.
/// </summary>
public sealed class PrismBurst() : WhitneyCard(-1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    public override WhitneyElement Element => WhitneyElement.Fire;

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [WhitneyTips.Attunement];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(4, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var x = EnergyCost.CapturedXValue;
        if (x <= 0)
            x = Owner.PlayerCombatState?.Energy ?? 0;

        if (x > 0 && Owner.Creature is not null)
        {
            await PowerCmd.Apply<AttunementPower>(
                choiceContext, Owner.Creature, x, Owner.Creature, this);
        }

        var attune = Owner.Creature?.GetPower<AttunementPower>()?.Amount ?? 0;
        var dmg = DynamicVars.Damage.BaseValue + attune;
        if (Owner.Creature?.CombatState is not null && dmg > 0)
        {
            await CreatureCmd.Damage(
                choiceContext,
                Owner.Creature.CombatState.HittableEnemies,
                dmg,
                ValueProp.Move,
                Owner.Creature,
                this);
        }
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
