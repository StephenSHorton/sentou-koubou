using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
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

/// <summary>In co-op, add the gold-gifting Trade button to the shop screen.</summary>
[HarmonyPatch(typeof(NMerchantRoom), "_Ready")]
public static class MerchantRoomReadyPatch
{
    public static void Postfix(NMerchantRoom __instance)
    {
        TradeSynchronizer? sync = TradeSynchronizer.Instance;
        if (sync != null && sync.OtherPlayers.Count > 0)
        {
            TradeUi.AddTradeButton(__instance);
        }
    }
}

/// <summary>In co-op runs, add the Trade option to every campfire.</summary>
[HarmonyPatch(typeof(RestSiteOption), nameof(RestSiteOption.Generate))]
public static class RestSiteOptionsPatch
{
    public static void Postfix(Player player, List<RestSiteOption> __result)
    {
        if (player.RunState.Players.Count > 1)
        {
            Loc.EnsureRestSiteEntries();
            __result.Add(new TradeRestSiteOption(player));
        }
    }
}

/// <summary>
/// Rest-site option icons are transparent cutouts. Ours ships as option_trade.png
/// beside the DLL (built with alpha — black frame punched out).
/// </summary>
[HarmonyPatch(typeof(RestSiteOption), "Icon", MethodType.Getter)]
public static class RestSiteOptionIconPatch
{
    public static bool Prefix(RestSiteOption __instance, ref Texture2D __result)
    {
        if (__instance is not TradeRestSiteOption)
        {
            return true;
        }
        __result = TradeAssets.OptionTrade
                   ?? TradeAssets.IconTrade
                   ?? GD.Load<Texture2D>("res://images/packed/sprite_fonts/gold_icon.png");
        return false;
    }
}
