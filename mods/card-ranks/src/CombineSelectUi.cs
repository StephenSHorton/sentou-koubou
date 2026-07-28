using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace CardRanks;

/// <summary>
/// After the first card is chosen, only matching same-tier partners stay clickable.
/// Need <see cref="RankMath.CardsPerCombine"/> (3) for a legal combine.
/// </summary>
public static class CombineSelectUi
{
    private static readonly Color Dim = new(0.45f, 0.45f, 0.45f, 0.55f);
    private static readonly Color Full = Colors.White;

    public static bool MayToggle(NDeckCardSelectScreen screen, CardModel card)
    {
        if (screen._selectedCards.Contains(card))
            return true;

        if (screen._selectedCards.Count == 0)
        {
            // Only cards that still have a full 3-set among what's on the grid.
            List<CardModel> pool = CollectDisplayedCards(screen);
            return CombineService.CanStartCombineWith(card, pool);
        }

        if (screen._selectedCards.Count >= screen._prefs.MaxSelect)
            return false;

        // Must pair with every already-selected card (same id + same tier).
        foreach (CardModel selected in screen._selectedCards)
        {
            if (!CombineService.CanPair(selected, card))
                return false;
        }
        return true;
    }

    public static void RefreshClickableState(NDeckCardSelectScreen screen)
    {
        NCardGrid? grid = screen._grid;
        if (grid == null)
            return;

        List<CardModel> pool = CollectDisplayedCards(screen);

        CardModel? anchor = screen._selectedCards.Count >= 1
            ? screen._selectedCards.First()
            : null;

        if (anchor != null)
        {
            MainFile.Logger.Info(
                $"Combine filter anchor: {CombineService.Describe(anchor)} " +
                $"(selected={screen._selectedCards.Count}/{RankMath.CardsPerCombine})");
        }

        int allowedCount = 0;
        int blockedCount = 0;

        foreach (NGridCardHolder holder in grid.CurrentlyDisplayedCardHolders)
        {
            CardModel? model = holder.CardModel;
            if (model == null)
                continue;

            bool allowed;
            if (screen._selectedCards.Count == 0)
            {
                allowed = CombineService.CanStartCombineWith(model, pool);
            }
            else if (screen._selectedCards.Contains(model))
            {
                allowed = true;
            }
            else if (screen._selectedCards.Count >= screen._prefs.MaxSelect)
            {
                allowed = false;
            }
            else if (anchor != null)
            {
                allowed = CombineService.CanPair(anchor, model);
            }
            else
            {
                allowed = false;
            }

            if (allowed)
                allowedCount++;
            else
                blockedCount++;

            try
            {
                holder.SetClickable(allowed);
                holder.Modulate = allowed ? Full : Dim;
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"SetClickable failed: {e.Message}");
            }
        }

        if (anchor != null || blockedCount > 0)
        {
            MainFile.Logger.Info(
                $"Combine filter: allowed={allowedCount} blocked={blockedCount} " +
                $"(anchorRank={(anchor != null ? CombineService.GetRank(anchor).ToString() : "-")})");
        }
    }

    private static List<CardModel> CollectDisplayedCards(NDeckCardSelectScreen screen)
    {
        var list = new List<CardModel>();
        NCardGrid? grid = screen._grid;
        if (grid == null)
            return list;

        foreach (NGridCardHolder holder in grid.CurrentlyDisplayedCardHolders)
        {
            CardModel? model = holder.CardModel;
            if (model != null)
                list.Add(model);
        }

        return list;
    }
}
