using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Brennen.BrennenCode.Cards.Uncommon;

/// <summary>Silence chat. Apply Weak to ALL enemies.</summary>
public sealed class MuteAll() : BrennenCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
{
    private int WeakAmount => IsUpgraded ? 3 : 2;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WeakPower>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var combat = Owner.Creature?.CombatState;
        if (combat is null) return;

        foreach (var enemy in combat.HittableEnemies)
        {
            await PowerCmd.Apply<WeakPower>(
                choiceContext,
                enemy,
                WeakAmount,
                Owner.Creature,
                this);
        }
    }
}
