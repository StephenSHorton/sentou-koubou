using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Whitney.WhitneyCode.Powers;

/// <summary>
/// Localization / hover-tip template for Ink only.
/// Runtime Ink is stored as player Stars (see <see cref="Ink"/>) so it appears
/// next to Energy — this power is not applied as a creature buff.
/// </summary>
public sealed class InkPower : WhitneyPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Ink",
            "Your second mana. Bank [gold]Ink[/gold] and spend it on seals. Shown next to Energy.",
            "You have {Amount} [gold]Ink[/gold].");
}
