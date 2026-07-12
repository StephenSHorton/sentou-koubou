using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Blake.BlakeCode.Powers;

/// <summary>
/// Localization / hover-tip template for Combo only.
/// Not applied as a combat buff — used via HoverTipFactory.FromPower.
/// </summary>
public sealed class ComboPower : BlakePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Combo",
            "Bonus if this is at least the Nth card you've played this turn.",
            "Bonus if this is at least the Nth card you've played this turn.");
}
