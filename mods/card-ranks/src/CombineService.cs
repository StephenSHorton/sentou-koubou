using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
// ModelDb used for rank prototypes under MultiEnchantment path.

namespace CardRanks;

/// <summary>
/// Single mutation path for combine: local rest-site success and multiplayer mirrors.
/// Ladder: plain → Tier I (blue) → Tier II → Tier III.
/// </summary>
public static class CombineService
{
    public static bool AllowBasics => CardRanksConfig.AllowCombineStrikeDefend;

    private static readonly ConditionalWeakTable<CardModel, RankBox> TrackedRanks = new();

    private sealed class RankBox
    {
        public CardRankLevel Rank;
    }

    public static CardRankLevel GetRank(CardModel card)
    {
        // Prefer live leaf enchantments (unwrap UncappedSpire MultiEnchantment).
        // Stale tracker alone caused false mismatches after rank-up.
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

        // MultiEnchantment: never use product multipliers (1.5×2=3 false Tier III).
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
        RankMath.DeckHasCombinablePair(GetDeckCards(player).Select(ToView), AllowBasics);

    public static bool OnlyBlockedByBasicsPolicy(Player player) =>
        RankMath.OnlyBlockedByBasicsPolicy(GetDeckCards(player).Select(ToView), AllowBasics);

    public static IReadOnlyList<CardModel> GetDeckCards(Player player) =>
        PileType.Deck.GetPile(player).Cards;

    public static Task ApplyLocalAsync(CardModel sacrifice, CardModel survivor)
    {
        CardRankLevel sacRank = GetRank(sacrifice);
        CardRankLevel survRank = GetRank(survivor);

        if (sacRank != survRank)
            throw new InvalidOperationException(
                $"Mixed ranks cannot combine: {Describe(sacrifice)} vs {Describe(survivor)}");

        if (!CanPair(sacrifice, survivor))
            throw new InvalidOperationException(
                $"Illegal pair: {Describe(sacrifice)} vs {Describe(survivor)}");

        int maxUp = Math.Max(
            Math.Max(survivor.MaxUpgradeLevel, sacrifice.MaxUpgradeLevel),
            sacrifice.CurrentUpgradeLevel + survivor.CurrentUpgradeLevel);

        if (!RankMath.TryPlanCombine(
                ToView(sacrifice), ToView(survivor), AllowBasics, maxUp,
                out CardRankLevel resultRank, out int resultUpgrade))
            throw new InvalidOperationException("TryPlanCombine failed.");

        int sacUp = sacrifice.CurrentUpgradeLevel;
        int survUp = survivor.CurrentUpgradeLevel;

        // Rank + upgrades first (survivor stays in deck). Sacrifice removal is done
        // by the reveal sequence so it can play the exhaust-style preview alone.
        ApplyRankEnchantment(survivor, resultRank);
        ApplyUpgradeLevel(survivor, resultUpgrade);

        if (GetRank(survivor) != resultRank)
            throw new InvalidOperationException(
                $"Rank apply failed (wanted {resultRank}, got {GetRank(survivor)}); " +
                $"sacrifice kept. {Describe(survivor)}");

        MainFile.Logger.Info(
            $"Combined OK → Tier {RankMath.TierRoman(resultRank)} " +
            $"up {sacUp}+{survUp}→{resultUpgrade} | {Describe(survivor)}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Remove sacrifice after the reveal sequence. Default silent (no deck-remove
    /// preview) so it does not stack with the survivor ribbon showcase.
    /// </summary>
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

    public static async Task ApplyRemoteAsync(Player player, CombineCardsMessage msg)
    {
        CardModel? sacrifice = FindCard(player, msg.category, msg.entry, (CardRankLevel)msg.sacrificeRank,
            msg.sacrificeUpgrade);
        CardModel? survivor = FindCard(player, msg.category, msg.entry, (CardRankLevel)msg.survivorRank,
            msg.survivorUpgrade, exclude: sacrifice);

        if (sacrifice == null || survivor == null)
        {
            MainFile.Logger.Warn("Remote combine could not resolve cards.");
            return;
        }

        if (GetRank(sacrifice) != GetRank(survivor) || !CanPair(sacrifice, survivor))
        {
            MainFile.Logger.Warn($"Remote combine rejected: {Describe(sacrifice)} vs {Describe(survivor)}");
            return;
        }

        ApplyRankEnchantment(survivor, (CardRankLevel)msg.resultRank);
        ApplyUpgradeLevel(survivor, msg.resultUpgradeLevel);
        if (GetRank(survivor) != (CardRankLevel)msg.resultRank)
        {
            MainFile.Logger.Error("Remote rank apply failed; not removing sacrifice.");
            return;
        }

        if (msg.bonusRolled != 0)
            TierBonusService.Apply(survivor, (TierBonus)msg.bonusRolled);

        await RemoveSacrificeAsync(sacrifice);
    }

    public static CombineCardsMessage BuildMessage(
        CardModel sacrifice, CardModel survivor, Player owner, TierBonus bonusRolled = TierBonus.None)
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
            bonusRolled = (int)bonusRolled,
        };
    }

    private static void ApplyRankEnchantment(CardModel card, CardRankLevel rank)
    {
        if (rank is not (CardRankLevel.Tier1 or CardRankLevel.Tier2 or CardRankLevel.Tier3))
            return;

        // Under UncappedSpire, CardCmd.Enchant ADDS into MultiEnchantment.
        // Strip previous First/Second/Third leaves first so we never stack ranks
        // (blue+purple dual tabs) or multiply multipliers into a false Tier III.
        MultiEnchantCompat.StripRankLeaves(card);

        EnchantmentModel? prototype = CreateRankPrototype(rank);
        if (prototype == null)
            throw new InvalidOperationException($"Could not create rank prototype for {rank}");

        prototype.Amount = 1;

        bool appliedIntoMulti = false;
        if (MultiEnchantCompat.IsMultiEnchantment(card.Enchantment))
        {
            // Keep non-rank leaves (Spiral, etc.); only add the new tier.
            appliedIntoMulti = MultiEnchantCompat.TryAddIntoMulti(card, prototype);
            if (appliedIntoMulti)
            {
                try
                {
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
            // Lone rank slot, or multi strip emptied the wrapper.
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
                MainFile.Logger.Warn($"Enchant failed ({rank}): {e.Message}; hard clear + retry.");
                MultiEnchantCompat.HardClearTop(card);
                EnchantmentModel? applied = rank switch
                {
                    CardRankLevel.Tier3 => CardCmd.Enchant<ThirdRank>(card, 1m),
                    CardRankLevel.Tier2 => CardCmd.Enchant<SecondRank>(card, 1m),
                    _ => CardCmd.Enchant<FirstRank>(card, 1m),
                };
                if (applied != null)
                    applied.Amount = 1;
            }
        }

        // Clamp Amount=1 on every rank leaf (never stack ribbon counters).
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
