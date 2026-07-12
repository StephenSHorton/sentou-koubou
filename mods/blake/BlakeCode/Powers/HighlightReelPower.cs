using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Blake.BlakeCode.Powers;

/// <summary>Whenever you Unleash, gain Amount Energy and draw Amount cards.</summary>
public sealed class HighlightReelPower : BlakePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Highlight Reel",
            "Whenever you [gold]Unleash[/gold], gain {Amount} Energy and draw {Amount} cards.",
            "Whenever you [gold]Unleash[/gold], gain {Amount} Energy and draw {Amount} cards.");
}
