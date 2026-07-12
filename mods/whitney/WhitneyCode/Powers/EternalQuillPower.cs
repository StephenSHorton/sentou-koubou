using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using Whitney.WhitneyCode.Cards;

namespace Whitney.WhitneyCode.Powers;

/// <summary>Seals cost 1 less Ink, minimum 1.</summary>
public sealed class EternalQuillPower : WhitneyPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Eternal Quill",
            "Seals cost [blue]1[/blue] less [gold]Ink[/gold] (minimum 1).",
            "Seals cost [blue]1[/blue] less [gold]Ink[/gold] (minimum 1).");

    public override bool TryModifyStarCost(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card is not WhitneyCard { IsSeal: true })
            return false;
        if (originalCost <= 0)
            return false;

        modifiedCost = System.Math.Max(1m, originalCost - 1m);
        return true;
    }
}
