using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Blake.BlakeCode.Powers;

/// <summary>
/// Localization / hover-tip template for Sweetspot only.
/// Not applied as a combat buff — used via HoverTipFactory.FromPower.
/// </summary>
public sealed class SweetspotPower : BlakePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Sweetspot",
            "A bonus if the stated condition is true — usually if the enemy intends to Attack.",
            "A bonus if the stated condition is true.");
}
