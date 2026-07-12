using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Brennen.BrennenCode;

/// <summary>
/// Per-player-turn counters for Brennen kit (reset by Duo Queue).
/// CardsPlayed / AttacksPlayed count completed plays this turn.
/// </summary>
public static class BrennenTurnState
{
    public static int CardsPlayedThisTurn { get; private set; }
    public static int AttacksPlayedThisTurn { get; private set; }

    public static void ResetTurn()
    {
        CardsPlayedThisTurn = 0;
        AttacksPlayedThisTurn = 0;
    }

    public static void OnCardPlayed(CardModel card)
    {
        CardsPlayedThisTurn++;
        if (card.Type == CardType.Attack)
            AttacksPlayedThisTurn++;
    }
}
