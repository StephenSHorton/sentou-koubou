using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
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

    /// <summary>
    /// Whether the merchant will buy this owned relic.
    /// Mirrors vanilla <see cref="RelicModel.IsTradable"/> but deliberately allows
    /// <c>HasUponPickupEffect</c> relics (Strawberry, Potion Belt, etc.) — those are
    /// untradable in vanilla because pickup bonuses never reverse; we reverse them on sell
    /// via <see cref="RelicSellEffects"/>.
    /// </summary>
    public static bool CanSellRelic(RelicModel? relic)
    {
        if (relic == null || relic.HasBeenRemovedFromState)
        {
            return false;
        }
        if (relic.IsMelted || relic.IsUsedUp)
        {
            return false;
        }
        if (relic.SpawnsPets || relic.AddsPet)
        {
            return false;
        }

        // Vanilla IsTradable also blocks Starter / Event / Ancient.
        RelicRarity rarity = relic.Rarity;
        if (rarity is RelicRarity.Starter or RelicRarity.Event or RelicRarity.Ancient)
        {
            return false;
        }

        // Placeholder / unbuyable costs (starter-style 999999999) stay unsellable.
        int cost = relic.MerchantCost;
        if (cost <= 0 || cost >= 999_999_999)
        {
            return false;
        }

        return true;
    }
}
