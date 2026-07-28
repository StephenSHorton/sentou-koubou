namespace CardRanks;

/// <summary>
/// Pure tier ladder. Plain → Tier I (blue) → Tier II → Tier III (max).
/// Multipliers: I ×1.5, II ×2, III ×3 on damage/block.
/// Combine needs <see cref="CardsPerCombine"/> matching copies (keep 1, sacrifice the rest).
/// </summary>
public enum CardRankLevel
{
    None = 0,
    Tier1 = 1,
    Tier2 = 2,
    Tier3 = 3,
}

public readonly record struct RankCardView(
    string Id,
    CardRankLevel Rank,
    bool IsBasicLike,
    int UpgradeLevel = 0);

public static class RankMath
{
    /// <summary>Number of matching same-tier cards required for one tier-up.</summary>
    public const int CardsPerCombine = 3;

    public const decimal Tier1Multiplier = 1.5m;
    public const decimal Tier2Multiplier = 2.0m;
    public const decimal Tier3Multiplier = 3.0m;

    public const decimal Rank2Multiplier = Tier1Multiplier;
    public const decimal Rank3Multiplier = Tier3Multiplier;

    public static decimal Multiplier(CardRankLevel rank) => rank switch
    {
        CardRankLevel.Tier1 => Tier1Multiplier,
        CardRankLevel.Tier2 => Tier2Multiplier,
        CardRankLevel.Tier3 => Tier3Multiplier,
        _ => 1m,
    };

    public static string TierRoman(CardRankLevel rank) => rank switch
    {
        CardRankLevel.Tier1 => "I",
        CardRankLevel.Tier2 => "II",
        CardRankLevel.Tier3 => "III",
        _ => "-",
    };

    public static bool LooksLikeTier1(string blob) =>
        blob.Contains("FIRST_RANK", StringComparison.OrdinalIgnoreCase)
        || blob.Contains("TIER1", StringComparison.OrdinalIgnoreCase)
        || blob.Contains("TIER_1", StringComparison.OrdinalIgnoreCase)
        || (blob.Contains("FIRST", StringComparison.OrdinalIgnoreCase)
            && blob.Contains("RANK", StringComparison.OrdinalIgnoreCase));

    public static bool LooksLikeSecondRank(string blob) =>
        blob.Contains("SECOND_RANK", StringComparison.OrdinalIgnoreCase)
        || blob.Contains("SECONDRANK", StringComparison.OrdinalIgnoreCase)
        || blob.Contains("TIER2", StringComparison.OrdinalIgnoreCase)
        || (blob.Contains("SECOND", StringComparison.OrdinalIgnoreCase)
            && blob.Contains("RANK", StringComparison.OrdinalIgnoreCase)
            && !blob.Contains("THIRD", StringComparison.OrdinalIgnoreCase));

    public static bool LooksLikeThirdRank(string blob) =>
        blob.Contains("THIRD_RANK", StringComparison.OrdinalIgnoreCase)
        || blob.Contains("THIRDRANK", StringComparison.OrdinalIgnoreCase)
        || blob.Contains("TIER3", StringComparison.OrdinalIgnoreCase)
        || (blob.Contains("THIRD", StringComparison.OrdinalIgnoreCase)
            && blob.Contains("RANK", StringComparison.OrdinalIgnoreCase));

    public static bool IsCandidate(CardRankLevel rank, bool isBasicLike, bool allowBasics)
    {
        if (rank >= CardRankLevel.Tier3)
            return false;
        if (isBasicLike && !allowBasics)
            return false;
        return true;
    }

    public static bool IsCandidate(RankCardView card, bool allowBasics) =>
        IsCandidate(card.Rank, card.IsBasicLike, allowBasics);

    /// <summary>Two cards share identity + tier and can participate in a combine group.</summary>
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

    /// <summary>All cards in the group match identity + tier and are candidates.</summary>
    public static bool CanGroup(IReadOnlyList<RankCardView> cards, bool allowBasics)
    {
        if (cards.Count != CardsPerCombine)
            return false;
        RankCardView anchor = cards[0];
        if (!IsCandidate(anchor, allowBasics))
            return false;
        for (int i = 1; i < cards.Count; i++)
        {
            if (!CanPair(anchor, cards[i], allowBasics))
                return false;
        }
        return true;
    }

