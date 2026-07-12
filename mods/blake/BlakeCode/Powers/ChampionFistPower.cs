using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Blake.BlakeCode.Powers;

/// <summary>
/// Raises base Charge by Amount (now and after every Unleash reset).
/// Applied amount is the base increase; on apply we also bump current Charge if below new floor.
/// </summary>
public sealed class ChampionFistPower : BlakePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Champion's Fist",
            "Increase your base [gold]Charge[/gold] by {Amount}.",
            "Your base [gold]Charge[/gold] is increased by {Amount}.");

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner.Player is null) return;
        Charge.AddBase(Owner.Player, Amount);

        var ctx = new ThrowingPlayerChoiceContext();
        await Charge.Ensure(ctx, Owner.Player, cardSource);
        var power = Owner.GetPower<ChargePower>();
        if (power is not null && power.Amount < Charge.GetBase(Owner.Player))
        {
            var delta = Charge.GetBase(Owner.Player) - power.Amount;
            await PowerCmd.ModifyAmount(ctx, power, delta, Owner, cardSource);
        }
    }
}
