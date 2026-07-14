using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace CardRanks;

/// <summary>
/// Post-combine feedback: auto bonus, then a clean two-beat showcase —
/// (1) sacrifice alone, (2) survivor alone with the new ribbon.
/// Never stack previews or insert long empty waits between beats.
/// </summary>
public static class RankUi
{
    public static async Task<TierBonus> AutoGrantBonusAndShowcaseAsync(
        CardModel sacrifice,
        CardModel survivor,
        CardRankLevel newTier,
        Func<Task>? removeSacrificeAsync)
    {
        TierBonus granted = TierBonus.None;

        if (CardRanksConfig.OfferTierBonusRolls
            && newTier is CardRankLevel.Tier1 or CardRankLevel.Tier2 or CardRankLevel.Tier3)
        {
            try
            {
                TierBonus? picked = TierBonusService.RollNew(survivor);
                if (picked != null)
                {
                    TierBonusService.Apply(survivor, picked.Value);
                    granted = picked.Value;
                    MainFile.Logger.Info(
                        $"Auto tier bonus GRANTED: {TierBonusService.DisplayName(granted)} " +
                        $"on {survivor.Id} (Tier {RankMath.TierRoman(newTier)}) " +
                        $"| {CombineService.Describe(survivor)}");
                }
                else
                {
                    MainFile.Logger.Info(
                        $"Auto tier bonus: pool exhausted for {survivor.Id}");
                }
            }
            catch (Exception e)
            {
                MainFile.Logger.Error($"Auto tier bonus failed: {e}");
            }
        }

        await PlayCombineRevealAsync(sacrifice, survivor, removeSacrificeAsync);
        return granted;
    }

    /// <summary>
    /// Beat 1 — sacrifice alone (short flash, then silent deck remove).
    /// Beat 2 — survivor alone gaining the ribbon (enchant VFX + short preview).
    /// Only one card is on the preview layer at a time.
    /// </summary>
    public static async Task PlayCombineRevealAsync(
        CardModel sacrifice,
        CardModel survivor,
        Func<Task>? removeSacrificeAsync)
    {
        try
        {
            // --- Beat 1: sacrifice alone ---
            // Brief single-card flash (feels like it's being consumed), then remove
            // without a second deck-remove preview that would stack later.
            await AwaitPreviewAsync(sacrifice, duration: 0.85f, timeoutMs: 1000);

            if (removeSacrificeAsync != null)
                await removeSacrificeAsync();
            else
                await CardPileCmd.RemoveFromDeck(sacrifice, showPreview: false);

            // Tiny handoff so the first preview can leave the container.
            await Task.Delay(120);

            // --- Beat 2: survivor alone gains the ribbon ---
            // Rank is already on the card; VFX = ribbon sparkle, Preview = one card center.
            SpawnEnchantVfx(survivor);
            await AwaitPreviewAsync(survivor, duration: 1.35f, timeoutMs: 1600);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Combine reveal failed: {e.Message}");
            try
            {
                if (removeSacrificeAsync != null)
                    await removeSacrificeAsync();
            }
            catch
            {
                // ignore
            }
        }
    }

    private static async Task AwaitPreviewAsync(CardModel card, float duration, int timeoutMs)
    {
        try
        {
            TaskCompletionSource? tcs = CardCmd.Preview(
                card, duration, CardPreviewStyle.HorizontalLayout);
            if (tcs != null)
                await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
            else
                await Task.Delay(Math.Min(timeoutMs, (int)(duration * 1000) + 100));
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Card preview failed: {e.Message}");
            await Task.Delay(200);
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
