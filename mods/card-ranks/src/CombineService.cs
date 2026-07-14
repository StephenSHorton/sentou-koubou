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

    public const string SecondRankEntry = "CARDRANKS-SECOND_RANK";
    public const string ThirdRankEntry = "CARDRANKS-THIRD_RANK";

    /// <summary>
    /// Resolve rank from the live enchantment. Uses type hierarchy, ModelId, full id string,
    /// and Amount (stacked SecondRank amount≥2 is treated as Rank3 — the old double-badge bug).
    /// </summary>
    public static CardRankLevel GetRank(CardModel card) => RankFromEnchantment(card.Enchantment);

    public static CardRankLevel RankFromEnchantment(EnchantmentModel? enchantment)
    {
        if (enchantment == null)
            return CardRankLevel.None;

        // Explicit types first.
        if (enchantment is ThirdRank)
            return CardRankLevel.Rank3;
        if (enchantment is SecondRank second)
        {
            // Old bug stacked Amount on SecondRank instead of promoting to ThirdRank.
            if (second.Amount >= 2)
                return CardRankLevel.Rank3;
            return CardRankLevel.Rank2;
        }
        if (enchantment is RankEnchantment ranked)
            return ranked.Rank;

        // Walk runtime type names (handles unexpected wrappers / name variants).
        for (Type? t = enchantment.GetType(); t != null && t != typeof(object); t = t.BaseType)
        {
            string n = t.Name;
            if (n.Contains("ThirdRank", StringComparison.OrdinalIgnoreCase)
                || n.Contains("OriginalThird", StringComparison.OrdinalIgnoreCase))
                return CardRankLevel.Rank3;
            if (n.Contains("SecondRank", StringComparison.OrdinalIgnoreCase)
                || n.Contains("OriginalSecond", StringComparison.OrdinalIgnoreCase))
            {
                if (enchantment.Amount >= 2)
                    return CardRankLevel.Rank3;
                return CardRankLevel.Rank2;
            }
        }

        string entry = enchantment.Id.Entry ?? "";
        string category = enchantment.Id.Category ?? "";
        string idBlob = $"{category}.{entry}|{enchantment.Id}|{enchantment.GetType().FullName}";

        if (RankMath.LooksLikeThirdRank(idBlob))
            return CardRankLevel.Rank3;
        if (RankMath.LooksLikeSecondRank(idBlob))
        {
            if (enchantment.Amount >= 2)
                return CardRankLevel.Rank3;
            return CardRankLevel.Rank2;
        }

        return CardRankLevel.None;
    }

    public static bool IsSecondRankEntry(string entry) => RankMath.LooksLikeSecondRank(entry);

    public static bool IsThirdRankEntry(string entry) => RankMath.LooksLikeThirdRank(entry);

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
        new(
            $"{card.Id.Category}/{card.Id.Entry}",
            GetRank(card),
            IsBasicLike(card),
            card.CurrentUpgradeLevel);

    public static bool IsCandidate(CardModel card) =>
        RankMath.IsCandidate(ToView(card), AllowBasics);

    public static bool CanPair(CardModel a, CardModel b)
    {
        bool ok = RankMath.CanPair(ToView(a), ToView(b), AllowBasics);
        if (!ok)
        {
            MainFile.Logger.Info(
                $"CanPair reject: {Describe(a)} vs {Describe(b)}");
        }
        return ok;
    }

    public static string Describe(CardModel card)
    {
        var e = card.Enchantment;
        return $"{card.Id} rank={GetRank(card)} up={card.CurrentUpgradeLevel} " +
               $"enchType={e?.GetType().Name ?? "null"} " +
               $"enchId={e?.Id.ToString() ?? "null"} " +
               $"entry={e?.Id.Entry ?? "null"} amount={e?.Amount.ToString() ?? "-"}";
    }

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
    public static IReadOnlyList<CardModel> GetDeckCards(Player player) =>
        PileType.Deck.GetPile(player).Cards;

    public static async Task ApplyLocalAsync(CardModel sacrifice, CardModel survivor)
    {
        CardRankLevel sacRank = GetRank(sacrifice);
        CardRankLevel survRank = GetRank(survivor);

        if (sacRank != survRank)
            throw new InvalidOperationException(
                $"Mixed ranks cannot combine: {Describe(sacrifice)} vs {Describe(survivor)}");

        if (!CanPair(sacrifice, survivor))
            throw new InvalidOperationException(
                $"Cards are not a legal combine pair: {Describe(sacrifice)} vs {Describe(survivor)}");

        int maxUp = Math.Max(survivor.MaxUpgradeLevel, sacrifice.MaxUpgradeLevel);
        // Uncapped / high caps: allow at least the sum.
        maxUp = Math.Max(maxUp, sacrifice.CurrentUpgradeLevel + survivor.CurrentUpgradeLevel);

        if (!RankMath.TryPlanCombine(
                ToView(sacrifice), ToView(survivor), AllowBasics, maxUp,
                out CardRankLevel resultRank, out int resultUpgrade))
            throw new InvalidOperationException("TryPlanCombine failed after CanPair succeeded.");

        // Snapshot upgrade levels before mutation (logging + safety).
        int sacUp = sacrifice.CurrentUpgradeLevel;
        int survUp = survivor.CurrentUpgradeLevel;

        // Enchant/upgrade FIRST, then remove sacrifice. Never delete a card if rank-up fails.
        ApplyRankEnchantment(survivor, resultRank);
        ApplyUpgradeLevel(survivor, resultUpgrade);

        CardRankLevel now = GetRank(survivor);
        if (now != resultRank)
            throw new InvalidOperationException(
                $"Rank apply failed (wanted {resultRank}, got {now}); sacrifice not removed. {Describe(survivor)}");

        await CardPileCmd.RemoveFromDeck(sacrifice, showPreview: false);

        MainFile.Logger.Info(
            $"Combined OK: {sacrifice.Id} {sacRank}+{survRank} → rank {resultRank} " +
            $"upgrade {sacUp}+{survUp}→{resultUpgrade} | now {Describe(survivor)}");
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

        // Re-validate on peer so desync cannot create mixed-rank stacks.
        if (GetRank(sacrifice) != GetRank(survivor) || !CanPair(sacrifice, survivor))
        {
            MainFile.Logger.Warn(
                $"Remote combine rejected pair: {Describe(sacrifice)} vs {Describe(survivor)}");
            return;
        }

        ApplyRankEnchantment(survivor, (CardRankLevel)msg.resultRank);
        ApplyUpgradeLevel(survivor, msg.resultUpgradeLevel);
        if (GetRank(survivor) != (CardRankLevel)msg.resultRank)
        {
            MainFile.Logger.Error(
                $"Remote rank apply failed; not removing sacrifice. {Describe(survivor)}");
            return;
        }
        await CardPileCmd.RemoveFromDeck(sacrifice, showPreview: false);
    }

    public static CombineCardsMessage BuildMessage(CardModel sacrifice, CardModel survivor, Player owner)
    {
        int maxUp = Math.Max(
            Math.Max(survivor.MaxUpgradeLevel, sacrifice.MaxUpgradeLevel),
            sacrifice.CurrentUpgradeLevel + survivor.CurrentUpgradeLevel);
        RankMath.TryPlanCombine(
            ToView(sacrifice), ToView(survivor), AllowBasics, maxUp,
            out CardRankLevel resultRank, out int resultUpgrade);

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
            resultUpgradeLevel = resultUpgrade,
        };
    }

    /// <summary>
    /// Replace any existing enchant with exactly one Rank 2/3 instance (never stack Amount).
    /// </summary>
    private static void ApplyRankEnchantment(CardModel card, CardRankLevel rank)
    {
        if (rank is not (CardRankLevel.Rank2 or CardRankLevel.Rank3))
            return;

        ForceClearEnchantment(card);

        EnchantmentModel? applied = rank switch
        {
            CardRankLevel.Rank3 => CardCmd.Enchant<ThirdRank>(card, 1m),
            _ => CardCmd.Enchant<SecondRank>(card, 1m),
        };

        if (applied != null && applied.Amount != 1)
            applied.Amount = 1;

        // Normalize legacy double-SecondRank stacks if something re-stacked.
        if (rank == CardRankLevel.Rank3
            && card.Enchantment is SecondRank { Amount: >= 2 })
        {
            ForceClearEnchantment(card);
            applied = CardCmd.Enchant<ThirdRank>(card, 1m);
            if (applied != null)
                applied.Amount = 1;
        }

        CardRankLevel now = GetRank(card);
        if (now != rank)
        {
            MainFile.Logger.Error(
                $"ApplyRankEnchantment expected {rank} but got {now} on {Describe(card)}");
        }
    }

    private static void ForceClearEnchantment(CardModel card)
    {
        if (card.Enchantment == null)
            return;

        try
        {
            CardCmd.ClearEnchantment(card);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"ClearEnchantment threw: {e.Message}");
        }

        if (card.Enchantment != null)
        {
            MainFile.Logger.Warn(
                $"ClearEnchantment left {card.Enchantment.Id}; forcing null.");
            card.Enchantment = null;
        }
    }

    /// <summary>Raise (or leave) survivor upgrade level to the planned sum.</summary>
    private static void ApplyUpgradeLevel(CardModel survivor, int targetLevel)
    {
        if (targetLevel < 0)
            targetLevel = 0;

        // Prefer CardCmd.Upgrade so upgrade hooks/stats apply.
        int guard = 0;
        while (survivor.CurrentUpgradeLevel < targetLevel && guard++ < 32)
        {
            int before = survivor.CurrentUpgradeLevel;
            try
            {
                CardCmd.Upgrade(survivor, CardPreviewStyle.None);
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"Upgrade step failed at {before}→{targetLevel}: {e.Message}");
                break;
            }
            if (survivor.CurrentUpgradeLevel <= before)
            {
                // Hit a hard cap; try publicized setter as last resort.
                if (survivor.CurrentUpgradeLevel < targetLevel)
                {
                    try
                    {
                        survivor.CurrentUpgradeLevel = targetLevel;
                    }
                    catch
                    {
                        // ignore
                    }
                }
                break;
            }
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
