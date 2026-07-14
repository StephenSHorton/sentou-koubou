using System.Runtime.CompilerServices;
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

    /// <inheritdoc cref="RankMath.Rank2AmountTag"/>
    public const int Rank2AmountTag = RankMath.Rank2AmountTag;
    /// <inheritdoc cref="RankMath.Rank3AmountTag"/>
    public const int Rank3AmountTag = RankMath.Rank3AmountTag;

    /// <summary>Weak map so we still know rank if detection fails mid-session.</summary>
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

    public static void Track(CardModel card, CardRankLevel rank)
    {
        TrackedRanks.GetOrCreateValue(card).Rank = rank;
    }

    public static CardRankLevel RankFromEnchantment(EnchantmentModel? enchantment)
    {
        if (enchantment == null)
            return CardRankLevel.None;

        // 1) Amount tags we stamp on apply (most reliable across type-identity quirks).
        if (enchantment.Amount == Rank3AmountTag)
            return CardRankLevel.Rank3;
        if (enchantment.Amount == Rank2AmountTag)
            return CardRankLevel.Rank2;

        // 2) Explicit types.
        if (enchantment is ThirdRank)
            return CardRankLevel.Rank3;
        if (enchantment is SecondRank)
        {
            // Legacy: stacked SecondRank amount meant "double badge" / should be Rank3.
            if (enchantment.Amount >= 2)
                return CardRankLevel.Rank3;
            return CardRankLevel.Rank2;
        }
        if (enchantment is RankEnchantment ranked)
            return ranked.Rank;

        // 3) Multiplier probe (virtual dispatch on our enchantments).
        try
        {
            decimal mult = enchantment.EnchantBlockMultiplicative(1m);
            if (mult >= 2.9m) // 3x Rank3 (or 4x original)
                return CardRankLevel.Rank3;
            if (mult > 1.2m && mult < 2.5m) // 1.5x Rank2 (or 2x original)
                return CardRankLevel.Rank2;
        }
        catch
        {
            // ignore
        }

        // 4) Icon path (res://card_ranks/rank2.png).
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
            return CardRankLevel.Rank3;
        if (icon.Contains("rank2", StringComparison.OrdinalIgnoreCase))
            return CardRankLevel.Rank2;

        // 5) Type name walk.
        for (Type? t = enchantment.GetType(); t != null && t != typeof(object); t = t.BaseType)
        {
            string n = t.Name;
            if (n.Contains("ThirdRank", StringComparison.OrdinalIgnoreCase))
                return CardRankLevel.Rank3;
            if (n.Contains("SecondRank", StringComparison.OrdinalIgnoreCase))
                return enchantment.Amount >= 2 ? CardRankLevel.Rank3 : CardRankLevel.Rank2;
        }

        // 6) Id / title blob.
        string entry = enchantment.Id.Entry ?? "";
        string category = enchantment.Id.Category ?? "";
        string title = "";
        try
        {
            title = enchantment.Title?.ToString() ?? "";
        }
        catch
        {
            // ignore
        }
        string idBlob = $"{category}.{entry}|{enchantment.Id}|{title}|{enchantment.GetType().FullName}";

        if (RankMath.LooksLikeThirdRank(idBlob) || title.Contains("Rank 3", StringComparison.OrdinalIgnoreCase))
            return CardRankLevel.Rank3;
        if (RankMath.LooksLikeSecondRank(idBlob) || title.Contains("Rank 2", StringComparison.OrdinalIgnoreCase))
            return CardRankLevel.Rank2;

        return CardRankLevel.None;
    }

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
            CardKey(card),
            GetRank(card),
            IsBasicLike(card),
            card.CurrentUpgradeLevel);

    public static string CardKey(CardModel card) =>
        $"{card.Id.Category}/{card.Id.Entry}";

    public static bool SameCardIdentity(CardModel a, CardModel b) =>
        a.Id.Equals(b.Id) || string.Equals(CardKey(a), CardKey(b), StringComparison.Ordinal);

    public static bool IsCandidate(CardModel card) =>
        RankMath.IsCandidate(ToView(card), AllowBasics);

    /// <summary>
    /// Same card identity + same rank tier. Logs mismatches at Info for debugging.
    /// </summary>
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
            MainFile.Logger.Info(
                $"CanPair: not a candidate {ra} | {Describe(a)} || {Describe(b)}");
            return false;
        }

        return true;
    }

    public static string Describe(CardModel card)
    {
        var e = card.Enchantment;
        string icon = "";
        decimal mult = 0;
        try
        {
            icon = e?.IconPath ?? e?.IntendedIconPath ?? "";
            if (e != null)
                mult = e.EnchantBlockMultiplicative(1m);
        }
        catch
        {
            // ignore
        }

        return $"{card.Id} rank={GetRank(card)} up={card.CurrentUpgradeLevel} " +
               $"enchType={e?.GetType().FullName ?? "null"} " +
               $"enchId={e?.Id.ToString() ?? "null"} " +
               $"entry={e?.Id.Entry ?? "null"} amount={e?.Amount.ToString() ?? "-"} " +
               $"mult={mult} icon={icon}";
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
        maxUp = Math.Max(maxUp, sacrifice.CurrentUpgradeLevel + survivor.CurrentUpgradeLevel);

        if (!RankMath.TryPlanCombine(
                ToView(sacrifice), ToView(survivor), AllowBasics, maxUp,
                out CardRankLevel resultRank, out int resultUpgrade))
            throw new InvalidOperationException("TryPlanCombine failed after CanPair succeeded.");

        int sacUp = sacrifice.CurrentUpgradeLevel;
        int survUp = survivor.CurrentUpgradeLevel;

        // Enchant/upgrade FIRST, then remove sacrifice.
        ApplyRankEnchantment(survivor, resultRank);
        ApplyUpgradeLevel(survivor, resultUpgrade);

        CardRankLevel now = GetRank(survivor);
        if (now != resultRank)
            throw new InvalidOperationException(
                $"Rank apply failed (wanted {resultRank}, got {now}); sacrifice not removed. {Describe(survivor)}");

        await CardPileCmd.RemoveFromDeck(sacrifice, showPreview: false);
        TrackedRanks.GetOrCreateValue(sacrifice).Rank = CardRankLevel.None;

        MainFile.Logger.Info(
            $"Combined OK: {sacrifice.Id} {sacRank}+{survRank} → rank {resultRank} " +
            $"upgrade {sacUp}+{survUp}→{resultUpgrade} | now {Describe(survivor)}");
    }

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
    /// Replace any existing enchant with exactly one Rank 2/3 instance.
    /// Amount is stamped with <see cref="Rank2AmountTag"/> / <see cref="Rank3AmountTag"/> for reliable reads.
    /// </summary>
    private static void ApplyRankEnchantment(CardModel card, CardRankLevel rank)
    {
        if (rank is not (CardRankLevel.Rank2 or CardRankLevel.Rank3))
            return;

        ForceClearEnchantment(card);

        int amountTag = rank == CardRankLevel.Rank3 ? Rank3AmountTag : Rank2AmountTag;

        EnchantmentModel? applied = rank switch
        {
            CardRankLevel.Rank3 => CardCmd.Enchant<ThirdRank>(card, amountTag),
            _ => CardCmd.Enchant<SecondRank>(card, amountTag),
        };

        if (applied != null && applied.Amount != amountTag)
            applied.Amount = amountTag;

        Track(card, rank);

        CardRankLevel now = GetRank(card);
        if (now != rank)
        {
            MainFile.Logger.Error(
                $"ApplyRankEnchantment expected {rank} but got {now} on {Describe(card)}");
            // Force tracker so picker filtering still works this run.
            Track(card, rank);
        }
        else
        {
            MainFile.Logger.Info($"ApplyRankEnchantment OK: {Describe(card)}");
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
                MainFile.Logger.Warn($"Upgrade step failed at {before}→{targetLevel}: {e.Message}");
                break;
            }
            if (survivor.CurrentUpgradeLevel <= before)
            {
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
