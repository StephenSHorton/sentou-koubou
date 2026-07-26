using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace CardRanks;

/// <summary>
/// Post-combine: auto bonus (visible vanilla enchant when possible) + ribbon VFX.
/// Silent remove of both sacrifices; single NCardEnchantVfx on the survivor.
/// </summary>
public static class RankUi
{
    public static async Task<TierBonus> AutoGrantBonusAndShowcaseAsync(
        CardModel sacrifice1,
        CardModel sacrifice2,
        CardModel survivor,
        CardRankLevel newTier,
        Func<Task>? removeSacrificesAsync)
    {
        TierBonus granted = TierBonus.None;

        // Random bonus on each new tier (plain → I, I → II, II → III).
        // Re-roll if a pick can't land (e.g. Soul's Power on Strike — game rejects the leaf).
        if (CardRanksConfig.OfferTierBonusRolls
            && newTier is CardRankLevel.Tier1 or CardRankLevel.Tier2 or CardRankLevel.Tier3)
        {
            try
            {
                // Uniform among bonuses that can actually land (CanLand).

                var tried = new HashSet<TierBonus>();

                var landable = TierBonusService.GetLandablePool(survivor);

                MainFile.Logger.Info(

                    $"Auto tier bonus pool for {survivor.Id}: " +

                    $"[{string.Join(", ", landable.Select(TierBonusService.DisplayName))}] " +

                    $"(stack={TierBonusService.CanStackExtraEnchant(survivor)})");

                for (int attempt = 0; attempt < 8; attempt++)
                {
                    TierBonus? picked = TierBonusService.RollNew(survivor, exclude: tried);
                    if (picked == null)
                    {
                        MainFile.Logger.Info(
                            $"Auto tier bonus: pool exhausted for {survivor.Id}");
                        break;
                    }

                    tried.Add(picked.Value);
                    if (!TierBonusService.Apply(survivor, picked.Value))
                    {
                        MainFile.Logger.Info(
                            $"Auto tier bonus retry after rejected {TierBonusService.DisplayName(picked.Value)}");
                        continue;
                    }

                    granted = picked.Value;
                    MainFile.Logger.Info(
                        $"Auto tier bonus GRANTED: {TierBonusService.DisplayName(granted)} " +
                        $"on {survivor.Id} (Tier {RankMath.TierRoman(newTier)}) " +
                        $"| {CombineService.Describe(survivor)}");
                    break;
                }
            }
            catch (Exception e)
            {
                MainFile.Logger.Error($"Auto tier bonus failed: {e}");
            }
        }

        await PlayCombineRevealAsync(sacrifice1, sacrifice2, survivor, removeSacrificesAsync);
        return granted;
    }

    public static async Task PlayCombineRevealAsync(
        CardModel sacrifice1,
        CardModel sacrifice2,
        CardModel survivor,
        Func<Task>? removeSacrificesAsync)
    {
        try
        {
            if (removeSacrificesAsync != null)
                await removeSacrificesAsync();
            else
                await CombineService.RemoveSacrificesAsync(sacrifice1, sacrifice2);

            SpawnEnchantVfx(survivor);
            await Task.Delay(750);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Combine reveal failed: {e.Message}");
            try
            {
                if (removeSacrificesAsync != null)
                    await removeSacrificesAsync();
            }
            catch
            {
                // ignore
            }
        }
    }

    private static void SpawnEnchantVfx(CardModel card)
    {
        try
        {
            var vfx = NCardEnchantVfx.Create(card);
            if (vfx == null)
                return;
            NRun? run = NRun.Instance;
            run?.GlobalUi?.CardPreviewContainer?.AddChildSafely(vfx);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Enchant VFX failed: {e.Message}");
        }
    }
}
