using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Brennen.BrennenCode.Powers;

/// <summary>Fed stacks never drop during combat. Scales snowball payoffs.</summary>
public sealed class FedPower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Fed",
            "Your snowball. Never decreases during combat.",
            "You have {Amount} [gold]Fed[/gold].");
}
