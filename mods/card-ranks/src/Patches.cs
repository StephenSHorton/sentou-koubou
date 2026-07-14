using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Runs;

namespace CardRanks;

[HarmonyPatch(typeof(RunManager), "InitializeShared")]
public static class RunManagerInitializePatch
{
    public static void Postfix(RunManager __instance)
    {
        CombineSynchronizer.Instance?.Dispose();
        CombineSynchronizer.Instance = new CombineSynchronizer(
            __instance.RunLocationTargetedBuffer,
            __instance.NetService,
            __instance.State!,
            __instance.NetService.NetId);
        Loc.EnsureSettingsEntries();
        Loc.EnsureRestSiteEntries();
        Loc.EnsureCardSelectionEntries();
        Loc.EnsureEnchantmentEntries();
        MainFile.Logger.Info("CombineSynchronizer attached to run.");
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
public static class RunManagerCleanUpPatch
{
    public static void Prefix()
    {
        CombineSynchronizer.Instance?.Dispose();
        CombineSynchronizer.Instance = null;
    }
}

[HarmonyPatch(typeof(RestSiteOption), nameof(RestSiteOption.Generate))]
public static class RestSiteOptionsPatch
{
    public static void Postfix(Player player, List<RestSiteOption> __result)
    {
        if (__result.Any(o => o.OptionId == CombineRestSiteOption.Id))
            return;
        Loc.EnsureRestSiteEntries();
        __result.Add(new CombineRestSiteOption(player));
    }
}

[HarmonyPatch(typeof(RestSiteOption), "Icon", MethodType.Getter)]
public static class RestSiteOptionIconPatch
{
    public static bool Prefix(RestSiteOption __instance, ref Texture2D __result)
    {
        if (__instance is not CombineRestSiteOption)
            return true;
        Texture2D? tex = RankAssets.CombineIcon;
        if (tex == null)
            return true;
        __result = tex;
        return false;
    }
}

/// <summary>
/// While choosing cards for combine, only complete selection when the pair is legal
/// (same id, same rank, candidates under current config).
/// </summary>
[HarmonyPatch(typeof(NDeckCardSelectScreen), "CheckIfSelectionComplete")]
public static class CardSelectPatch
{
    public static bool Prefix(NDeckCardSelectScreen __instance)
    {
        CardSelectorPrefs prefs = __instance._prefs;
        if (prefs.Prompt.LocEntryKey is not "TO_COMBINE")
            return true;
        if (__instance._selectedCards.Count < 2)
            return true;

        CardModel[] picked = __instance._selectedCards.Take(2).ToArray();
        return CombineService.CanPair(picked[0], picked[1]);
    }
}
