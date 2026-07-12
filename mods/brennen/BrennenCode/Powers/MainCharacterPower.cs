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

/// <summary>While Tilted, gain 1 Energy at start of turn.</summary>
public sealed class MainCharacterPower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Main Character",
            "While [gold]Tilted[/gold], gain 1 Energy at the start of your turn.",
            "While [gold]Tilted[/gold], gain 1 Energy at the start of your turn.");

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner))
            return;
        if (Owner.Player is null || !Tilted.IsTilted(Owner.Player))
            return;
        Flash();
        await PlayerCmd.GainEnergy(1, Owner.Player);
    }
}
