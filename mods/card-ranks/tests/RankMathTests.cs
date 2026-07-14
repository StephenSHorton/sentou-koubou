using Xunit;

namespace CardRanks.Tests;

public class RankMathTests
{
    private static RankCardView Plain(string id, bool basic = false, int up = 0) =>
        new(id, CardRankLevel.None, basic, up);

    private static RankCardView T1(string id, bool basic = false, int up = 0) =>
        new(id, CardRankLevel.Tier1, basic, up);

    private static RankCardView T2(string id, bool basic = false, int up = 0) =>
        new(id, CardRankLevel.Tier2, basic, up);

    private static RankCardView T3(string id, bool basic = false, int up = 0) =>
        new(id, CardRankLevel.Tier3, basic, up);

    [Fact]
    public void Ladder_PlainToIToIIToIII()
    {
        Assert.Equal(CardRankLevel.Tier1, RankMath.NextRank(CardRankLevel.None));
        Assert.Equal(CardRankLevel.Tier2, RankMath.NextRank(CardRankLevel.Tier1));
        Assert.Equal(CardRankLevel.Tier3, RankMath.NextRank(CardRankLevel.Tier2));
        Assert.Equal(1.5m, RankMath.Multiplier(CardRankLevel.Tier1));
        Assert.Equal(2.0m, RankMath.Multiplier(CardRankLevel.Tier2));
        Assert.Equal(3.0m, RankMath.Multiplier(CardRankLevel.Tier3));
    }

    [Fact]
    public void TwoPlain_PlansTier1()
    {
        Assert.True(RankMath.TryPlanCombine(
            Plain("STRIKE"), Plain("STRIKE"), allowBasics: true, maxUpgradeLevel: 5,
            out CardRankLevel rank, out int up));
        Assert.Equal(CardRankLevel.Tier1, rank);
        Assert.Equal(0, up);
    }

    [Fact]
    public void TwoTier1_PlansTier2()
    {
        Assert.True(RankMath.TryPlanCombine(
            T1("STRIKE"), T1("STRIKE"), allowBasics: true, maxUpgradeLevel: 5,
            out CardRankLevel rank, out _));
        Assert.Equal(CardRankLevel.Tier2, rank);
    }

    [Fact]
    public void TwoTier2_PlansTier3()
    {
        Assert.True(RankMath.TryPlanCombine(
            T2("DEFEND", up: 1), T2("DEFEND", up: 1), allowBasics: true, maxUpgradeLevel: 5,
            out CardRankLevel rank, out int up));
        Assert.Equal(CardRankLevel.Tier3, rank);
        Assert.Equal(2, up);
    }

    [Fact]
    public void Tier3_IsNotCandidate()
    {
        Assert.False(RankMath.IsCandidate(CardRankLevel.Tier3, isBasicLike: false, allowBasics: true));
        Assert.False(RankMath.CanPair(T3("X"), T3("X"), allowBasics: true));
    }

    [Fact]
    public void MixedTiers_Rejected()
    {
        Assert.False(RankMath.CanPair(Plain("STRIKE"), T1("STRIKE"), allowBasics: true));
        Assert.False(RankMath.CanPair(T1("STRIKE"), T2("STRIKE"), allowBasics: true));
        Assert.False(RankMath.CanPair(T2("STRIKE"), T3("STRIKE"), allowBasics: true));
    }

    [Fact]
    public void DifferentIds_Rejected()
    {
        Assert.False(RankMath.CanPair(Plain("STRIKE"), Plain("DEFEND"), allowBasics: true));
    }

    [Fact]
    public void UpgradeLevels_SumAndClamp()
    {
        Assert.Equal(2, RankMath.SumUpgradeLevels(1, 1, 5));
        Assert.Equal(5, RankMath.SumUpgradeLevels(3, 3, 5));
    }

    [Fact]
    public void BasicsBlockedWhenSettingOff()
    {
        Assert.False(RankMath.CanPair(
            Plain("MOD-STRIKE", basic: true),
            Plain("MOD-STRIKE", basic: true),
            allowBasics: false));
    }

    [Fact]
    public void ModdedBasicAllowedWhenSettingOn()
    {
        Assert.True(RankMath.CanPair(
            Plain("BRENNEN-STRIKE", basic: true),
            Plain("BRENNEN-STRIKE", basic: true),
            allowBasics: true));
    }

    [Fact]
    public void AmountTags_Distinct()
    {
        Assert.Equal(1, RankMath.Tier1AmountTag);
        Assert.Equal(2, RankMath.Tier2AmountTag);
        Assert.Equal(3, RankMath.Tier3AmountTag);
    }

    [Fact]
    public void RomanLabels()
    {
        Assert.Equal("I", RankMath.TierRoman(CardRankLevel.Tier1));
        Assert.Equal("II", RankMath.TierRoman(CardRankLevel.Tier2));
        Assert.Equal("III", RankMath.TierRoman(CardRankLevel.Tier3));
    }
}
