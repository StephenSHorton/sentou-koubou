using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Whitney.WhitneyCode.Powers;

namespace Whitney.WhitneyCode.Cards.Uncommon;

/// <summary>Fire + Water confluence — AoE dmg + Weak ALL + gain Ink.</summary>
public sealed class SteamBurst() : WhitneyCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<InkPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7, ValueProp.Move),
        new DynamicVar("Weak", 1),
        new DynamicVar("Ink", 1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        var combat = Owner.Creature?.CombatState;
        if (combat is not null)
        {
            foreach (var enemy in combat.HittableEnemies)
            {
                await PowerCmd.Apply<WeakPower>(
                    choiceContext,
                    enemy,
                    DynamicVars["Weak"].IntValue,
                    Owner.Creature,
                    this);
            }
        }
        await Ink.Gain(choiceContext, Owner, DynamicVars["Ink"].IntValue, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
