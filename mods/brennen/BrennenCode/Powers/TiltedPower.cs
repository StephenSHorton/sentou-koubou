using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Brennen.BrennenCode.Powers;

/// <summary>
/// Applied Tilted status. Also used as the hover-tip template for the keyword.
/// You are also Tilted while at or below 50% max HP (checked in <see cref="Tilted"/>).
/// </summary>
public sealed class TiltedPower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Tilted",
            "You're tilted. Some cards are stronger while [gold]Tilted[/gold]. You are also [gold]Tilted[/gold] while at or below [blue]50%[/blue] max HP.",
            "You're tilted. Some cards are stronger while [gold]Tilted[/gold]. Also while at or below [blue]50%[/blue] max HP.");
}
