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

/// <summary>Inject settings loc before the player opens BaseLib's mod config menu.</summary>
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

    public static bool TryGetSelectedPair(NDeckCardSelectScreen screen, out CardModel a, out CardModel b)
    {
        a = null!;
        b = null!;
        if (screen._selectedCards.Count < 2)
            return false;
        CardModel[] picked = screen._selectedCards.Take(2).ToArray();
        a = picked[0];
        b = picked[1];
        return true;
    }

    public static bool SelectedPairIsLegal(NDeckCardSelectScreen screen)
    {
        if (!TryGetSelectedPair(screen, out CardModel a, out CardModel b))
            return true; // not enough cards yet — let vanilla handle min-count
        return CombineService.CanPair(a, b);
    }
}

/// <summary>
/// Block finishing the combine select unless the two cards share identity + rank.
/// Patched at multiple entry points: auto-complete, preview open, and final confirm.
/// </summary>
[HarmonyPatch(typeof(NDeckCardSelectScreen), "CheckIfSelectionComplete")]
public static class CardSelectCompletePatch
{
    public static bool Prefix(NDeckCardSelectScreen __instance)
    {
        if (!CombineSelectGate.IsCombinePrompt(__instance._prefs))
            return true;
        if (__instance._selectedCards.Count < 2)
            return true;

        if (!CombineSelectGate.SelectedPairIsLegal(__instance))
        {
            MainFile.Logger.Info("Combine confirm blocked: mixed rank/id pair.");
            return false; // skip original → do not complete selection
        }
        return true;
    }
}

/// <summary>
/// Main Confirm opens the preview strip without calling CheckIfSelectionComplete.
/// Reject illegal pairs before that UI can open (feels like a successful combine).
/// </summary>
[HarmonyPatch(typeof(NDeckCardSelectScreen), "PreviewSelection", new Type[] { })]
public static class CardSelectPreviewPatch
{
    public static bool Prefix(NDeckCardSelectScreen __instance)
    {
        if (!CombineSelectGate.IsCombinePrompt(__instance._prefs))
            return true;
        if (__instance._selectedCards.Count < 2)
            return true;
        if (CombineSelectGate.SelectedPairIsLegal(__instance))
            return true;

        MainFile.Logger.Info("Combine preview blocked: mixed rank/id pair.");
        return false;
    }
}
