using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Blake.BlakeCode.Powers;

/// <summary>
/// Localization / hover-tip template for Unleash only.
/// Not applied as a combat buff — used via HoverTipFactory.FromPower.
/// </summary>
public sealed class UnleashPower : BlakePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Unleash",
            "Deal damage equal to your [gold]Charge[/gold], then reset [gold]Charge[/gold] to base.",
            "Deal damage equal to your [gold]Charge[/gold], then reset to base.");
}
