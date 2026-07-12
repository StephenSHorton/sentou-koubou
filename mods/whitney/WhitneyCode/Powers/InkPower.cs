using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Whitney.WhitneyCode.Powers;

/// <summary>
/// Whitney's second mana. Banked during combat; spent on bigger seals.
/// Shown as a buff counter (Ink pot) — not Stars.
/// </summary>
public sealed class InkPower : WhitneyPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Ink",
            "Your ink pot. Spend [gold]Ink[/gold] to cast greater seals.",
            "You have {Amount} [gold]Ink[/gold].");
}
