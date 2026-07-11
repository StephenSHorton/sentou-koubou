using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace TradingPost;

/// <summary>
/// Creates/disposes the trade synchronizer alongside the game's own run synchronizers.
/// InitializeShared is where RunManager builds OneOffSynchronizer and friends.
/// </summary>
[HarmonyPatch(typeof(RunManager), "InitializeShared")]
public static class RunManagerInitializePatch
{
    public static void Postfix(RunManager __instance)
    {
        TradeSynchronizer.Instance?.Dispose();
        TradeSynchronizer.Instance = new TradeSynchronizer(
            __instance.RunLocationTargetedBuffer,
            __instance.NetService,
            __instance.State!,
            __instance.NetService.NetId);
        MainFile.Logger.Info("TradeSynchronizer attached to run.");
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
public static class RunManagerCleanUpPatch
{
    public static void Prefix()
    {
        TradeSynchronizer.Instance?.Dispose();
        TradeSynchronizer.Instance = null;
    }
}

/// <summary>
/// Each merchant room load: reset the once-per-visit trade and, in co-op, add the Trade button.
/// </summary>
[HarmonyPatch(typeof(NMerchantRoom), "_Ready")]
public static class MerchantRoomReadyPatch
{
    public static void Postfix(NMerchantRoom __instance)
    {
        TradeSynchronizer? sync = TradeSynchronizer.Instance;
        if (sync == null)
        {
            return;
        }
        sync.ResetVisit();
        if (sync.OtherPlayers.Count > 0)
        {
            TradeUi.AddTradeButton(__instance);
        }
    }
}
