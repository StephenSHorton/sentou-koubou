using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Blake.BlakeCode.Powers;

/// <summary>
/// Localization / hover-tip template for Follow-Through only.
/// Not applied as a combat buff — used via HoverTipFactory.FromPower.
/// </summary>
public sealed class FollowThroughPower : BlakePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Follow-Through",
            "Excess damage from this attack hits another enemy.",
            "Excess damage hits another enemy.");
}