    public static CardRankLevel NextRank(CardRankLevel current) => current switch
    {
        CardRankLevel.None => CardRankLevel.Tier1,
        CardRankLevel.Tier1 => CardRankLevel.Tier2,
        CardRankLevel.Tier2 => CardRankLevel.Tier3,
        _ => CardRankLevel.Tier3,
    };

    public static int SumUpgradeLevels(IEnumerable<int> levels, int maxUpgradeLevel)
    {
        if (maxUpgradeLevel < 0)
            maxUpgradeLevel = 0;
        long sum = 0;
        foreach (int level in levels)
            sum += Math.Max(0, level);
        if (sum > maxUpgradeLevel)
            return maxUpgradeLevel;
        return (int)sum;
    }

    public static int SumUpgradeLevels(int sacrificeLevel, int survivorLevel, int maxUpgradeLevel) =>
        SumUpgradeLevels([sacrificeLevel, survivorLevel], maxUpgradeLevel);

    public static bool DeckHasCombinableGroup(IEnumerable<RankCardView> cards, bool allowBasics)
    {
        List<RankCardView> list = cards.Where(c => IsCandidate(c, allowBasics)).ToList();
        // Group by id+rank; need at least CardsPerCombine in any bucket.
        var buckets = new Dictionary<(string Id, CardRankLevel Rank), int>();
        foreach (RankCardView c in list)
        {
            var key = (c.Id, c.Rank);
            buckets.TryGetValue(key, out int n);
            buckets[key] = n + 1;
            if (n + 1 >= CardsPerCombine)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Cards that belong to an (id, rank) bucket with at least
    /// <see cref="CardsPerCombine"/> members — i.e. can actually form a combine.
    /// Used to hide unusable cards from the rest-site combine picker.
    /// </summary>
    public static List<RankCardView> FilterToCombinableGroupMembers(
        IEnumerable<RankCardView> cards,
        bool allowBasics)
    {
        List<RankCardView> list = cards.Where(c => IsCandidate(c, allowBasics)).ToList();
        var counts = new Dictionary<(string Id, CardRankLevel Rank), int>();
        foreach (RankCardView c in list)
        {
            var key = (c.Id, c.Rank);
            counts.TryGetValue(key, out int n);
            counts[key] = n + 1;
        }

        return list
            .Where(c => counts.TryGetValue((c.Id, c.Rank), out int n) && n >= CardsPerCombine)
            .ToList();
    }

    /// <summary>Legacy name — true when a full combine group exists in the deck.</summary>
    public static bool DeckHasCombinablePair(IEnumerable<RankCardView> cards, bool allowBasics) =>
        DeckHasCombinableGroup(cards, allowBasics);

    public static bool OnlyBlockedByBasicsPolicy(IEnumerable<RankCardView> cards, bool allowBasics)
    {
        if (allowBasics)
            return false;
        if (DeckHasCombinableGroup(cards, allowBasics: true))
            return !DeckHasCombinableGroup(cards, allowBasics: false);
        return false;
    }

    public static bool TryPlanCombine(
        IReadOnlyList<RankCardView> group,
        bool allowBasics,
        int maxUpgradeLevel,
        out CardRankLevel resultRank,
        out int resultUpgradeLevel)
    {
        resultRank = CardRankLevel.None;
        resultUpgradeLevel = 0;
        if (!CanGroup(group, allowBasics))
            return false;
        resultRank = NextRank(group[0].Rank);
        resultUpgradeLevel = SumUpgradeLevels(group.Select(c => c.UpgradeLevel), maxUpgradeLevel);
        return true;
    }

    public static bool TryPlanCombine(
        RankCardView sacrifice,
        RankCardView survivor,
        bool allowBasics,
        int maxUpgradeLevel,
        out CardRankLevel resultRank,
        out int resultUpgradeLevel)
    {
        // Back-compat 2-card API — not used for live combine (needs 3).
        resultRank = CardRankLevel.None;
        resultUpgradeLevel = 0;
        if (!CanPair(sacrifice, survivor, allowBasics))
            return false;
        // Incomplete group of 2 cannot plan a real combine.
        return false;
    }

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
        return false;
    }
}
