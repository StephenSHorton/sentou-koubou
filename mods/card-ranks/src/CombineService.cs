using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace CardRanks;

/// <summary>
/// Combine path: three matching same-tier cards → keep one, sacrifice two, raise tier.
/// Ladder: plain → Tier I (blue) → Tier II → Tier III.
/// </summary>
public static class CombineService
{
    public static bool AllowBasics => CardRanksConfig.AllowCombineStrikeDefend;

    public static int CardsPerCombine => RankMath.CardsPerCombine;

    private static readonly ConditionalWeakTable<CardModel, RankBox> TrackedRanks = new();

    private sealed class RankBox
    {
        public CardRankLevel Rank;
    }

    public static CardRankLevel GetRank(CardModel card)
    {
        CardRankLevel detected = MultiEnchantCompat.DetectRank(card);
        if (detected != CardRankLevel.None)
        {
            Track(card, detected);
            return detected;
        }

        if (card.Enchantment == null
            && TrackedRanks.TryGetValue(card, out RankBox? box)
            && box.Rank != CardRankLevel.None)
            return box.Rank;

        return CardRankLevel.None;
    }

    public static void Track(CardModel card, CardRankLevel rank) =>
        TrackedRanks.GetOrCreateValue(card).Rank = rank;

    public static CardRankLevel RankFromEnchantment(EnchantmentModel? enchantment)
    {
        if (enchantment == null)
            return CardRankLevel.None;
        if (MultiEnchantCompat.IsMultiEnchantment(enchantment))
            return CardRankLevel.None;
        return MultiEnchantCompat.RankFromLeaf(enchantment);
    }

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

    public static string CardKey(CardModel card) => $"{card.Id.Category}/{card.Id.Entry}";

    public static RankCardView ToView(CardModel card) =>
        new(CardKey(card), GetRank(card), IsBasicLike(card), card.CurrentUpgradeLevel);

    public static bool SameCardIdentity(CardModel a, CardModel b) =>
        a.Id.Equals(b.Id) || string.Equals(CardKey(a), CardKey(b), StringComparison.Ordinal);

    public static bool IsCandidate(CardModel card) =>
        RankMath.IsCandidate(ToView(card), AllowBasics);

    public static bool CanPair(CardModel a, CardModel b)
    {
        if (ReferenceEquals(a, b))
            return false;
        if (!SameCardIdentity(a, b))
            return false;

        CardRankLevel ra = GetRank(a);
        CardRankLevel rb = GetRank(b);
        if (ra != rb)
        {
            MainFile.Logger.Info(
                $"CanPair: rank mismatch {ra} vs {rb} | {Describe(a)} || {Describe(b)}");
            return false;
        }

        if (!RankMath.IsCandidate(ra, IsBasicLike(a), AllowBasics)
            || !RankMath.IsCandidate(rb, IsBasicLike(b), AllowBasics))
        {
            MainFile.Logger.Info($"CanPair: not a candidate {ra}");
            return false;
        }

        return true;
    }

    public static bool CanGroup(IReadOnlyList<CardModel> cards)
    {
        if (cards.Count != CardsPerCombine)
            return false;
        for (int i = 0; i < cards.Count; i++)
        {
            for (int j = i + 1; j < cards.Count; j++)
            {
                if (!CanPair(cards[i], cards[j]))
                    return false;
            }
        }
        return true;
    }

    public static string Describe(CardModel card)
    {
        var e = card.Enchantment;
        string leaves = string.Join("+",
            MultiEnchantCompat.EnumerateLeafEnchantments(card).Select(l => l.GetType().Name));
        return $"{card.Id} tier={RankMath.TierRoman(GetRank(card))}({GetRank(card)}) " +
               $"up={card.CurrentUpgradeLevel} ench={e?.GetType().Name ?? "null"} " +
               $"leaves=[{leaves}] amount={e?.Amount.ToString() ?? "-"} " +
               $"bonuses=[{string.Join(",", TierBonusService.GetAll(card))}]";
    }

    public static bool DeckHasCombinablePair(Player player) =>
        RankMath.DeckHasCombinableGroup(GetDeckCards(player).Select(ToView), AllowBasics);

    public static bool OnlyBlockedByBasicsPolicy(Player player) =>
        RankMath.OnlyBlockedByBasicsPolicy(GetDeckCards(player).Select(ToView), AllowBasics);

    public static IReadOnlyList<CardModel> GetDeckCards(Player player) =>
        PileType.Deck.GetPile(player).Cards;

