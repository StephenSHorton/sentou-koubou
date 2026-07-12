using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Whitney.WhitneyCode.Powers;

/// <summary>
/// Elemental focus. Dual-purpose Earth/setup cards stack this;
/// some spells scale off it (atelier density of seals).
/// </summary>
public sealed class AttunementPower : WhitneyPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Attunement",
            "Your seals are charged. Attacks deal additional damage equal to [gold]Attunement[/gold].",
            "Attacks deal {Amount} additional damage.");
}
