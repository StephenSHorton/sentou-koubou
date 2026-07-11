using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Whitney.WhitneyCode.Powers;

namespace Whitney.WhitneyCode.Cards.Rare;

/// <summary>Ultimate ink spend — AoE + dual debuffs.</summary>
public sealed class CataclysmSeal() : WhitneyCard(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override int InkCost => 5;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<InkPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(14, ValueProp.Move),
        new DynamicVar("Weak", 2),
        new DynamicVar("Vulnerable", 2),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (!await Ink.TrySpend(choiceContext, Owner, InkCost, this))
            return;

        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        var combat = Owner.Creature?.CombatState;
        if (combat is null)
            return;
        foreach (var enemy in combat.HittableEnemies)
        {
            await PowerCmd.Apply<WeakPower>(
                choiceContext, enemy, DynamicVars["Weak"].IntValue, Owner.Creature, this);
            await PowerCmd.Apply<VulnerablePower>(
                choiceContext, enemy, DynamicVars["Vulnerable"].IntValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
