using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

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
        if (TrackedRanks.TryGetValue(card, out RankBox? box) && box.Rank != CardRankLevel.None)
            return box.Rank;

        CardRankLevel detected = RankFromEnchantment(card.Enchantment);
        if (detected != CardRankLevel.None)
            Track(card, detected);
        return detected;
    }

    public static void Track(CardModel card, CardRankLevel rank) =>
        TrackedRanks.GetOrCreateValue(card).Rank = rank;

    public static CardRankLevel RankFromEnchantment(EnchantmentModel? enchantment)
    {
        if (enchantment == null)
            return CardRankLevel.None;

        // Prefer concrete types (Amount must stay 1 — the UI draws one ribbon per Amount).
        switch (enchantment)
        {
            case ThirdRank:
                return CardRankLevel.Tier3;
            case SecondRank:
                return CardRankLevel.Tier2;
            case FirstRank:
                return CardRankLevel.Tier1;
            case RankEnchantment ranked:
                return ranked.Rank;
        }

        try
        {
            decimal mult = enchantment.EnchantBlockMultiplicative(1m);
            if (mult >= 2.9m)
                return CardRankLevel.Tier3;
            if (mult >= 1.9m)
                return CardRankLevel.Tier2;
            if (mult > 1.2m)
                return CardRankLevel.Tier1;
        }
        catch
        {
            // ignore
        }

        string icon = "";
        try
        {
            icon = enchantment.IconPath ?? enchantment.IntendedIconPath ?? "";
        }
        catch
        {
            // ignore
        }
        if (icon.Contains("rank3", StringComparison.OrdinalIgnoreCase))
            return CardRankLevel.Tier3;
        if (icon.Contains("rank2", StringComparison.OrdinalIgnoreCase))
            return CardRankLevel.Tier2;
        if (icon.Contains("rank1", StringComparison.OrdinalIgnoreCase))
            return CardRankLevel.Tier1;

        for (Type? t = enchantment.GetType(); t != null && t != typeof(object); t = t.BaseType)
        {
            string n = t.Name;
            if (n.Contains("ThirdRank", StringComparison.OrdinalIgnoreCase))
                return CardRankLevel.Tier3;
            if (n.Contains("SecondRank", StringComparison.OrdinalIgnoreCase))
                return CardRankLevel.Tier2;
            if (n.Contains("FirstRank", StringComparison.OrdinalIgnoreCase))
                return CardRankLevel.Tier1;
        }

        string entry = enchantment.Id.Entry ?? "";
        string category = enchantment.Id.Category ?? "";
        string idBlob = $"{category}.{entry}|{enchantment.Id}|{enchantment.GetType().FullName}";
        if (RankMath.LooksLikeThirdRank(idBlob))
            return CardRankLevel.Tier3;
        if (RankMath.LooksLikeSecondRank(idBlob))
            return CardRankLevel.Tier2;
        if (RankMath.LooksLikeTier1(idBlob))
            return CardRankLevel.Tier1;

        return CardRankLevel.None;
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
        return $"{card.Id} tier={RankMath.TierRoman(GetRank(card))}({GetRank(card)}) " +
               $"up={card.CurrentUpgradeLevel} ench={e?.GetType().Name ?? "null"} " +
               $"amount={e?.Amount.ToString() ?? "-"} bonuses=[{string.Join(",", TierBonusService.GetAll(card))}]";
    }

    public static bool DeckHasCombinablePair(Player player) =>
        RankMath.DeckHasCombinablePair(GetDeckCards(player).Select(ToView), AllowBasics);

    public static bool OnlyBlockedByBasicsPolicy(Player player) =>
        RankMath.OnlyBlockedByBasicsPolicy(GetDeckCards(player).Select(ToView), AllowBasics);

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

        ApplyRankEnchantment(survivor, resultRank);
        ApplyUpgradeLevel(survivor, resultUpgrade);

        if (GetRank(survivor) != resultRank)
            throw new InvalidOperationException(
                $"Rank apply failed (wanted {resultRank}); sacrifice kept. {Describe(survivor)}");

        await CardPileCmd.RemoveFromDeck(sacrifice, showPreview: false);
        TrackedRanks.GetOrCreateValue(sacrifice).Rank = CardRankLevel.None;

        MainFile.Logger.Info(
            $"Combined OK → Tier {RankMath.TierRoman(resultRank)} " +
            $"up {sacUp}+{survUp}→{resultUpgrade} | {Describe(survivor)}");
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
        await CardPileCmd.RemoveFromDeck(sacrifice, showPreview: false);

        if (msg.bonusRolled != 0)
            TierBonusService.Apply(survivor, (TierBonus)msg.bonusRolled);
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

        ForceClearEnchantment(card);

        // Amount MUST be 1. The game paints one enchantment ribbon per Amount unit
        // (even when ShowAmount is false) — using 2/3 as "tags" caused double/triple ribbons.
        EnchantmentModel? applied = rank switch
        {
            CardRankLevel.Tier3 => CardCmd.Enchant<ThirdRank>(card, 1m),
            CardRankLevel.Tier2 => CardCmd.Enchant<SecondRank>(card, 1m),
            _ => CardCmd.Enchant<FirstRank>(card, 1m),
        };

        if (applied != null && applied.Amount != 1)
            applied.Amount = 1;

        Track(card, rank);
        MainFile.Logger.Info($"ApplyRankEnchantment → {Describe(card)}");
    }

    private static void ForceClearEnchantment(CardModel card)
    {
        // Only clear *our* rank enchantments — never wipe a vanilla/game enchantment.
        if (card.Enchantment == null)
            return;

        if (RankFromEnchantment(card.Enchantment) == CardRankLevel.None
            && card.Enchantment is not RankEnchantment)
        {
            MainFile.Logger.Warn(
                $"Card already has non-rank enchantment {card.Enchantment.Id}; not clearing for rank.");
            return;
        }

        try
        {
            CardCmd.ClearEnchantment(card);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"ClearEnchantment threw: {e.Message}");
        }

        if (card.Enchantment != null && card.Enchantment is RankEnchantment)
            card.Enchantment = null;

        TrackedRanks.GetOrCreateValue(card).Rank = CardRankLevel.None;
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
