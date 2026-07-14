using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace CardRanks;

/// <summary>
/// After a free Combine, rest-site chrome can stay mid-tween / mid-selection.
/// RankUpCards2 reloads the option button; we also pull description back up so the
/// next deck-select overlay lays out on a clean rest-site frame.
/// </summary>
public static class RestSiteUi
{
    public static void RefreshAfterCombine(RestSiteOption option)
    {
        try
        {
            NRestSiteRoom? room = NRestSiteRoom.Instance;
            if (room == null)
                return;

            // Undo description drop that happens around option hover / select.
            room.AnimateDescriptionUp();

            var button = room.GetButtonForOption(option);
            if (button == null)
                return;

            button.Reload();
            // Keep free-action tile clickable when more pairs remain.
            button._isUnclickable = !option.IsEnabled;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Rest-site UI refresh failed: {e.Message}");
        }
    }
}
