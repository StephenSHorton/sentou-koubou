using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using MegaCrit.Sts2.Core.Context;

namespace TradingPost;

/// <summary>
/// Shop-only sell UI: extra potion popup option + painted Sell button on relic inspect.
/// </summary>
public static class SellUi
{
    public const string PotionSellButtonName = "TradingPostSellPotion";
    public const string RelicSellButtonName = "TradingPostSellRelic";

    // ------------------------------------------------------------ potion dropdown

    /// <summary>
    /// After the vanilla Use/Discard popup is ready, clone Discard into a Sell option
    /// when we're at the merchant. Only for the local player's potions.
    /// </summary>
    public static void TryInjectPotionSellOption(NPotionPopup popup, PotionModel? potion)
    {
        if (popup == null || !GodotObject.IsInstanceValid(popup))
        {
            return;
        }
        if (!MerchantContext.IsInShop() || potion == null)
        {
            return;
        }
        if (!LocalContext.IsMine(potion))
        {
            return;
        }
        if (popup.FindChild(PotionSellButtonName, recursive: true, owned: false) != null)
        {
            return;
        }

        Control? discard = FindNamedDescendant(popup, "DiscardButton")
                           ?? FindNamedDescendant(popup, "%DiscardButton");
        if (discard == null)
        {
            MainFile.Logger.Warn("Potion sell: DiscardButton not found on popup.");
            return;
        }

        Node parent = discard.GetParent();
        if (parent == null)
        {
            return;
        }

        Control sell = (Control)discard.Duplicate();
        sell.Name = PotionSellButtonName;
        parent.AddChild(sell);
        // Place after Use, before Discard when possible
        int discardIdx = discard.GetIndex();
        parent.MoveChild(sell, Math.Max(0, discardIdx));

        ApplyPotionSellLabel(sell, potion);
        WirePotionSellPress(sell, popup, potion);
        MainFile.Logger.Info($"Potion sell option injected for {potion.Id} ({SellPricing.PotionSellPrice(potion)}g).");
    }

    private static void ApplyPotionSellLabel(Control sell, PotionModel potion)
    {
        int gold = SellPricing.PotionSellPrice(potion);
        string text = $"Sell ({gold}g)";

        if (sell is NPotionPopupButton potionBtn)
        {
            try
            {
                Loc.EnsurePotionPopupSellEntry(gold);
                potionBtn.SetLocKey("POTION_POPUP.sell");
                return;
            }
            catch
            {
                // fall through to raw label
            }
        }

        Label? label = sell as Label
                       ?? sell.FindChild("*", recursive: true, owned: false) as Label
                       ?? FindFirstLabel(sell);
        if (label != null)
        {
            label.Text = text;
        }
    }

    private static void WirePotionSellPress(Control sell, NPotionPopup popup, PotionModel potion)
    {
        // NClickableControl.Released is a custom event (not parameterless).
        if (sell is NClickableControl clickable)
        {
            clickable.Released += _ => OnPotionSellPressed(popup, potion);
            return;
        }
        if (sell is BaseButton button)
        {
            button.Pressed += () => OnPotionSellPressed(popup, potion);
            return;
        }
        if (sell.HasSignal("released"))
        {
            sell.Connect("released", Callable.From(() => OnPotionSellPressed(popup, potion)));
        }
        else if (sell.HasSignal("pressed"))
        {
            sell.Connect("pressed", Callable.From(() => OnPotionSellPressed(popup, potion)));
        }
    }

    private static void OnPotionSellPressed(NPotionPopup popup, PotionModel potion)
    {
        if (!MerchantContext.IsInShop())
        {
            return;
        }
        TradeSynchronizer? sync = TradeSynchronizer.Instance;
        if (sync == null)
        {
            return;
        }
        int gold = SellPricing.PotionSellPrice(potion);
        string title = SafePotionTitle(potion);
        TradeUi.Confirm($"Sell {title} to the merchant for {gold} gold?", ok =>
        {
            if (!ok)
            {
                return;
            }
            try
            {
                popup.Remove();
            }
            catch
            {
                try { popup.QueueFree(); } catch { /* ignore */ }
            }
            TaskHelper.RunSafely(sync.SellPotionLocal(potion));
        });
    }

    // ------------------------------------------------------------ relic inspect

