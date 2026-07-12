using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Blake.BlakeCode.Powers;

/// <summary>Charge can no longer be halved (turns off Interrupt).</summary>
public sealed class SuperArmorPower : BlakePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Super Armor",
            "Your [gold]Charge[/gold] can no longer be halved.",
            "Your [gold]Charge[/gold] can no longer be halved.");
}
