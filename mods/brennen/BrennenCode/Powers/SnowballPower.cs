using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Brennen.BrennenCode.Powers;

/// <summary>Whenever you gain Fed, gain Amount Block and draw 1 (wired via Fed.Gain).</summary>
public sealed class SnowballPower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Snowball",
            "Whenever you gain [gold]Fed[/gold], gain {Amount} [gold]Block[/gold] and draw 1 card.",
            "Whenever you gain [gold]Fed[/gold], gain {Amount} [gold]Block[/gold] and draw 1 card.");
}
