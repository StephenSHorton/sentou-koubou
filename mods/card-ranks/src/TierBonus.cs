using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace CardRanks;

/// <summary>
/// Bonuses granted each tier-up. Applied as real vanilla enchantments when possible
/// (UncappedSpire MultiEnchantment stacks them with rank). Keywords/flags remain as fallback.
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

    /// <summary>Copy all tracked bonuses from source onto dest (flags + side effects).</summary>
    public static void MergeFrom(CardModel source, CardModel dest)
    {
        if (ReferenceEquals(source, dest))
            return;
        foreach (TierBonus b in GetAll(source))
            Apply(dest, b);
    }

    public static void Apply(CardModel card, TierBonus bonus)
    {
        if (bonus == TierBonus.None)
            return;

        BonusBox box = Table.GetOrCreateValue(card);
        if (!box.Bonuses.Add(bonus))
            return; // already had it

        bool realEnchantOk = false;
        try
        {
            // Prefer a real vanilla enchantment so the player sees a ribbon/tab.
            // With UncappedSpire, CardCmd.Enchant stacks into MultiEnchantment.
            // Without it, Enchant fails if rank already occupies the slot — fall back.
            realEnchantOk = TryApplyVanillaEnchantment(card, bonus);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Vanilla enchant for {bonus} failed: {e.Message}");
        }

        try
        {
            // Always apply keyword / replay side-effects so combat works even without
            // the vanilla enchantment instance (or if Multi only shows the icon).
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
                    // Flags for Clone rest option / RankEnchantment hooks.
                    break;
            }

            MainFile.Logger.Info(
                $"Tier bonus APPLIED: {DisplayName(bonus)} on {card.Id} " +
                $"(realEnchant={realEnchantOk}, replay={card.BaseReplayCount}, " +
                $"bonuses=[{string.Join(",", GetAll(card))}])");
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Tier bonus apply {bonus} failed: {e}");
        }
    }

    private static bool TryApplyVanillaEnchantment(CardModel card, TierBonus bonus)
    {
        // Only stack extra enchants when MultiEnchantment is available (or slot empty).
        // Otherwise rank occupies the only slot and Enchant would throw / replace rank.
        bool canStack = card.Enchantment == null
                        || MultiEnchantCompat.IsMultiEnchantment(card.Enchantment);
        if (!canStack)
            return false;

        try
        {
            EnchantmentModel? applied = bonus switch
            {
                TierBonus.Clone => CardCmd.Enchant<Clone>(card, 1m),
                TierBonus.SoulsPower => CardCmd.Enchant<SoulsPower>(card, 1m),
                TierBonus.Steady => CardCmd.Enchant<Steady>(card, 1m),
                TierBonus.Spiral => CardCmd.Enchant<Spiral>(card, 1m),
                TierBonus.Imbued => CardCmd.Enchant<Imbued>(card, 1m),
                TierBonus.PerfectFit => CardCmd.Enchant<PerfectFit>(card, 1m),
                TierBonus.RoyallyApproved => CardCmd.Enchant<RoyallyApproved>(card, 1m),
                _ => null,
            };
            return applied != null;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn(
                $"Could not stack vanilla {bonus} (rank preserved via flags): {e.Message}");
            return false;
        }
    }

    public static bool HasClone(CardModel card) => Has(card, TierBonus.Clone);

    public static bool HasImbued(CardModel card) => Has(card, TierBonus.Imbued);
}