    /// <summary>
    /// Deck cards that sit in a full combine bucket (same id + tier, count ≥ 3).
    /// Shown in the rest-site combine picker so singles / pairs are not listed.
    /// </summary>
    public static List<CardModel> GetCombinableDeckCards(Player player)
    {
        List<CardModel> deck = GetDeckCards(player).ToList();
        var buckets = new Dictionary<(string Id, CardRankLevel Rank), List<CardModel>>();
        foreach (CardModel card in deck)
        {
            if (!IsCandidate(card))
                continue;
            var key = (CardKey(card), GetRank(card));
            if (!buckets.TryGetValue(key, out List<CardModel>? list))
            {
                list = [];
                buckets[key] = list;
            }

            list.Add(card);
        }

        return buckets.Values
            .Where(list => list.Count >= CardsPerCombine)
            .SelectMany(list => list)
            .ToList();
    }

    /// <summary>
    /// True when <paramref name="card"/> plus matching peers in
    /// <paramref name="pool"/> can form a full combine group.
    /// </summary>
    public static bool CanStartCombineWith(CardModel card, IReadOnlyList<CardModel> pool)
    {
        if (!IsCandidate(card))
            return false;

        int matches = 0;
        foreach (CardModel other in pool)
        {
            if (ReferenceEquals(card, other) || CanPair(card, other))
                matches++;
            if (matches >= CardsPerCombine)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Pick survivor and the two sacrifices from a 3-card selection.
    /// Prefer the copy that already has non-rank enchantments (Spiral etc.), then
    /// highest upgrade — so combine does not throw away event/shop enchantments.
    /// </summary>
    public static void SplitSurvivorAndSacrifices(
        IReadOnlyList<CardModel> picked,
        out CardModel survivor,
        out CardModel sacrifice1,
        out CardModel sacrifice2)
    {
        if (picked.Count != CardsPerCombine)
            throw new ArgumentException($"Need {CardsPerCombine} cards, got {picked.Count}");

        List<CardModel> ordered = picked
            .OrderByDescending(MultiEnchantCompat.CountNonRankLeaves)
            .ThenByDescending(c => c.CurrentUpgradeLevel)
            .ThenByDescending(c => c.Enchantment != null)
            .ToList();
        survivor = ordered[0];
        sacrifice1 = ordered[1];
        sacrifice2 = ordered[2];

        if (MultiEnchantCompat.CountNonRankLeaves(survivor) > 0
            || MultiEnchantCompat.CountNonRankLeaves(sacrifice1) > 0
            || MultiEnchantCompat.CountNonRankLeaves(sacrifice2) > 0)
        {
            MainFile.Logger.Info(
                $"Combine survivor pick: keep {Describe(survivor)} " +
                $"(sac {Describe(sacrifice1)} | {Describe(sacrifice2)})");
        }
    }

    /// <summary>
    /// Apply tier + upgrades on survivor. Does not remove sacrifices (reveal path does).
    /// Merges tier bonuses from both sacrifices onto the survivor first.
    /// </summary>
    public static Task ApplyLocalAsync(
        CardModel sacrifice1, CardModel sacrifice2, CardModel survivor)
    {
        List<CardModel> group = [sacrifice1, sacrifice2, survivor];
        if (!CanGroup(group))
            throw new InvalidOperationException(
                $"Illegal triple: {Describe(sacrifice1)} | {Describe(sacrifice2)} | {Describe(survivor)}");

        // Carry tier-bonus flags + real non-rank enchant leaves (Spiral, Steady, …)
        // from sacrificed copies so combine does not bulldoze existing enchants.
        TierBonusService.MergeFrom(sacrifice1, survivor);
        TierBonusService.MergeFrom(sacrifice2, survivor);
        MultiEnchantCompat.MergeNonRankLeaves(sacrifice1, survivor);
        MultiEnchantCompat.MergeNonRankLeaves(sacrifice2, survivor);

        int maxUp = Math.Max(
            Math.Max(survivor.MaxUpgradeLevel, sacrifice1.MaxUpgradeLevel),
            Math.Max(sacrifice2.MaxUpgradeLevel,
                sacrifice1.CurrentUpgradeLevel + sacrifice2.CurrentUpgradeLevel
                + survivor.CurrentUpgradeLevel));

        List<RankCardView> views = group.Select(ToView).ToList();
        if (!RankMath.TryPlanCombine(views, AllowBasics, maxUp,
                out CardRankLevel resultRank, out int resultUpgrade))
            throw new InvalidOperationException("TryPlanCombine failed.");

        int up1 = sacrifice1.CurrentUpgradeLevel;
        int up2 = sacrifice2.CurrentUpgradeLevel;
        int upS = survivor.CurrentUpgradeLevel;

        ApplyRankEnchantment(survivor, resultRank);
        ApplyUpgradeLevel(survivor, resultUpgrade);

        if (GetRank(survivor) != resultRank)
            throw new InvalidOperationException(
                $"Rank apply failed (wanted {resultRank}, got {GetRank(survivor)}); " +
                $"sacrifices kept. {Describe(survivor)}");

        MainFile.Logger.Info(
            $"Combined OK → Tier {RankMath.TierRoman(resultRank)} " +
            $"up {up1}+{up2}+{upS}→{resultUpgrade} | {Describe(survivor)}");
        return Task.CompletedTask;
    }

    public static async Task RemoveSacrificeAsync(CardModel sacrifice, bool showPreview = false)
    {
        try
        {
            await CardPileCmd.RemoveFromDeck(sacrifice, showPreview: showPreview);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"RemoveSacrifice: {e.Message}");
            try
            {
                await CardPileCmd.RemoveFromDeck(sacrifice, showPreview: false);
            }
            catch (Exception e2)
            {
                MainFile.Logger.Error($"RemoveSacrifice hard fail: {e2}");
            }
        }

        TrackedRanks.GetOrCreateValue(sacrifice).Rank = CardRankLevel.None;
    }

    public static async Task RemoveSacrificesAsync(
        CardModel sacrifice1, CardModel sacrifice2, bool showPreview = false)
    {
        await RemoveSacrificeAsync(sacrifice1, showPreview);
        await RemoveSacrificeAsync(sacrifice2, showPreview: false);
    }

    public static async Task ApplyRemoteAsync(Player player, CombineCardsMessage msg)
    {
        CardModel? sac1 = FindCard(player, msg.category, msg.entry, (CardRankLevel)msg.sacrifice1Rank,
            msg.sacrifice1Upgrade);
        CardModel? sac2 = FindCard(player, msg.category, msg.entry, (CardRankLevel)msg.sacrifice2Rank,
            msg.sacrifice2Upgrade, exclude: sac1);
        CardModel? survivor = FindCard(player, msg.category, msg.entry, (CardRankLevel)msg.survivorRank,
            msg.survivorUpgrade, exclude: sac1, exclude2: sac2);

        if (sac1 == null || sac2 == null || survivor == null)
        {
            MainFile.Logger.Warn("Remote combine could not resolve cards.");
            return;
        }

        if (!CanGroup([sac1, sac2, survivor]))
        {
            MainFile.Logger.Warn(
                $"Remote combine rejected: {Describe(sac1)} | {Describe(sac2)} | {Describe(survivor)}");
            return;
        }

        TierBonusService.MergeFrom(sac1, survivor);
        TierBonusService.MergeFrom(sac2, survivor);
        MultiEnchantCompat.MergeNonRankLeaves(sac1, survivor);
        MultiEnchantCompat.MergeNonRankLeaves(sac2, survivor);

        ApplyRankEnchantment(survivor, (CardRankLevel)msg.resultRank);
        ApplyUpgradeLevel(survivor, msg.resultUpgradeLevel);
        if (GetRank(survivor) != (CardRankLevel)msg.resultRank)
        {
            MainFile.Logger.Error("Remote rank apply failed; not removing sacrifices.");
            return;
        }

        if (msg.bonusRolled != 0)
            TierBonusService.Apply(survivor, (TierBonus)msg.bonusRolled);

        await RemoveSacrificesAsync(sac1, sac2);
    }

    private static void ApplyRankEnchantment(CardModel card, CardRankLevel rank)
    {
        if (rank is not (CardRankLevel.Tier1 or CardRankLevel.Tier2 or CardRankLevel.Tier3))
            return;

        MultiEnchantCompat.StripRankLeaves(card);

        EnchantmentModel? prototype = CreateRankPrototype(rank);
        if (prototype == null)
            throw new InvalidOperationException($"Could not create rank prototype for {rank}");

        prototype.Amount = 1;

        bool appliedIntoMulti = false;
        if (MultiEnchantCompat.IsMultiEnchantment(card.Enchantment))
        {
            // Multi-add may not bind Card; ModifyCard requires it or rank hooks mis-fire.
            if (!prototype.HasCard)
            {
                try
                {
                    prototype.Card = card;
                }
                catch (Exception e)
                {
                    MainFile.Logger.Warn($"Could not bind rank prototype Card: {e.Message}");
                }
            }

            appliedIntoMulti = MultiEnchantCompat.TryAddIntoMulti(card, prototype);
            if (appliedIntoMulti)
            {
                try
                {
                    if (prototype.HasCard)
                        prototype.ModifyCard();
                }
                catch (Exception e)
                {
                    MainFile.Logger.Warn($"Rank ModifyCard after multi-add: {e.Message}");
                }
            }
        }

        if (!appliedIntoMulti)
        {
            if (card.Enchantment != null && MultiEnchantCompat.IsRankLeaf(card.Enchantment))
                MultiEnchantCompat.HardClearTop(card);

            try
            {
                EnchantmentModel? applied = rank switch
                {
                    CardRankLevel.Tier3 => CardCmd.Enchant<ThirdRank>(card, 1m),
                    CardRankLevel.Tier2 => CardCmd.Enchant<SecondRank>(card, 1m),
                    CardRankLevel.Tier1 => CardCmd.Enchant<FirstRank>(card, 1m),
                    _ => null,
                };
                if (applied != null)
                    applied.Amount = 1;
            }
            catch (Exception e)
            {
                // Do not HardClearTop here — that wiped Spiral/other non-rank enchants.
                // StripRankLeaves already removed only rank leaves; retry Enchant only.
                MainFile.Logger.Warn($"Enchant failed ({rank}): {e.Message}; retry without clear.");
                try
                {
                    EnchantmentModel? applied = rank switch
                    {
                        CardRankLevel.Tier3 => CardCmd.Enchant<ThirdRank>(card, 1m),
                        CardRankLevel.Tier2 => CardCmd.Enchant<SecondRank>(card, 1m),
                        _ => CardCmd.Enchant<FirstRank>(card, 1m),
                    };
                    if (applied != null)
                        applied.Amount = 1;
                }
                catch (Exception e2)
                {
                    MainFile.Logger.Error($"Enchant retry failed ({rank}): {e2.Message}");
                    throw;
                }
            }
        }

        foreach (EnchantmentModel leaf in MultiEnchantCompat.EnumerateLeafEnchantments(card))
        {
            if (MultiEnchantCompat.IsRankLeaf(leaf) && leaf.Amount != 1)
                leaf.Amount = 1;
        }

        Track(card, rank);

        CardRankLevel now = GetRank(card);
        MainFile.Logger.Info(
            $"ApplyRankEnchantment wanted={rank} now={now} type={card.Enchantment?.GetType().Name} " +
            $"multi={MultiEnchantCompat.IsMultiEnchantment(card.Enchantment)} | {Describe(card)}");

        if (now != rank)
            throw new InvalidOperationException(
                $"Rank mismatch after apply: wanted {rank}, got {now} ({Describe(card)})");
    }

    private static EnchantmentModel? CreateRankPrototype(CardRankLevel rank)
    {
        try
        {
            return rank switch
            {
                CardRankLevel.Tier3 => ModelDb.Enchantment<ThirdRank>().ToMutable(),
                CardRankLevel.Tier2 => ModelDb.Enchantment<SecondRank>().ToMutable(),
                CardRankLevel.Tier1 => ModelDb.Enchantment<FirstRank>().ToMutable(),
                _ => null,
            };
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"CreateRankPrototype {rank}: {e}");
            return null;
        }
    }

    private static void ApplyUpgradeLevel(CardModel survivor, int targetLevel)
    {
        if (targetLevel < 0)
            targetLevel = 0;
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
                MainFile.Logger.Warn($"Upgrade failed: {e.Message}");
                break;
            }
            if (survivor.CurrentUpgradeLevel <= before)
            {
                try
                {
                    survivor.CurrentUpgradeLevel = targetLevel;
                }
                catch
                {
                    // ignore
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
        CardModel? exclude = null,
        CardModel? exclude2 = null)
    {
        var matches = GetDeckCards(player)
            .Where(c => c != exclude
                        && c != exclude2
                        && c.Id.Category == category
                        && c.Id.Entry == entry
                        && GetRank(c) == rank)
            .ToList();
        return matches.FirstOrDefault(c => c.CurrentUpgradeLevel == upgradeLevel)
               ?? matches.FirstOrDefault();
    }
}
