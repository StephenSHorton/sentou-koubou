using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Whitney.WhitneyCode.Powers;

/// <summary>
/// Elemental focus. Dual-purpose Earth/setup cards stack this;
/// all of Whitney's attacks deal +Amount damage while it is up.
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

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner)
            return amount;
        // Only powered attack/move damage (matches Strength-style scaling).
        if ((props & ValueProp.Move) == 0)
            return amount;
        if ((props & ValueProp.Unpowered) != 0)
            return amount;

        return amount + Amount;
    }
}
