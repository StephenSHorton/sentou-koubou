using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;
using System.Threading.Tasks;

namespace Brennen.BrennenCode.Powers;

/// <summary>
/// Solo tank risk engine: take double attack damage; gain Block at start of turn.
/// (Co-op half-ally package remains Frontline / TankPower.)
/// </summary>
public sealed class MainTankPower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Main Tank",
            "You take double damage from attacks. At the start of your turn, gain {Amount} [gold]Block[/gold].",
            "You take double damage from attacks. At the start of your turn, gain {Amount} [gold]Block[/gold].");

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner)
            return amount;
        // Double incoming attack/move damage (mirror TankPower self-side).
        if ((props & ValueProp.Move) == 0)
            return amount;
        if ((props & ValueProp.Unpowered) != 0)
            return amount;

        return amount * 2m;
    }

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner))
            return;

        Flash();
        await CreatureCmd.GainBlock(
            Owner,
            Amount,
            ValueProp.Unpowered,
            null,
            false);
    }
}
