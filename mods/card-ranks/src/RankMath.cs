namespace CardRanks;

/// <summary>
/// Pure combine eligibility and rank ladder. No game types — unit-tested directly.
/// Rank 2 multiplies damage/block by 1.5; Rank 3 by 3. Manual combine only (v1).
/// </summary>
public enum CardRankLevel
{
    None = 0,
    Rank2 = 1,
    Rank3 = 2,
}

public readonly record struct RankCardView(string Id, CardRankLevel Rank, bool IsBasicLike);

public static class RankMath
{
    public const decimal Rank2Multiplier = 1.5m;
    public const decimal Rank3Multiplier = 3m;

    public static decimal Multiplier(CardRankLevel rank) => rank switch
    {
        CardRankLevel.Rank2 => Rank2Multiplier,
        CardRankLevel.Rank3 => Rank3Multiplier,
        _ => 1m,
    };

    /// <summary>A card can be offered as a combine candidate.</summary>
    public static bool IsCandidate(CardRankLevel rank, bool isBasicLike, bool allowBasics)
    {
        if (rank >= CardRankLevel.Rank3)
            return false;
        if (isBasicLike && !allowBasics)
            return false;
        return true;
    }

    public static bool IsCandidate(RankCardView card, bool allowBasics) =>
        IsCandidate(card.Rank, card.IsBasicLike, allowBasics);

    /// <summary>
    /// Two cards may be combined only if they share identity, share rank
    /// (both plain → Rank2, both Rank2 → Rank3), and both pass candidate rules.
    /// </summary>
    public static bool CanPair(RankCardView a, RankCardView b, bool allowBasics)
    {
        if (!string.Equals(a.Id, b.Id, StringComparison.Ordinal))
            return false;
        if (a.Rank != b.Rank)
            return false;
        if (!IsCandidate(a, allowBasics) || !IsCandidate(b, allowBasics))
            return false;
        return true;
    }

    public static CardRankLevel NextRank(CardRankLevel current) => current switch
    {
        CardRankLevel.None => CardRankLevel.Rank2,
        CardRankLevel.Rank2 => CardRankLevel.Rank3,
        _ => CardRankLevel.Rank3,
    };

    /// <summary>Whether the deck snapshot has at least one legal combine pair.</summary>
    public static bool DeckHasCombinablePair(IEnumerable<RankCardView> cards, bool allowBasics)
    {
        List<RankCardView> list = cards.Where(c => IsCandidate(c, allowBasics)).ToList();
        for (int i = 0; i < list.Count; i++)
        {
            for (int j = i + 1; j < list.Count; j++)
            {
                if (CanPair(list[i], list[j], allowBasics))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Deterministic apply plan: which rank the survivor ends at, and whether
    /// an upgrade should be forced if either input was upgraded.
    /// </summary>
    public static bool TryPlanCombine(
        RankCardView sacrifice,
        RankCardView survivor,
        bool allowBasics,
        bool eitherUpgraded,
        out CardRankLevel resultRank,
        out bool resultUpgraded)
    {
        resultRank = CardRankLevel.None;
        resultUpgraded = false;
        if (!CanPair(sacrifice, survivor, allowBasics))
            return false;
        resultRank = NextRank(survivor.Rank);
        resultUpgraded = eitherUpgraded;
        return true;
    }
}
