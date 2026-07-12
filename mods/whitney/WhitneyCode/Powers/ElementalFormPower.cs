using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Whitney.WhitneyCode.Powers;

/// <summary>
/// Cards count as every element for Blend / Masterwork while this power is active.
/// </summary>
public sealed class ElementalFormPower : WhitneyPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Elemental Form",
            "Your cards count as every element for [gold]Blend[/gold] and [gold]Masterwork[/gold].",
            "Your cards count as every element for [gold]Blend[/gold] and [gold]Masterwork[/gold].");

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner.Player is not null)
            WhitneyBrush.SetAllElementsMode(Owner.Player, true);
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature? oldOwner)
    {
        if (oldOwner?.Player is not null)
            WhitneyBrush.SetAllElementsMode(oldOwner.Player, false);
        return Task.CompletedTask;
    }
}
