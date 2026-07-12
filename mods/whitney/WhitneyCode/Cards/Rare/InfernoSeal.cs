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

public sealed class InfernoSeal() : WhitneyCard(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    public override WhitneyElement Element => WhitneyElement.Fire;
    protected override int SealCost => 4;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        WhitneyTips.Ink,
        WhitneyTips.Vulnerable,
        WhitneyTips.Weak,
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(15, ValueProp.Move),
        new DynamicVar("Vulnerable", 2),
        new DynamicVar("Weak", 2),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (Owner.Creature?.CombatState is not null)
        {
            foreach (var enemy in Owner.Creature.CombatState.HittableEnemies)
            {
                await PowerCmd.Apply<VulnerablePower>(
                    choiceContext, enemy, DynamicVars["Vulnerable"].IntValue, Owner.Creature, this);
                await PowerCmd.Apply<WeakPower>(
                    choiceContext, enemy, DynamicVars["Weak"].IntValue, Owner.Creature, this);
            }
        }
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5m);
}
