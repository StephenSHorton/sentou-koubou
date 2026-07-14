using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace CardRanks;

/// <summary>
/// Single mutation path for combine: local rest-site success and multiplayer mirrors.
/// </summary>
public static class CombineService
{
    public static bool AllowBasics => CardRanksConfig.AllowCombineStrikeDefend;

    // BaseLib model ids look like CARDRANKS-SECOND_RANK (see RankUpCards2's Id.Entry checks).
    public const string SecondRankEntry = "CARDRANKS-SECOND_RANK";
    public const string ThirdRankEntry = "CARDRANKS-THIRD_RANK";

    /// <summary>
    /// Resolve rank from the live enchantment. Prefer type checks, fall back to ModelId.Entry
    /// (RankUpCards2 style) so we never treat ranked cards as plain — that allowed mixed-tier
    /// pairs and re-applied Rank 2 (which stacks Amount instead of upgrading).
    /// </summary>
    public static CardRankLevel GetRank(CardModel card) => RankFromEnchantment(card.Enchantment);

    public static CardRankLevel RankFromEnchantment(EnchantmentModel? enchantment)
    {
        if (enchantment == null)
            return CardRankLevel.None;

        switch (enchantment)
        {
            case ThirdRank:
                return CardRankLevel.Rank3;
            case SecondRank:
                return CardRankLevel.Rank2;
            case RankEnchantment ranked:
                return ranked.Rank;
        }

        string entry = enchantment.Id.Entry ?? "";
        if (IsThirdRankEntry(entry))
            return CardRankLevel.Rank3;
        if (IsSecondRankEntry(entry))
            return CardRankLevel.Rank2;

        // Last resort: type name (handles unexpected id prefixes).
        string typeName = enchantment.GetType().Name;
        if (typeName.Contains("ThirdRank", StringComparison.OrdinalIgnoreCase))
            return CardRankLevel.Rank3;
        if (typeName.Contains("SecondRank", StringComparison.OrdinalIgnoreCase))
            return CardRankLevel.Rank2;

        return CardRankLevel.None;
    }

    public static bool IsSecondRankEntry(string entry) =>
        entry.Equals(SecondRankEntry, StringComparison.OrdinalIgnoreCase)
        || entry.EndsWith("-SECOND_RANK", StringComparison.OrdinalIgnoreCase)
        || entry.EndsWith("SECOND_RANK", StringComparison.OrdinalIgnoreCase);

