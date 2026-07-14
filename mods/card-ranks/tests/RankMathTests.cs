using Xunit;

namespace CardRanks.Tests;

public class RankMathTests
{
    private static RankCardView Plain(string id, bool basic = false) =>
        new(id, CardRankLevel.None, basic);

    private static RankCardView R2(string id, bool basic = false) =>
        new(id, CardRankLevel.Rank2, basic);

    private static RankCardView R3(string id, bool basic = false) =>
        new(id, CardRankLevel.Rank3, basic);

    [Fact]
    public void TwoPlainSameId_PlansRank2()
    {
        Assert.True(RankMath.TryPlanCombine(
            Plain("STRIKE"), Plain("STRIKE"), allowBasics: true, eitherUpgraded: false,
            out CardRankLevel rank, out bool upgraded));
        Assert.Equal(CardRankLevel.Rank2, rank);
        Assert.False(upgraded);
        Assert.Equal(1.5m, RankMath.Multiplier(rank));
    }

    [Fact]
    public void TwoRank2SameId_PlansRank3()
    {
        Assert.True(RankMath.TryPlanCombine(
            R2("DEFEND"), R2("DEFEND"), allowBasics: true, eitherUpgraded: true,
            out CardRankLevel rank, out bool upgraded));
        Assert.Equal(CardRankLevel.Rank3, rank);
        Assert.True(upgraded);
        Assert.Equal(3m, RankMath.Multiplier(rank));
    }

    [Fact]
    public void Rank3_IsNotCandidate()
    {
        Assert.False(RankMath.IsCandidate(CardRankLevel.Rank3, isBasicLike: false, allowBasics: true));
        Assert.False(RankMath.CanPair(R3("X"), R3("X"), allowBasics: true));
    }

    [Fact]
    public void DifferentIds_Rejected()
    {
        Assert.False(RankMath.CanPair(Plain("STRIKE"), Plain("DEFEND"), allowBasics: true));
    }

    [Fact]
    public void MixedRanks_Rejected()
    {
        Assert.False(RankMath.CanPair(Plain("STRIKE"), R2("STRIKE"), allowBasics: true));
    }

    [Fact]
    public void BasicsBlockedWhenSettingOff()
    {
        Assert.False(RankMath.IsCandidate(CardRankLevel.None, isBasicLike: true, allowBasics: false));
        Assert.False(RankMath.CanPair(
            Plain("MOD-STRIKE", basic: true),
            Plain("MOD-STRIKE", basic: true),
            allowBasics: false));
    }

    [Fact]
    public void ModdedBasicAllowedWhenSettingOn()
    {
        Assert.True(RankMath.IsCandidate(CardRankLevel.None, isBasicLike: true, allowBasics: true));
        Assert.True(RankMath.CanPair(
            Plain("BRENNEN-STRIKE", basic: true),
            Plain("BRENNEN-STRIKE", basic: true),
            allowBasics: true));
    }

    [Fact]
    public void DeckHasPair_DetectsLegalPair()
    {
        RankCardView[] deck =
        [
            Plain("A"),
            Plain("B"),
            Plain("A"),
            R3("C"),
        ];
        Assert.True(RankMath.DeckHasCombinablePair(deck, allowBasics: true));
    }

    [Fact]
    public void DeckHasPair_FalseWhenOnlyBasicsBlocked()
    {
        RankCardView[] deck =
        [
            Plain("STRIKE", basic: true),
            Plain("STRIKE", basic: true),
            R3("RARE"),
        ];
        Assert.False(RankMath.DeckHasCombinablePair(deck, allowBasics: false));
        Assert.True(RankMath.DeckHasCombinablePair(deck, allowBasics: true));
        Assert.True(RankMath.OnlyBlockedByBasicsPolicy(deck, allowBasics: false));
        Assert.False(RankMath.OnlyBlockedByBasicsPolicy(deck, allowBasics: true));
    }

    [Fact]
    public void NextRank_Ladder()
    {
        Assert.Equal(CardRankLevel.Rank2, RankMath.NextRank(CardRankLevel.None));
        Assert.Equal(CardRankLevel.Rank3, RankMath.NextRank(CardRankLevel.Rank2));
    }

    /// <summary>
    /// Apply-plan is pure and deterministic for multiplayer mirrors:
    /// same inputs always yield the same result rank (and upgrade flag).
    /// </summary>
    [Fact]
    public void TryPlanCombine_IsDeterministicForPeerMirror()
    {
        var sac = R2("BRENNEN-STRIKE", basic: true);
        var surv = R2("BRENNEN-STRIKE", basic: true);
        Assert.True(RankMath.TryPlanCombine(sac, surv, allowBasics: true, eitherUpgraded: true,
            out CardRankLevel r1, out bool u1));
        Assert.True(RankMath.TryPlanCombine(sac, surv, allowBasics: true, eitherUpgraded: true,
            out CardRankLevel r2, out bool u2));
        Assert.Equal(r1, r2);
        Assert.Equal(u1, u2);
        Assert.Equal(CardRankLevel.Rank3, r1);
        Assert.True(u1);
    }

    [Fact]
    public void MixedTiers_CannotPair()
    {
        Assert.False(RankMath.CanPair(Plain("STRIKE"), R2("STRIKE"), allowBasics: true));
        Assert.False(RankMath.CanPair(R2("STRIKE"), R3("STRIKE"), allowBasics: true));
        Assert.False(RankMath.CanPair(Plain("STRIKE"), R3("STRIKE"), allowBasics: true));
    }

    [Fact]
    public void SameTier_PlanNextTierOnly()
    {
        Assert.True(RankMath.TryPlanCombine(
            Plain("X"), Plain("X"), allowBasics: true, eitherUpgraded: false,
            out CardRankLevel toR2, out _));
        Assert.Equal(CardRankLevel.Rank2, toR2);

        Assert.True(RankMath.TryPlanCombine(
            R2("X"), R2("X"), allowBasics: true, eitherUpgraded: false,
            out CardRankLevel toR3, out _));
        Assert.Equal(CardRankLevel.Rank3, toR3);
    }

    /// <summary>
    /// v1 is manual-only: RankMath has no pile-change auto-merge API —
    /// only candidate/pair/plan helpers used by the rest-site path.
    /// </summary>
    [Fact]
    public void ManualOnly_PublicSurfaceIsRestSiteHelpers()
    {
        var names = typeof(RankMath).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(m => m.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToArray();
        Assert.DoesNotContain("AfterCardChangedPiles", names);
        Assert.DoesNotContain("AutoCombine", names);
        Assert.Contains("CanPair", names);
        Assert.Contains("TryPlanCombine", names);
        Assert.Contains("IsCandidate", names);
    }
}