    public static void TryInjectRelicSellButton(NInspectRelicScreen screen, RelicModel? relic)
    {
        if (screen == null || !GodotObject.IsInstanceValid(screen))
        {
            return;
        }

        // Always clear previous sell button when browsing left/right.
        RemoveNamed(screen, RelicSellButtonName);

        if (!MerchantContext.IsInShop() || !SellPricing.CanSellRelic(relic))
        {
            return;
        }
        if (relic == null || !LocalContext.IsMine(relic))
        {
            return;
        }

        Control host = FindRelicPopupHost(screen) ?? screen;
        int gold = SellPricing.RelicSellPrice(relic);
        Button sell = TradeUi.MakePaintedButton($"Sell  ·  {gold}g", () => OnRelicSellPressed(screen, relic),
            minWidth: 280, minHeight: 72);
        sell.Name = RelicSellButtonName;
        host.AddChild(sell);

        // Bottom-center of the inspect popup
        sell.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        sell.AnchorLeft = 0.5f;
        sell.AnchorRight = 0.5f;
        sell.OffsetLeft = -140;
        sell.OffsetRight = 140;
        sell.OffsetTop = -110;
        sell.OffsetBottom = -38;
        sell.GrowHorizontal = Control.GrowDirection.Both;

        MainFile.Logger.Info($"Relic sell button injected for {relic.Id} ({gold}g).");
    }

    private static void OnRelicSellPressed(NInspectRelicScreen screen, RelicModel relic)
    {
        if (!MerchantContext.IsInShop() || !SellPricing.CanSellRelic(relic))
        {
            return;
        }
        TradeSynchronizer? sync = TradeSynchronizer.Instance;
        if (sync == null)
        {
            return;
        }
        int gold = SellPricing.RelicSellPrice(relic);
        string title = SafeRelicTitle(relic);
        TradeUi.Confirm($"Sell {title} to the merchant for {gold} gold?", ok =>
        {
            if (!ok)
            {
                return;
            }
            try
            {
                screen.Close();
            }
            catch
            {
                // ignore
            }
            TaskHelper.RunSafely(sync.SellRelicLocal(relic));
        });
    }

    private static Control? FindRelicPopupHost(NInspectRelicScreen screen)
    {
        // Prefer the popup panel if present so the button scrolls/animates with it.
        FieldInfo? popupField = AccessTools.Field(typeof(NInspectRelicScreen), "_popup");
        if (popupField?.GetValue(screen) is Control popup && GodotObject.IsInstanceValid(popup))
        {
            return popup;
        }
        Node? named = screen.FindChild("Popup", recursive: true, owned: false)
                      ?? screen.FindChild("%Popup", recursive: true, owned: false);
        return named as Control;
    }

    // ------------------------------------------------------------ helpers

    private static Control? FindNamedDescendant(Node root, string name)
    {
        if (root.Name == name)
        {
            return root as Control;
        }
        // Unique-name %Foo paths
        if (name.StartsWith('%'))
        {
            try
            {
                return root.GetNodeOrNull<Control>(name);
            }
            catch
            {
                // ignore
            }
        }
        foreach (Node child in root.GetChildren())
        {
            if (child.Name == name && child is Control c)
            {
                return c;
            }
            Control? nested = FindNamedDescendant(child, name);
            if (nested != null)
            {
                return nested;
            }
        }
        return null;
    }

