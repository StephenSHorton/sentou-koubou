using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace CardRanks;

/// <summary>
/// After a free Combine, rest-site chrome can stay mid-tween / mid-selection.
/// Reload the option button and pull description back up.
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

            room.AnimateDescriptionUp();

            var button = room.GetButtonForOption(option);
            if (button == null)
                return;

            button.Reload();
            button._isUnclickable = !option.IsEnabled;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Rest-site UI refresh failed: {e.Message}");
        }
    }
}
