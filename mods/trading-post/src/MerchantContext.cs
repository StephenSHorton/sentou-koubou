using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace TradingPost;

/// <summary>Whether the local player is currently in a merchant / shop room.</summary>
public static class MerchantContext
{
    public static bool IsInShop()
    {
        try
        {
            NMerchantRoom? node = NMerchantRoom.Instance;
            if (node != null && GodotObject.IsInstanceValid(node))
            {
                return true;
            }
        }
        catch
        {
            // Instance accessor can throw outside a run
        }

        try
        {
            return RunManager.Instance?.State?.CurrentRoom is MerchantRoom;
        }
        catch
        {
            return false;
        }
    }
}
