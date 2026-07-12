using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Blake.BlakeCode.Powers;

/// <summary>Whenever you Rev, deal Amount damage to ALL enemies (handled in Charge.Rev).</summary>
public sealed class HeatHazePower : BlakePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Heat Haze",
            "Whenever you [gold]Rev[/gold], deal {Amount} damage to ALL enemies.",
            "Whenever you [gold]Rev[/gold], deal {Amount} damage to ALL enemies.");
}
