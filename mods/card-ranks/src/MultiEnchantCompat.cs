using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;

namespace CardRanks;

/// <summary>
/// Compatibility with UncappedSpire's MultiEnchantment wrapper.
/// That mod intercepts CardCmd.Enchant / set_Enchantment so each new enchant is
/// ADDED into a MultiEnchantment (multiple ribbon tabs) instead of replaced.
/// Rank upgrades must strip previous First/Second/Third ranks before applying
/// the next tier, or multipliers stack (1.5×2=3) and dual ribbons appear.
/// </summary>
public static class MultiEnchantCompat
{
    private static Type? _multiType;
    private static MethodInfo? _getEnchantmentsOnCards;
    private static MethodInfo? _addEnchantment;
    private static bool _resolved;

    private static void EnsureResolved()
    {
        if (_resolved)
            return;
        _resolved = true;

        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                Type? t = asm.GetType(
                    "UncappedSpire.UncappedSpireCode.UncappedEnchantments.MultiEnchantment",
                    throwOnError: false);
                if (t == null)
                    continue;
                _multiType = t;
                _getEnchantmentsOnCards = t.GetMethod(
                    "get_EnchantmentsOnCards",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                _addEnchantment = t.GetMethod(
                    "AddEnchantment",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    binder: null,
                    types: [typeof(EnchantmentModel)],
                    modifiers: null);
                MainFile.Logger.Info(
                    $"UncappedSpire MultiEnchantment detected (Add={_addEnchantment != null}, " +
                    $"List={_getEnchantmentsOnCards != null}). Rank apply will unwrap/replace.");
                return;
            }
            catch
            {
                // ignore assemblies that fail to inspect
            }
        }
    }

    public static bool IsMultiEnchantment(EnchantmentModel? e)
    {
        if (e == null)
            return false;
        EnsureResolved();
        return _multiType != null && _multiType.IsInstanceOfType(e);
    }

    /// <summary>All leaf enchantments on the card (unwraps MultiEnchantment).</summary>
    public static IReadOnlyList<EnchantmentModel> EnumerateLeafEnchantments(CardModel card)
    {
        EnchantmentModel? top = card.Enchantment;
        if (top == null)
            return Array.Empty<EnchantmentModel>();

        if (!IsMultiEnchantment(top))
            return [top];

        List<EnchantmentModel> leaves = [];
        try
        {
            object? listObj = _getEnchantmentsOnCards?.Invoke(top, null);
            if (listObj is IEnumerable enumerable)
            {
                foreach (object? item in enumerable)
                {
                    if (item is not CardModel storage)
                        continue;
                    EnchantmentModel? nested = storage.Enchantment;
                    if (nested != null && !IsMultiEnchantment(nested))
                        leaves.Add(nested);
                }
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"MultiEnchant enumerate failed: {e.Message}");
        }

        return leaves;
    }

    /// <summary>True if this enchantment (or its type name) is one of our rank tiers.</summary>
    public static bool IsRankLeaf(EnchantmentModel? e)
    {
        if (e == null)
            return false;
        if (e is RankEnchantment)
            return true;
        string n = e.GetType().Name;
        return n.Contains("FirstRank", StringComparison.OrdinalIgnoreCase)
               || n.Contains("SecondRank", StringComparison.OrdinalIgnoreCase)
               || n.Contains("ThirdRank", StringComparison.OrdinalIgnoreCase)
               || n.Contains("RankEnchantment", StringComparison.OrdinalIgnoreCase);
    }

    public static CardRankLevel RankFromLeaf(EnchantmentModel e)
    {
        switch (e)
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

        string n = e.GetType().Name;
        if (n.Contains("ThirdRank", StringComparison.OrdinalIgnoreCase))
            return CardRankLevel.Tier3;
        if (n.Contains("SecondRank", StringComparison.OrdinalIgnoreCase))
            return CardRankLevel.Tier2;
        if (n.Contains("FirstRank", StringComparison.OrdinalIgnoreCase))
            return CardRankLevel.Tier1;

        // Do NOT use multiplier product on MultiEnchantment wrappers (1.5*2=3 false Tier3).
        if (IsMultiEnchantment(e))
            return CardRankLevel.None;

        try
        {
            decimal mult = e.EnchantBlockMultiplicative(1m);
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
            icon = e.IconPath ?? e.IntendedIconPath ?? "";
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

        return CardRankLevel.None;
    }

    /// <summary>
    /// Highest rank among leaf enchantments. Multi wrappers never contribute via product mult.
    /// </summary>
    public static CardRankLevel DetectRank(CardModel card)
    {
        CardRankLevel best = CardRankLevel.None;
        foreach (EnchantmentModel leaf in EnumerateLeafEnchantments(card))
        {
            CardRankLevel r = RankFromLeaf(leaf);
            if (r > best)
                best = r;
        }

        // Fallback: top-level only if no multi unwrap and no leaves
        if (best == CardRankLevel.None && card.Enchantment != null && !IsMultiEnchantment(card.Enchantment))
            best = RankFromLeaf(card.Enchantment);

        return best;
    }

    /// <summary>
    /// Remove every rank-tier leaf from MultiEnchantment (or clear a lone rank enchantment).
    /// Preserves non-rank enchantments stacked by UncappedSpire.
    /// </summary>
    public static void StripRankLeaves(CardModel card)
    {
        EnchantmentModel? top = card.Enchantment;
        if (top == null)
            return;

        if (IsMultiEnchantment(top))
        {
            try
            {
                object? listObj = _getEnchantmentsOnCards?.Invoke(top, null);
                if (listObj is not IList list)
                {
                    HardClearTop(card);
                    return;
                }

                // Remove from end so indices stay valid.
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i] is not CardModel storage)
                        continue;
                    if (IsRankLeaf(storage.Enchantment))
                        list.RemoveAt(i);
                }

                if (list.Count == 0)
                    HardClearTop(card);

                MainFile.Logger.Info(
                    $"Stripped rank leaves from MultiEnchantment; remaining={list.Count}");
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"StripRankLeaves multi failed: {e.Message}; hard clear.");
                HardClearTop(card);
            }

            return;
        }

        if (IsRankLeaf(top) || RankFromLeaf(top) != CardRankLevel.None)
            HardClearTop(card);
    }

    /// <summary>
    /// Force-clear the card enchantment slot. Uses ClearEnchantment + backing-field null
    /// because UncappedSpire can no-op property set to null.
    /// </summary>
    public static void HardClearTop(CardModel card)
    {
        try
        {
            CardCmd.ClearEnchantment(card);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"ClearEnchantment: {e.Message}");
        }

        if (card.Enchantment == null)
            return;

        try
        {
            FieldInfo? backing = typeof(CardModel).GetField(
                "<Enchantment>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (backing != null)
            {
                backing.SetValue(card, null);
                MainFile.Logger.Info("Hard-cleared Enchantment via backing field.");
            }
            else
            {
                card.Enchantment = null;
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Hard clear backing field failed: {e.Message}");
            try
            {
                card.Enchantment = null;
            }
            catch
            {
                // ignore
            }
        }
    }

    public static bool TryAddIntoMulti(CardModel card, EnchantmentModel enchantment)
    {
        EnsureResolved();
        if (card.Enchantment == null || !IsMultiEnchantment(card.Enchantment))
            return false;
        if (_addEnchantment == null)
            return false;
        try
        {
            _addEnchantment.Invoke(card.Enchantment, [enchantment]);
            return true;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Multi.AddEnchantment failed: {e.Message}");
            return false;
        }
    }
}
