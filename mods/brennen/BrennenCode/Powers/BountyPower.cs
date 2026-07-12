using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Brennen.BrennenCode.Powers;

/// <summary>Fed gains are doubled while Tilted (logic in Fed.Gain).</summary>
public sealed class BountyPower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Bounty",
            "[gold]Fed[/gold] you gain is doubled while [gold]Tilted[/gold].",
            "[gold]Fed[/gold] you gain is doubled while [gold]Tilted[/gold].");
}
