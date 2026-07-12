using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using Whitney.WhitneyCode.Cards;

namespace Whitney.WhitneyCode;

/// <summary>
/// Per-player turn state for Blend, element tracking, and seal free flags.
/// Cleared at the start of each player turn via <see cref="Powers.BrushTrackerPower"/>.
/// </summary>
public static class WhitneyBrush
{
    private sealed class State
    {
        public WhitneyElement? LastElement;
        public readonly HashSet<WhitneyElement> ElementsPlayedThisTurn = new();
        public int CardsPlayedThisTurn;
        public bool AllElementsMode;
        public bool SealsFreeThisTurn;
        public bool NextSealFree;
        public int NextSealDiscount;
        public bool MasterworkTriggeredThisTurn;
        public int WindCardsThisTurn;
    }

    private static readonly Dictionary<int, State> States = new();

    private static int Key(Player? player) =>
        player?.Creature?.GetHashCode() ?? 0;

    private static State GetState(Player? player)
    {
        var k = Key(player);
        if (!States.TryGetValue(k, out var s))
        {
            s = new State();
            States[k] = s;
        }
        return s;
    }

    public static void Clear(Player? player)
    {
        if (player is null)
            return;
        var s = GetState(player);
        var allMode = s.AllElementsMode; // persists across turns while power up
        s.LastElement = null;
        s.ElementsPlayedThisTurn.Clear();
        s.CardsPlayedThisTurn = 0;
        s.SealsFreeThisTurn = false;
        s.NextSealFree = false;
        s.NextSealDiscount = 0;
        s.MasterworkTriggeredThisTurn = false;
        s.WindCardsThisTurn = 0;
        s.AllElementsMode = allMode;
    }

    public static void ClearAll() => States.Clear();

    public static WhitneyElement? LastElement(Player? player) =>
        GetState(player).LastElement;

    public static IReadOnlyCollection<WhitneyElement> ElementsPlayedThisTurn(Player? player) =>
        GetState(player).ElementsPlayedThisTurn;

    public static int CardsPlayedThisTurn(Player? player) =>
        GetState(player).CardsPlayedThisTurn;

    public static int DistinctElementsThisTurn(Player? player) =>
        GetState(player).ElementsPlayedThisTurn.Count;

    public static bool AllElementsMode
    {
        get => false; // use per-player
    }

    public static bool GetAllElementsMode(Player? player) =>
        GetState(player).AllElementsMode;

    public static void SetAllElementsMode(Player? player, bool value)
    {
        if (player is null) return;
        GetState(player).AllElementsMode = value;
    }

    public static bool SealsFreeThisTurn(Player? player) =>
        GetState(player).SealsFreeThisTurn;

    public static void SetSealsFreeThisTurn(Player? player, bool value)
    {
        if (player is null) return;
        GetState(player).SealsFreeThisTurn = value;
    }

    public static bool NextSealFree(Player? player) =>
        GetState(player).NextSealFree;

    public static void SetNextSealFree(Player? player, bool value)
    {
        if (player is null) return;
        GetState(player).NextSealFree = value;
    }

    public static int NextSealDiscount(Player? player) =>
        GetState(player).NextSealDiscount;

    public static void SetNextSealDiscount(Player? player, int value)
    {
        if (player is null) return;
        GetState(player).NextSealDiscount = Math.Max(0, value);
    }

    public static bool MasterworkTriggeredThisTurn(Player? player) =>
        GetState(player).MasterworkTriggeredThisTurn;

    public static void SetMasterworkTriggered(Player? player, bool value)
    {
        if (player is null) return;
        GetState(player).MasterworkTriggeredThisTurn = value;
    }

    public static int WindCardsThisTurn(Player? player) =>
        GetState(player).WindCardsThisTurn;

    public static void IncrementWind(Player? player)
    {
        if (player is null) return;
        GetState(player).WindCardsThisTurn++;
    }

    /// <summary>
    /// Whether playing <paramref name="element"/> would Blend (different from last,
    /// or all-elements mode with a prior element). Check <b>before</b> <see cref="NotePlay"/>.
    /// </summary>
    public static bool IsBlend(Player? player, WhitneyElement element)
    {
        var s = GetState(player);
        if (s.LastElement is null)
            return false;
        if (s.AllElementsMode)
            return true;
        return s.LastElement.Value != element;
    }

    /// <summary>
    /// Call at the <b>end</b> of a Whitney card's OnPlay. Updates last element / counts.
    /// Blend for the just-played card must be sampled before this call.
    /// </summary>
    public static void NotePlay(Player? player, WhitneyCard card)
    {
        if (player is null)
            return;

        var s = GetState(player);
        s.CardsPlayedThisTurn++;

        if (s.AllElementsMode)
        {
            s.ElementsPlayedThisTurn.Add(WhitneyElement.Fire);
            s.ElementsPlayedThisTurn.Add(WhitneyElement.Water);
            s.ElementsPlayedThisTurn.Add(WhitneyElement.Earth);
            s.ElementsPlayedThisTurn.Add(WhitneyElement.Wind);
        }
        else
        {
            s.ElementsPlayedThisTurn.Add(card.Element);
        }

        s.LastElement = card.Element;

        if (card.Element == WhitneyElement.Wind)
            s.WindCardsThisTurn++;
    }

    /// <summary>
    /// Distinct elements after hypothetically playing this element (for Confluence).
    /// </summary>
    public static int DistinctIncluding(Player? player, WhitneyElement element)
    {
        var s = GetState(player);
        if (s.AllElementsMode)
            return 4;
        var set = new HashSet<WhitneyElement>(s.ElementsPlayedThisTurn) { element };
        return set.Count;
    }
}
