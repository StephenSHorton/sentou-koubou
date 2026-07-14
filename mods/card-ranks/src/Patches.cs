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
        Loc.EnsureRestSiteEntries();
        if (__result.All(o => o.OptionId != CombineRestSiteOption.Id))
            __result.Add(new CombineRestSiteOption(player));
        if (__result.All(o => o.OptionId != CloneRestSiteOption.Id))
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
/// Gate each click: first pick must be a candidate; second pick must CanPair with the first.
/// Vanilla auto-opens PreviewSelection when MaxSelect is reached — illegal seconds never add.
/// </summary>
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

/// <summary>
/// After the grid appears, dim non-candidates so Rank 3 / blocked basics aren't tempting.
/// </summary>
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

/// <summary>
/// Block finishing the combine select unless the two cards share identity + rank.
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
            return false;
        }
        return true;
    }
}

/// <summary>
/// Auto-preview on 2nd click / Confirm must not open for illegal pairs.
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
        // Drop the illegal second selection if it somehow got in.
        try
        {
            if (__instance._selectedCards.Count >= 2)
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
            MainFile.Logger.Warn($"Failed to unselect illegal pair: {e.Message}");
        }
        return false;
    }
}
