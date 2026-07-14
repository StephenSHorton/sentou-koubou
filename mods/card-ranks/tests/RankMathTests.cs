using Xunit;

namespace CardRanks.Tests;

public class RankMathTests
{
    private static RankCardView Plain(string id, bool basic = false, int up = 0) =>
        new(id, CardRankLevel.None, basic, up);

    private static RankCardView R2(string id, bool basic = false, int up = 0) =>
        new(id, CardRankLevel.Rank2, basic, up);

    private static RankCardView R3(string id, bool basic = false, int up = 0) =>
        new(id, CardRankLevel.Rank3, basic, up);

    [Fact]
    public void TwoPlainSameId_PlansRank2()
    {
        Assert.True(RankMath.TryPlanCombine(
            Plain("STRIKE"), Plain("STRIKE"), allowBasics: true, maxUpgradeLevel: 5,
            out CardRankLevel rank, out int up));
        Assert.Equal(CardRankLevel.Rank2, rank);
        Assert.Equal(0, up);
        Assert.Equal(1.5m, RankMath.Multiplier(rank));
    }

    [Fact]
    public void TwoRank2SameId_PlansRank3()
    {
        Assert.True(RankMath.TryPlanCombine(
            R2("DEFEND", up: 1), R2("DEFEND", up: 1), allowBasics: true, maxUpgradeLevel: 5,
            out CardRankLevel rank, out int up));
        Assert.Equal(CardRankLevel.Rank3, rank);
        Assert.Equal(2, up);
        Assert.Equal(3m, RankMath.Multiplier(rank));
    }

    [Fact]
    public void UpgradeLevels_SumAndClamp()
    {
        Assert.Equal(0, RankMath.SumUpgradeLevels(0, 0, 5));
        Assert.Equal(1, RankMath.SumUpgradeLevels(1, 0, 5));
        Assert.Equal(2, RankMath.SumUpgradeLevels(1, 1, 5));
        Assert.Equal(3, RankMath.SumUpgradeLevels(1, 2, 5));
        Assert.Equal(5, RankMath.SumUpgradeLevels(3, 3, 5)); // clamp
        Assert.Equal(4, RankMath.SumUpgradeLevels(1, 3, 99));
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
        Assert.False(RankMath.CanPair(R2("STRIKE"), R3("STRIKE"), allowBasics: true));
        Assert.False(RankMath.CanPair(Plain("STRIKE"), R3("STRIKE"), allowBasics: true));
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

    [Fact]
    public void TryPlanCombine_IsDeterministicForPeerMirror()
    {
        var sac = R2("BRENNEN-STRIKE", basic: true, up: 1);
        var surv = R2("BRENNEN-STRIKE", basic: true, up: 0);
        Assert.True(RankMath.TryPlanCombine(sac, surv, allowBasics: true, maxUpgradeLevel: 10,
            out CardRankLevel r1, out int u1));
        Assert.True(RankMath.TryPlanCombine(sac, surv, allowBasics: true, maxUpgradeLevel: 10,
            out CardRankLevel r2, out int u2));
        Assert.Equal(r1, r2);
        Assert.Equal(u1, u2);
        Assert.Equal(CardRankLevel.Rank3, r1);
        Assert.Equal(1, u1);
    }

    [Fact]
    public void SameTier_PlanNextTierOnly()
    {
        Assert.True(RankMath.TryPlanCombine(
            Plain("X"), Plain("X"), allowBasics: true, maxUpgradeLevel: 5,
            out CardRankLevel toR2, out _));
        Assert.Equal(CardRankLevel.Rank2, toR2);

        Assert.True(RankMath.TryPlanCombine(
            R2("X"), R2("X"), allowBasics: true, maxUpgradeLevel: 5,
            out CardRankLevel toR3, out _));
        Assert.Equal(CardRankLevel.Rank3, toR3);
    }

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
        Assert.Contains("SumUpgradeLevels", names);
    }

    [Fact]
    public void LooksLikeRank_IdBlobs()
    {
        Assert.True(RankMath.LooksLikeSecondRank("CARDRANKS-SECOND_RANK"));
        Assert.True(RankMath.LooksLikeSecondRank("Foo.SECOND_RANK"));
        Assert.True(RankMath.LooksLikeThirdRank("CARDRANKS-THIRD_RANK"));
        Assert.False(RankMath.LooksLikeSecondRank("CARDRANKS-THIRD_RANK"));
        Assert.False(RankMath.LooksLikeThirdRank("CARDRANKS-SECOND_RANK"));
    }

    [Fact]
    public void AmountTags_AreDistinctForRank2AndRank3()
    {
        Assert.Equal(2, RankMath.Rank2AmountTag);
        Assert.Equal(3, RankMath.Rank3AmountTag);
        Assert.NotEqual(RankMath.Rank2AmountTag, RankMath.Rank3AmountTag);
    }
}
