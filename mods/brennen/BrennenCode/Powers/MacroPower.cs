using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Brennen.BrennenCode.Powers;

/// <summary>Start of turn draw. Play the map.</summary>
public sealed class MacroPower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Macro",
            "At the start of your turn, draw {Amount} card(s).",
            "At the start of your turn, draw {Amount} card(s).");

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner))
            return;
        if (Owner.Player == null)
            return;

        Flash();
        await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), Amount, Owner.Player);
    }
}
