using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Brennen.BrennenCode.Powers;

/// <summary>Start of turn: Weak ALL enemies. Moderated.</summary>
public sealed class ChatModPower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Chat Mod",
            "At the start of your turn, apply {Amount} [gold]Weak[/gold] to ALL enemies.",
            "At the start of your turn, apply {Amount} [gold]Weak[/gold] to ALL enemies.");

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>()];

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner))
            return;

        Flash();
        var ctx = new ThrowingPlayerChoiceContext();
        foreach (var enemy in combatState.HittableEnemies)
        {
            await PowerCmd.Apply<WeakPower>(ctx, enemy, Amount, Owner, null);
        }
    }
}
