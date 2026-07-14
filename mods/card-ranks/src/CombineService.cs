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
    public static bool AllowBasics => MainFile.Config?.AllowCombineStrikeDefend ?? false;

    public static CardRankLevel GetRank(CardModel card) => card.Enchantment switch
    {
        SecondRank => CardRankLevel.Rank2,
        ThirdRank => CardRankLevel.Rank3,
        RankEnchantment re => re.Rank,
        _ => CardRankLevel.None,
    };

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
        IEnumerable<RankCardView> views = player.Deck.Cards.Select(ToView);
        return RankMath.DeckHasCombinablePair(views, AllowBasics);
    }

    public static async Task ApplyLocalAsync(CardModel sacrifice, CardModel survivor)
    {
        if (!CanPair(sacrifice, survivor))
            throw new InvalidOperationException("Cards are not a legal combine pair.");

        bool eitherUpgraded = sacrifice.IsUpgraded || survivor.IsUpgraded;
        CardRankLevel resultRank = RankMath.NextRank(GetRank(survivor));

        await CardPileCmd.RemoveFromDeck(sacrifice, showPreview: false);
        ApplyRankEnchantment(survivor, resultRank);
        if (eitherUpgraded && !survivor.IsUpgraded)
            CardCmd.Upgrade(survivor, CardPreviewStyle.None);

        MainFile.Logger.Info(
            $"Combined {sacrifice.Id} → survivor rank {resultRank} (upgraded={eitherUpgraded}).");
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

    private static void ApplyRankEnchantment(CardModel card, CardRankLevel rank)
    {
        CardCmd.ClearEnchantment(card);
        switch (rank)
        {
            case CardRankLevel.Rank2:
                CardCmd.Enchant<SecondRank>(card, 1m);
                break;
            case CardRankLevel.Rank3:
                CardCmd.Enchant<ThirdRank>(card, 1m);
                break;
        }
    }

    private static CardModel? FindCard(
        Player player,
        string category,
        string entry,
        CardRankLevel rank,
        int upgradeLevel,
        CardModel? exclude = null)
    {
        var matches = player.Deck.Cards
            .Where(c => c != exclude
                        && c.Id.Category == category
                        && c.Id.Entry == entry
                        && GetRank(c) == rank)
            .ToList();

        return matches.FirstOrDefault(c => c.CurrentUpgradeLevel == upgradeLevel)
               ?? matches.FirstOrDefault();
    }
}
