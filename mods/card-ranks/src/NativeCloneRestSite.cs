using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Runs;

namespace CardRanks;

/// <summary>
/// Wire tier-bonus Clone into the <b>vanilla</b> rest-site Clone button:
/// <see cref="CloneRestSiteOption"/> (OptionId <c>CLONE</c>, native
/// <c>ui/rest_site/option_clone.png</c> art, <b>spends</b> the campfire action).
///
/// Vanilla only injects that option via the Paels' Growth relic. We inject the same
/// type when the deck has any Clone-enchanted card so rolling Clone as a tier bonus
/// turns on the real button instead of a custom free tile.
///
/// MultiEnchantment (UncappedSpire) detection is left to UncappedSpire's own
/// CloneRestSiteOption patch when present; without it, top-level <see cref="Clone"/>
/// still matches vanilla's <c>enchantment is Clone</c> filter.
/// </summary>
public static class NativeCloneRestSite
{
    public const string OptionId = "CLONE";

    /// <summary>True if this card should be picked up by the vanilla Clone rest action.</summary>
    public static bool CardHasCloneEnchantment(CardModel card)
    {
        if (card.Enchantment is Clone)
            return true;

        foreach (EnchantmentModel leaf in MultiEnchantCompat.EnumerateLeafEnchantments(card))
        {
            if (leaf is Clone)
                return true;
        }

        return TierBonusService.Has(card, TierBonus.Clone);
    }

    public static bool DeckHasCloneableCard(Player player) =>
        CombineService.GetDeckCards(player).Any(CardHasCloneEnchantment);

    /// <summary>
    /// Add the vanilla Clone rest option when needed. Skips if Paels' Growth (or we)
    /// already added OptionId CLONE.
    /// </summary>
    public static void EnsureVanillaOption(Player player, List<RestSiteOption> options)
    {
        if (options.Any(o => string.Equals(o.OptionId, OptionId, StringComparison.OrdinalIgnoreCase)))
            return;
        if (!DeckHasCloneableCard(player))
            return;

        options.Add(new CloneRestSiteOption(player));
        MainFile.Logger.Info(
            "Injected vanilla Clone rest-site option (CLONE / option_clone.png, spends rest).");
    }
}

/// <summary>
/// After any CloneCard, copy our tier-bonus flags (ConditionalWeakTable does not travel).
/// </summary>
[HarmonyPatch(typeof(RunState), nameof(RunState.CloneCard))]
public static class CloneCardFlagsPatch
{
    public static void Postfix(CardModel mutableCard, CardModel __result)
    {
        if (__result == null || mutableCard == null)
            return;
        try
        {
            TierBonusService.CopyFlagsOnly(mutableCard, __result);
            CombineService.Track(__result, CombineService.GetRank(mutableCard));
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"CloneCard flag copy failed: {e.Message}");
        }
    }
}
