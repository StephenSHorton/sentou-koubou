using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TradingPost;

/// <summary>
/// Reverses permanent "upon pickup" bonuses before a relic is removed on sell.
/// Vanilla never undoes these (and marks such relics untradable); we do so the
/// merchant can buy fruits / potion belts without leaving free max HP or slots.
/// </summary>
/// <remarks>
/// Reversible (stats applied in <c>AfterObtained</c>):
/// <list type="bullet">
/// <item><description>Max HP gain (Strawberry, Pear, Mango, Lee's Waffle, …)</description></item>
/// <item><description>Max HP loss (HpLoss vars, e.g. Distinguished Cape if ever sellable)</description></item>
/// <item><description>Potion slots (Potion Belt, Phial Holster, …)</description></item>
/// </list>
/// Left alone (one-shot / deck mutations — keeping the benefit is intentional):
/// gold, card upgrades/transforms/removes, curses, potion rewards, map changes.
/// Ongoing hooks (e.g. ModifyMaxEnergy) clear automatically when the relic is removed.
/// </remarks>
public static class RelicSellEffects
{
    private const string PotionSlotsKey = "PotionSlots";

    /// <summary>
    /// Undo permanent pickup bonuses while the relic is still owned.
    /// Safe to call on any relic; no-ops when there is nothing to reverse.
    /// </summary>
    public static async Task RevertPermanentEffects(RelicModel relic)
    {
        if (relic == null || relic.HasBeenRemovedFromState)
        {
            return;
        }

        Player owner;
        try
        {
            owner = relic.Owner;
        }
        catch
        {
            return;
        }

        if (owner?.Creature == null)
        {
            return;
        }

        // Only reverse when the game itself marks this as a one-shot pickup effect.
        // Passive combat relics without that flag don't bake stats into the player this way.
        if (!relic.HasUponPickupEffect)
        {
            return;
        }

        DynamicVarSet vars = relic.DynamicVars;

        try
        {
            // Fruit-style max HP gain.
            if (vars.TryGetValue(MaxHpVar.defaultName, out DynamicVar? maxHp) && maxHp.BaseValue > 0m)
            {
                decimal amount = maxHp.BaseValue;
                MainFile.Logger.Info($"Sell reverse: -{amount} max HP from {relic.Id}");
                await CreatureCmd.LoseMaxHp(
                    new ThrowingPlayerChoiceContext(),
                    owner.Creature,
                    amount,
                    isFromCard: false);
            }

            // Explicit HP sacrifice on pickup (restore it).
            if (vars.TryGetValue(HpLossVar.defaultName, out DynamicVar? hpLoss) && hpLoss.BaseValue > 0m)
            {
                decimal amount = hpLoss.BaseValue;
                MainFile.Logger.Info($"Sell reverse: +{amount} max HP (undo loss) from {relic.Id}");
                await CreatureCmd.GainMaxHp(owner.Creature, amount);
            }

            // Potion belt / holster style slot grants.
            if (vars.TryGetValue(PotionSlotsKey, out DynamicVar? slots) && slots.IntValue > 0)
            {
                int count = slots.IntValue;
                // Don't strip below 1 slot or below currently held potions.
                int heldPotions = owner.Potions.Count();
                int maxRemovable = Math.Max(0, owner.MaxPotionCount - Math.Max(1, heldPotions));
                int remove = Math.Min(count, maxRemovable);
                if (remove > 0)
                {
                    MainFile.Logger.Info($"Sell reverse: -{remove} potion slots from {relic.Id}");
                    await PlayerCmd.LoseMaxPotionCount(remove, owner);
                }
                else
                {
                    MainFile.Logger.Info(
                        $"Sell reverse: potion slots kept for {relic.Id} " +
                        $"(belt wanted -{count}, max removable {maxRemovable})");
                }
            }
        }
        catch (Exception e)
        {
            // Don't abort the sale if a reverse step fails — still remove the relic.
            MainFile.Logger.Warn($"Sell reverse failed for {relic.Id}: {e.Message}");
        }
    }
}
