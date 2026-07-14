using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace CardRanks;

/// <summary>
/// After the first card is chosen for combine, only legal partners stay clickable.
/// Prevents mixed-rank attempts that used to reach apply and delete a card.
/// </summary>
public static class CombineSelectUi
{
    private static readonly Color Dim = new(0.45f, 0.45f, 0.45f, 0.55f);
    private static readonly Color Full = Colors.White;

    /// <summary>
    /// Whether <paramref name="card"/> may be toggled given the current selection.
    /// Deselect always allowed; first pick must be a candidate; later picks must CanPair with anchor.
    /// </summary>
    public static bool MayToggle(NDeckCardSelectScreen screen, CardModel card)
    {
        // Already selected → allow deselect (vanilla Add fails then Remove).
        if (screen._selectedCards.Contains(card))
            return true;

        if (screen._selectedCards.Count == 0)
            return CombineService.IsCandidate(card);

        // At max already — only deselect path should run; don't add a third.
        if (screen._selectedCards.Count >= screen._prefs.MaxSelect)
            return false;

        // Second (or further) pick: must pair with every already-selected card
        // (with MaxSelect=2 this is just the single anchor).
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

        CardModel? anchor = screen._selectedCards.Count == 1
            ? screen._selectedCards.First()
            : null;

        foreach (NGridCardHolder holder in grid.CurrentlyDisplayedCardHolders)
        {
            CardModel? model = holder.CardModel;
            if (model == null)
                continue;

            bool allowed;
            if (screen._selectedCards.Count == 0)
            {
                // Nothing selected: only legal combine starters.
                allowed = CombineService.IsCandidate(model);
            }
            else if (screen._selectedCards.Contains(model))
            {
                // Keep selected cards interactive so they can be deselected.
                allowed = true;
            }
            else if (anchor != null)
            {
                allowed = CombineService.CanPair(anchor, model);
            }
            else
            {
                // Two already selected (preview path): lock further adds.
                allowed = false;
            }

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
    }
}
