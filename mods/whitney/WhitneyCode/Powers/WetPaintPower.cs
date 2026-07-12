using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Whitney.WhitneyCode.Cards;

namespace Whitney.WhitneyCode.Powers;

/// <summary>Seals cost 0 Ink this turn (brush flag). Cost hook also lives on BrushTracker.</summary>
public sealed class WetPaintPower : WhitneyPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Wet Paint",
            "Seals cost [blue]0[/blue] [gold]Ink[/gold] this turn.",
            "Seals cost [blue]0[/blue] [gold]Ink[/gold] this turn.");

    public override bool TryModifyStarCost(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card is not WhitneyCard { IsSeal: true })
            return false;
        if (Owner.Player is null || !WhitneyBrush.SealsFreeThisTurn(Owner.Player))
            return false;

        modifiedCost = 0m;
        return true;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Task.CompletedTask;
    }
}
