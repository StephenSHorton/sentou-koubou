using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Whitney.WhitneyCode.Powers;

/// <summary>
/// Localization / hover-tip template for Blend only.
/// Not applied as a combat buff — used via HoverTipFactory.FromPower.
/// </summary>
public sealed class BlendPower : WhitneyPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Blend",
            "A bonus if this card's element is different from the last Whitney card you played this turn.",
            "Bonus if this element's brush stroke differs from your previous one.");
}
