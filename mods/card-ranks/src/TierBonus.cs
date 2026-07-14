using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace CardRanks;

/// <summary>
/// Optional rewards offered when a card reaches a new tier. Applied without clearing rank:
/// keywords / Replay on the card, plus flags for hooks that live on <see cref="RankEnchantment"/>.
/// </summary>
public enum TierBonus
{
    None = 0,
    Clone = 1,
    SoulsPower = 2,
    Steady = 3,
    Spiral = 4,
    Imbued = 5,
    PerfectFit = 6,
    RoyallyApproved = 7,
}

public static class TierBonusService
{
    private static readonly TierBonus[] Pool =
    [
        TierBonus.Clone,
        TierBonus.SoulsPower,
        TierBonus.Steady,
        TierBonus.Spiral,
        TierBonus.Imbued,
        TierBonus.PerfectFit,
        TierBonus.RoyallyApproved,
    ];

    private static readonly ConditionalWeakTable<CardModel, BonusBox> Table = new();

    private sealed class BonusBox
    {
        public readonly HashSet<TierBonus> Bonuses = new();
    }

    public static IReadOnlyList<TierBonus> AllPool => Pool;

    public static bool Has(CardModel card, TierBonus bonus) =>
        Table.TryGetValue(card, out BonusBox? box) && box.Bonuses.Contains(bonus);

    public static int ReplayBonus(CardModel? card)
    {
        if (card == null || !Table.TryGetValue(card, out BonusBox? box))
            return 0;
        int n = 0;
        foreach (TierBonus b in box.Bonuses)
        {
            if (b == TierBonus.Spiral)
                n += 1;
        }
        return n;
    }

    public static IReadOnlyCollection<TierBonus> GetAll(CardModel card)
    {
        if (!Table.TryGetValue(card, out BonusBox? box))
            return Array.Empty<TierBonus>();
        return box.Bonuses.ToArray();
    }

    public static string DisplayName(TierBonus bonus) => bonus switch
    {
        TierBonus.Clone => "Clone",
        TierBonus.SoulsPower => "Soul's Power",
        TierBonus.Steady => "Steady",
        TierBonus.Spiral => "Spiral",
        TierBonus.Imbued => "Imbued",
        TierBonus.PerfectFit => "Perfect Fit",
        TierBonus.RoyallyApproved => "Royally Approved",
        _ => "None",
    };

    public static string Description(TierBonus bonus) => bonus switch
    {
        TierBonus.Clone => "Can be duplicated at rest sites.",
        TierBonus.SoulsPower => "Loses Exhaust (if it had it).",
        TierBonus.Steady => "Gains Retain.",
        TierBonus.Spiral => "Gains Replay +1.",
        TierBonus.Imbued => "Plays automatically at the start of combat.",
        TierBonus.PerfectFit => "After shuffle into draw, goes on top.",
        TierBonus.RoyallyApproved => "Gains Innate and Retain.",
        _ => "",
    };

    /// <summary>Pick a random bonus the card does not already have; null if pool exhausted.</summary>
    public static TierBonus? RollNew(CardModel card, Random? rng = null)
    {
        rng ??= Random.Shared;
        HashSet<TierBonus> have = Table.TryGetValue(card, out BonusBox? box)
            ? box.Bonuses
            : new HashSet<TierBonus>();
        List<TierBonus> available = Pool.Where(b => !have.Contains(b)).ToList();
        if (available.Count == 0)
            return null;
        return available[rng.Next(available.Count)];
    }

    public static void Apply(CardModel card, TierBonus bonus)
    {
        if (bonus == TierBonus.None)
            return;

        BonusBox box = Table.GetOrCreateValue(card);
        if (!box.Bonuses.Add(bonus))
            return; // already had it

        try
        {
            switch (bonus)
            {
                case TierBonus.Steady:
                    CardCmd.ApplyKeyword(card, CardKeyword.Retain);
                    break;
                case TierBonus.RoyallyApproved:
                    CardCmd.ApplyKeyword(card, CardKeyword.Innate, CardKeyword.Retain);
                    break;
                case TierBonus.SoulsPower:
                    CardCmd.RemoveKeyword(card, CardKeyword.Exhaust);
                    break;
                case TierBonus.Spiral:
                    card.BaseReplayCount = Math.Max(0, card.BaseReplayCount) + 1;
                    break;
                case TierBonus.Clone:
                case TierBonus.Imbued:
                case TierBonus.PerfectFit:
                    // Handled by RankEnchantment hooks / rest-site Clone option.
                    break;
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Tier bonus apply {bonus} failed: {e}");
        }

        MainFile.Logger.Info(
            $"Tier bonus {DisplayName(bonus)} on {card.Id} (rank stays).");
    }

    public static bool HasClone(CardModel card) => Has(card, TierBonus.Clone);

    public static bool HasImbued(CardModel card) => Has(card, TierBonus.Imbued);
}
