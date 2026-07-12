using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Whitney.WhitneyCode.Powers;

/// <summary>At the start of your turn, gain Ink.</summary>
public sealed class ElementalFormPower : WhitneyPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Elemental Form",
            "At the start of your turn, gain {Amount} [gold]Ink[/gold].",
            "At the start of your turn, gain {Amount} [gold]Ink[/gold].");

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner))
            return;
        if (Owner.Player is null)
            return;

        Flash();
        await Ink.Gain(new ThrowingPlayerChoiceContext(), Owner.Player, Amount);
    }
}
