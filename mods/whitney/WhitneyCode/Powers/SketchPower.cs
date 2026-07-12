using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Whitney.WhitneyCode.Powers;

/// <summary>
/// Loc/hover template for Sketch. Runtime discount is on <see cref="WhitneyBrush"/>
/// flags applied by the Sketch card; <see cref="WetPaintPower"/> hooks star cost.
/// </summary>
public sealed class SketchPower : WhitneyPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Sketch",
            "The next Seal you play has reduced [gold]Ink[/gold] cost.",
            "The next Seal you play has reduced [gold]Ink[/gold] cost.");
}