    private static Label? FindFirstLabel(Node root)
    {
        if (root is Label l)
        {
            return l;
        }
        foreach (Node child in root.GetChildren())
        {
            Label? found = FindFirstLabel(child);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    private static void RemoveNamed(Node root, string name)
    {
        Node? existing = root.FindChild(name, recursive: true, owned: false);
        existing?.QueueFree();
    }

    private static string SafePotionTitle(PotionModel potion)
    {
        try
        {
            string t = potion.Title?.GetFormattedText() ?? "";
            if (!string.IsNullOrWhiteSpace(t) && !t.Contains('.'))
            {
                return t;
            }
        }
        catch
        {
            // ignore
        }
        return potion.Id.Entry;
    }

    private static string SafeRelicTitle(RelicModel relic)
    {
        try
        {
            string t = relic.Title?.GetFormattedText() ?? "";
            if (!string.IsNullOrWhiteSpace(t) && !t.Contains('.'))
            {
                return t;
            }
        }
        catch
        {
            // ignore
        }
        return relic.Id.Entry;
    }
}

// ------------------------------------------------------------ Harmony patches

[HarmonyPatch(typeof(NPotionHolder), nameof(NPotionHolder.OpenPotionPopup))]
public static class PotionHolderOpenPopupPatch
{
    public static void Postfix(NPotionHolder __instance)
    {
        try
        {
            // Holder.Potion is the NPotion node; the model lives on it.
            PotionModel? potion = __instance.Potion?.Model;
            NPotionPopup? popup = FindPopupNear(__instance);
            if (popup == null)
            {
                SceneTree? tree = __instance.GetTree();
                if (tree != null)
                {
                    SceneTreeTimer timer = tree.CreateTimer(0.01);
                    timer.Timeout += () =>
                    {
                        try
                        {
                            NPotionPopup? late = FindPopupNear(__instance);
                            if (late != null)
                            {
                                SellUi.TryInjectPotionSellOption(late, potion);
                            }
                        }
                        catch (Exception e)
                        {
                            MainFile.Logger.Warn($"Late potion sell inject failed: {e.Message}");
                        }
                    };
                }
                return;
            }
            SellUi.TryInjectPotionSellOption(popup, potion);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Potion sell inject failed: {e.Message}");
        }
    }

    private static NPotionPopup? FindPopupNear(NPotionHolder holder)
    {
        foreach (Node child in holder.GetChildren())
        {
            if (child is NPotionPopup p)
            {
                return p;
            }
        }
        // Search up a few parents for a just-opened popup
        Node? n = holder.GetParent();
        for (int depth = 0; depth < 6 && n != null; depth++, n = n.GetParent())
        {
            foreach (Node child in n.GetChildren())
            {
                if (child is NPotionPopup p)
                {
                    return p;
                }
            }
        }
        // Scene tree root scan (last resort, small tree of popups)
        SceneTree? tree = holder.GetTree();
        if (tree?.Root != null)
        {
            return FindPopupRecursive(tree.Root, maxDepth: 12, depth: 0);
        }
        return null;
    }

    private static NPotionPopup? FindPopupRecursive(Node node, int maxDepth, int depth)
    {
        if (node is NPotionPopup p)
        {
            return p;
        }
        if (depth >= maxDepth)
        {
            return null;
        }
        foreach (Node child in node.GetChildren())
        {
            NPotionPopup? found = FindPopupRecursive(child, maxDepth, depth + 1);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }
}

[HarmonyPatch(typeof(NInspectRelicScreen), "SetRelic")]
public static class InspectRelicSetRelicPatch
{
    public static void Postfix(NInspectRelicScreen __instance, object[] __args)
    {
        try
        {
            RelicModel? relic = null;
            if (__args.Length > 0)
            {
                if (__args[0] is RelicModel model)
                {
                    relic = model;
                }
                else if (__args[0] is int index)
                {
                    relic = RelicAt(__instance, index);
                }
            }
            relic ??= CurrentRelic(__instance);
            SellUi.TryInjectRelicSellButton(__instance, relic);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Relic sell inject failed: {e.Message}");
        }
    }

    internal static RelicModel? CurrentRelic(NInspectRelicScreen screen)
    {
        FieldInfo? relicsField = AccessTools.Field(typeof(NInspectRelicScreen), "_relics");
        FieldInfo? indexField = AccessTools.Field(typeof(NInspectRelicScreen), "_index");
        if (relicsField?.GetValue(screen) is not System.Collections.IList list)
        {
            return null;
        }
        int idx = indexField?.GetValue(screen) is int i ? i : 0;
        return RelicAt(screen, idx, list);
    }

    private static RelicModel? RelicAt(NInspectRelicScreen screen, int index,
        System.Collections.IList? list = null)
    {
        list ??= AccessTools.Field(typeof(NInspectRelicScreen), "_relics")?.GetValue(screen)
            as System.Collections.IList;
        if (list == null || index < 0 || index >= list.Count)
        {
            return null;
        }
        return list[index] as RelicModel;
    }
}

[HarmonyPatch(typeof(NInspectRelicScreen), nameof(NInspectRelicScreen.Open))]
public static class InspectRelicOpenPatch
{
    public static void Postfix(NInspectRelicScreen __instance)
    {
        try
        {
            SellUi.TryInjectRelicSellButton(__instance, InspectRelicSetRelicPatch.CurrentRelic(__instance));
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Relic sell open inject failed: {e.Message}");
        }
    }
}
