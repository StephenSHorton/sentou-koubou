using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;

namespace TradingPost;

/// <summary>
/// Sell prices paid by the merchant. Mirrors base shop buy costs (no RNG variance)
/// at half value so sales stay deterministic across multiplayer peers.
/// </summary>
public static class SellPricing
{
    /// <summary>
    /// Base merchant potion cost by rarity — same ladder as
    /// <c>MerchantPotionEntry.GetCost</c> (Uncommon 75, Rare 100, else 50).
    /// </summary>
    public static int PotionBaseBuyCost(PotionRarity rarity) => rarity switch
    {
        PotionRarity.Uncommon => 75,
        PotionRarity.Rare => 100,
        _ => 50,
    };

    public static int PotionSellPrice(PotionModel potion) =>
        Math.Max(1, PotionBaseBuyCost(potion.Rarity) / 2);

    public static int RelicSellPrice(RelicModel relic) =>
        Math.Max(1, relic.MerchantCost / 2);

    public static bool CanSellRelic(RelicModel? relic) =>
        relic != null
        && !relic.HasBeenRemovedFromState
        && relic.IsTradable
        && relic.MerchantCost > 0;
}
