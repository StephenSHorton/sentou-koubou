using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Blake.BlakeCode.Powers;

/// <summary>At the start of your turn, Rev.</summary>
public sealed class GDiffuserPower : BlakePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "G-Diffuser",
            "At the start of your turn, [gold]Rev[/gold] (double your Charge).",
            "At the start of your turn, [gold]Rev[/gold] {Amount} time(s).");

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (Owner.Player is null) return;
        if (side != Owner.Side) return;
        if (!participants.Contains(Owner)) return;

        Charge.OnPlayerTurnStart(Owner.Player);
        Flash();
        var times = Math.Max(1, Amount);
        await Charge.Rev(new ThrowingPlayerChoiceContext(), Owner.Player, times);
    }
}