    public static bool IsThirdRankEntry(string entry) =>
        entry.Equals(ThirdRankEntry, StringComparison.OrdinalIgnoreCase)
        || entry.EndsWith("-THIRD_RANK", StringComparison.OrdinalIgnoreCase)
        || entry.EndsWith("THIRD_RANK", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Vanilla basics plus modded starter basics: Rarity Basic with Strike/Defend tag,
    /// or the game's IsBasicStrikeOrDefend flag.
    /// </summary>
    public static bool IsBasicLike(CardModel card)
    {
        if (card.IsBasicStrikeOrDefend)
            return true;
        if (card.Rarity != CardRarity.Basic)
            return false;
        foreach (CardTag tag in card.Tags)
        {
            if (tag is CardTag.Strike or CardTag.Defend)
                return true;
        }
        return false;
    }

    public static RankCardView ToView(CardModel card) =>
        new($"{card.Id.Category}/{card.Id.Entry}", GetRank(card), IsBasicLike(card));

    public static bool IsCandidate(CardModel card) =>
        RankMath.IsCandidate(ToView(card), AllowBasics);

    public static bool CanPair(CardModel a, CardModel b) =>
        RankMath.CanPair(ToView(a), ToView(b), AllowBasics);

    public static bool DeckHasCombinablePair(Player player)
    {
        IEnumerable<RankCardView> views = GetDeckCards(player).Select(ToView);
        return RankMath.DeckHasCombinablePair(views, AllowBasics);
    }

    public static bool OnlyBlockedByBasicsPolicy(Player player)
    {
        IEnumerable<RankCardView> views = GetDeckCards(player).Select(ToView);
        return RankMath.OnlyBlockedByBasicsPolicy(views, AllowBasics);
    }

    /// <summary>Master deck pile (rest-site master list), not draw/discard combat piles.</summary>
    public static IReadOnlyList<CardModel> GetDeckCards(Player player)
    {
        // Prefer PileType.Deck — same source RankUpCards2 used; player.Deck is usually
        // the same reference but GetPile is the stable rest-site master list.
        return PileType.Deck.GetPile(player).Cards;
    }

    public static async Task ApplyLocalAsync(CardModel sacrifice, CardModel survivor)
    {
        CardRankLevel sacRank = GetRank(sacrifice);
        CardRankLevel survRank = GetRank(survivor);
        if (!CanPair(sacrifice, survivor))
            throw new InvalidOperationException(
                $"Cards are not a legal combine pair (sac={sacRank}, surv={survRank}, " +
                $"enchS={sacrifice.Enchantment?.Id.Entry}, enchV={survivor.Enchantment?.Id.Entry}).");

        bool eitherUpgraded = sacrifice.IsUpgraded || survivor.IsUpgraded;
        // Same-tier only (enforced by CanPair); ladder is plain→R2, R2→R3.
        CardRankLevel resultRank = RankMath.NextRank(survRank);

        await CardPileCmd.RemoveFromDeck(sacrifice, showPreview: false);
        ApplyRankEnchantment(survivor, resultRank);
        if (eitherUpgraded && !survivor.IsUpgraded)
            CardCmd.Upgrade(survivor, CardPreviewStyle.None);

        MainFile.Logger.Info(
            $"Combined {sacrifice.Id} {sacRank}+{survRank} → {resultRank} " +
            $"(upgraded={eitherUpgraded}, now={GetRank(survivor)}, " +
            $"entry={survivor.Enchantment?.Id.Entry}, amount={survivor.Enchantment?.Amount}).");
    }

    /// <summary>
    /// Deterministic peer apply from network payload (no CardModel identity across clients).
    /// </summary>
    public static async Task ApplyRemoteAsync(Player player, CombineCardsMessage msg)
    {
        CardModel? sacrifice = FindCard(player, msg.category, msg.entry, (CardRankLevel)msg.sacrificeRank,
            msg.sacrificeUpgrade);
        CardModel? survivor = FindCard(player, msg.category, msg.entry, (CardRankLevel)msg.survivorRank,
            msg.survivorUpgrade, exclude: sacrifice);

        if (sacrifice == null || survivor == null)
        {
            MainFile.Logger.Warn(
                $"Remote combine could not resolve cards for {msg.category}/{msg.entry} " +
                $"(sac rank={msg.sacrificeRank} up={msg.sacrificeUpgrade}, " +
                $"surv rank={msg.survivorRank} up={msg.survivorUpgrade}).");
            return;
        }

        await CardPileCmd.RemoveFromDeck(sacrifice, showPreview: false);
        ApplyRankEnchantment(survivor, (CardRankLevel)msg.resultRank);
        if (msg.resultUpgraded && !survivor.IsUpgraded)
            CardCmd.Upgrade(survivor, CardPreviewStyle.None);
    }

    public static CombineCardsMessage BuildMessage(CardModel sacrifice, CardModel survivor, Player owner)
    {
        bool eitherUpgraded = sacrifice.IsUpgraded || survivor.IsUpgraded;
        CardRankLevel resultRank = RankMath.NextRank(GetRank(survivor));
        return new CombineCardsMessage
        {
            ownerNetId = owner.NetId,
            category = sacrifice.Id.Category,
            entry = sacrifice.Id.Entry,
            sacrificeRank = (int)GetRank(sacrifice),
            sacrificeUpgrade = sacrifice.CurrentUpgradeLevel,
            survivorRank = (int)GetRank(survivor),
            survivorUpgrade = survivor.CurrentUpgradeLevel,
            resultRank = (int)resultRank,
            resultUpgraded = eitherUpgraded || survivor.IsUpgraded,
        };
    }

    /// <summary>
    /// Replace any existing enchant with exactly one Rank 2/3 instance.
    /// CardCmd.Enchant stacks Amount when the same type is already present — that is
    /// what made "Rank 2" appear twice when we failed to clear / re-applied Rank 2.
    /// </summary>
    private static void ApplyRankEnchantment(CardModel card, CardRankLevel rank)
    {
        if (rank is not (CardRankLevel.Rank2 or CardRankLevel.Rank3))
            return;

        // Always clear first so we never hit the same-type amount-stack branch.
        if (card.Enchantment != null)
            CardCmd.ClearEnchantment(card);

        // Belt-and-suspenders: publicized setter if Clear left anything (shouldn't).
        if (card.Enchantment != null)
        {
            MainFile.Logger.Warn(
                $"ClearEnchantment left {card.Enchantment.Id.Entry}; forcing null before re-rank.");
            card.Enchantment = null;
        }

        EnchantmentModel? applied = rank switch
        {
            CardRankLevel.Rank3 => CardCmd.Enchant<ThirdRank>(card, 1m),
            _ => CardCmd.Enchant<SecondRank>(card, 1m),
        };

        // Never display stacked amounts as a fake "double rank".
        if (applied != null && applied.Amount != 1)
            applied.Amount = 1;
    }

    private static CardModel? FindCard(
        Player player,
        string category,
        string entry,
        CardRankLevel rank,
        int upgradeLevel,
        CardModel? exclude = null)
    {
        var matches = GetDeckCards(player)
            .Where(c => c != exclude
                        && c.Id.Category == category
                        && c.Id.Entry == entry
                        && GetRank(c) == rank)
            .ToList();

        return matches.FirstOrDefault(c => c.CurrentUpgradeLevel == upgradeLevel)
               ?? matches.FirstOrDefault();
    }
}
