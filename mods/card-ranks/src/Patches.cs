using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;

namespace CardRanks;

[HarmonyPatch(typeof(NMainMenu), "_Ready")]
public static class MainMenuReadyPatch
{
    public static void Postfix()
    {
        Loc.EnsureSettingsEntries();
    }
}

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
        Loc.EnsureRestSiteEntries();
        if (__result.All(o => o.OptionId != CombineRestSiteOption.Id))
            __result.Add(new CombineRestSiteOption(player));

        if (__result.All(o => o.OptionId != CloneRestSiteOption.Id)
            && CloneRestSiteOption.DeckHasCloneCard(player))
            __result.Add(new CloneRestSiteOption(player));
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

internal static class CombineSelectGate
{
    public static bool IsCombinePrompt(CardSelectorPrefs prefs)
    {
        string key = prefs.Prompt.LocEntryKey ?? "";
        string table = prefs.Prompt.LocTable ?? "";
        return key.Equals("TO_COMBINE", StringComparison.OrdinalIgnoreCase)
               || key.EndsWith("TO_COMBINE", StringComparison.OrdinalIgnoreCase)
               || key.Contains("TO_COMBINE", StringComparison.OrdinalIgnoreCase)
               || (table.Contains("card_selection", StringComparison.OrdinalIgnoreCase)
                   && key.Contains("COMBINE", StringComparison.OrdinalIgnoreCase));
    }

    public static bool SelectedGroupIsLegal(NDeckCardSelectScreen screen)
    {
        if (screen._selectedCards.Count < RankMath.CardsPerCombine)
            return true; // not enough yet — vanilla min-count handles it
        return CombineService.CanGroup(screen._selectedCards.Take(RankMath.CardsPerCombine).ToList());
    }
}

[HarmonyPatch(typeof(NDeckCardSelectScreen), "OnCardClicked")]
public static class CardSelectClickPatch
{
    public static bool Prefix(NDeckCardSelectScreen __instance, CardModel card)
    {
        if (!CombineSelectGate.IsCombinePrompt(__instance._prefs))
            return true;

        if (CombineSelectUi.MayToggle(__instance, card))
            return true;

        MainFile.Logger.Info(
            $"Combine click blocked: {CombineService.Describe(card)} " +
            $"(selected={__instance._selectedCards.Count})");
        return false;
    }

    public static void Postfix(NDeckCardSelectScreen __instance)
    {
        if (!CombineSelectGate.IsCombinePrompt(__instance._prefs))
            return;
        CombineSelectUi.RefreshClickableState(__instance);
    }
}

[HarmonyPatch(typeof(NDeckCardSelectScreen), "AfterOverlayShown")]
public static class CardSelectShownPatch
{
    public static void Postfix(NDeckCardSelectScreen __instance)
    {
        if (!CombineSelectGate.IsCombinePrompt(__instance._prefs))
            return;
        CombineSelectUi.RefreshClickableState(__instance);
    }
}

[HarmonyPatch(typeof(NDeckCardSelectScreen), "CheckIfSelectionComplete")]
public static class CardSelectCompletePatch
{
    public static bool Prefix(NDeckCardSelectScreen __instance)
    {
        if (!CombineSelectGate.IsCombinePrompt(__instance._prefs))
            return true;
        if (__instance._selectedCards.Count < RankMath.CardsPerCombine)
            return true;

        if (!CombineSelectGate.SelectedGroupIsLegal(__instance))
        {
            MainFile.Logger.Info("Combine confirm blocked: illegal triple.");
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(NDeckCardSelectScreen), "PreviewSelection", new Type[] { })]
public static class CardSelectPreviewPatch
{
    public static bool Prefix(NDeckCardSelectScreen __instance)
    {
        if (!CombineSelectGate.IsCombinePrompt(__instance._prefs))
            return true;
        if (__instance._selectedCards.Count < RankMath.CardsPerCombine)
            return true;
        if (CombineSelectGate.SelectedGroupIsLegal(__instance))
            return true;

        MainFile.Logger.Info("Combine preview blocked: illegal triple.");
        try
        {
            if (__instance._selectedCards.Count >= RankMath.CardsPerCombine)
            {
                CardModel? last = __instance._selectedCards.LastOrDefault();
                if (last != null)
                {
                    __instance._selectedCards.Remove(last);
                    __instance._grid?.UnhighlightCard(last);
                }
                CombineSelectUi.RefreshClickableState(__instance);
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Failed to unselect illegal pick: {e.Message}");
        }
        return false;
    }
}
