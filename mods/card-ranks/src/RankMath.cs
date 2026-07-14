namespace CardRanks;

/// <summary>
/// Pure tier ladder. Plain → Tier I (blue) → Tier II → Tier III (max).
/// Multipliers: I ×1.5, II ×2, III ×3 on damage/block.
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
    public const decimal Tier1Multiplier = 1.5m;
    public const decimal Tier2Multiplier = 2.0m;
    public const decimal Tier3Multiplier = 3.0m;

    // Amount is always 1 on rank enchantments (UI draws one icon per Amount).
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
        CardRankLevel.None => CardRankLevel.Tier1,
        CardRankLevel.Tier1 => CardRankLevel.Tier2,
        CardRankLevel.Tier2 => CardRankLevel.Tier3,
        _ => CardRankLevel.Tier3,
    };

    public static int SumUpgradeLevels(int sacrificeLevel, int survivorLevel, int maxUpgradeLevel)
    {
        if (maxUpgradeLevel < 0)
            maxUpgradeLevel = 0;
        long sum = (long)Math.Max(0, sacrificeLevel) + Math.Max(0, survivorLevel);
        if (sum > maxUpgradeLevel)
            return maxUpgradeLevel;
        return (int)sum;
    }

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

    public static bool OnlyBlockedByBasicsPolicy(IEnumerable<RankCardView> cards, bool allowBasics)
    {
        if (allowBasics)
            return false;
        if (DeckHasCombinablePair(cards, allowBasics: true))
            return !DeckHasCombinablePair(cards, allowBasics: false);
        return false;
    }

    public static bool TryPlanCombine(
        RankCardView sacrifice,
        RankCardView survivor,
        bool allowBasics,
        int maxUpgradeLevel,
        out CardRankLevel resultRank,
        out int resultUpgradeLevel)
    {
        resultRank = CardRankLevel.None;
        resultUpgradeLevel = 0;
        if (!CanPair(sacrifice, survivor, allowBasics))
            return false;
        resultRank = NextRank(survivor.Rank);
        resultUpgradeLevel = SumUpgradeLevels(
            sacrifice.UpgradeLevel, survivor.UpgradeLevel, maxUpgradeLevel);
        return true;
    }

    public static bool TryPlanCombine(
        RankCardView sacrifice,
        RankCardView survivor,
        bool allowBasics,
        bool eitherUpgraded,
        out CardRankLevel resultRank,
        out bool resultUpgraded)
    {
        bool ok = TryPlanCombine(sacrifice, survivor, allowBasics, 99,
            out resultRank, out int level);
        resultUpgraded = level > 0 || eitherUpgraded;
        return ok;
    }
}
