using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Whitney.WhitneyCode.Cards;

namespace Whitney.WhitneyCode.Powers;

/// <summary>
/// Turn tracker: clears <see cref="WhitneyBrush"/> at the start of Whitney's turn,
/// and applies free/discount seal Ink cost from brush flags.
/// Applied by Traveler's Inkpot at combat start.
/// </summary>
public sealed class BrushTrackerPower : WhitneyPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Brush",
            "Tracks elemental strokes this turn.",
            "Tracks elemental strokes this turn.");

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner))
            return;
        if (Owner.Player is null)
            return;

        WhitneyBrush.Clear(Owner.Player);
        await Task.CompletedTask;
    }

    public override bool TryModifyStarCost(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card is not WhitneyCard { IsSeal: true })
            return false;
        if (Owner.Player is null)
            return false;

        if (WhitneyBrush.SealsFreeThisTurn(Owner.Player) || WhitneyBrush.NextSealFree(Owner.Player))
        {
            modifiedCost = 0m;
            return true;
        }

        var discount = WhitneyBrush.NextSealDiscount(Owner.Player);
        if (discount > 0)
        {
            modifiedCost = System.Math.Max(0m, originalCost - discount);
            return true;
        }

        return false;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner)
            return;
        if (cardPlay.Card is not WhitneyCard { IsSeal: true })
            return;
        if (Owner.Player is null)
            return;

        if (WhitneyBrush.NextSealFree(Owner.Player))
            WhitneyBrush.SetNextSealFree(Owner.Player, false);
        if (WhitneyBrush.NextSealDiscount(Owner.Player) > 0)
            WhitneyBrush.SetNextSealDiscount(Owner.Player, 0);

        await Task.CompletedTask;
    }
}
