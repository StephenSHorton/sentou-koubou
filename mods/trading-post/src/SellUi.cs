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
        // Tree order: Use → Sell → Discard (when free-layout, we also shift positions).
        int discardIdx = discard.GetIndex();
        parent.MoveChild(sell, Math.Max(0, discardIdx));

        // Duplicate() keeps absolute Position/anchors of Discard, so Sell would sit on
        // top of it. Reflow into a third row (container layout or manual stack).
        LayoutPotionSellButton(popup, parent, sell, discard);

        ApplyPotionSellLabel(sell, potion);
        WirePotionSellPress(sell, popup, potion);
        MainFile.Logger.Info($"Potion sell option injected for {potion.Id} ({SellPricing.PotionSellPrice(potion)}g).");
    }

    /// <summary>
    /// Place Sell as its own row between Use and Discard (or above Discard).
    /// Vanilla popup uses fixed positions, not a reflowing box — must shift Discard down.
    /// </summary>
    private static void LayoutPotionSellButton(
        NPotionPopup popup, Node parent, Control sell, Control discard)
    {
        const float gap = 6f;

        // If parent is a box/container that sizes children, clear absolute coords so it reflows.
        if (parent is BoxContainer box)
        {
            sell.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
            sell.Position = Vector2.Zero;
            sell.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            if (sell.CustomMinimumSize.Y < 1f && discard.Size.Y > 1f)
            {
                sell.CustomMinimumSize = new Vector2(discard.Size.X, discard.Size.Y);
            }
            // Ensure Sell is directly above Discard in child order.
            int discardIdx = discard.GetIndex();
            int sellIdx = sell.GetIndex();
            if (sellIdx > discardIdx)
            {
                box.MoveChild(sell, discardIdx);
            }
            return;
        }

        // Free layout (typical for NPotionPopup): clone sat on Discard's pixel position.
        // Put Sell where Discard is, push Discard down by one row, grow the popup.
        float rowH = discard.Size.Y;
        if (rowH < 8f)
        {
            rowH = discard.GetCombinedMinimumSize().Y;
        }
        if (rowH < 8f)
        {
            rowH = discard.CustomMinimumSize.Y;
        }
        if (rowH < 8f)
        {
            rowH = 40f;
        }

        Vector2 discardPos = discard.Position;
        Vector2 discardSize = discard.Size.X > 1f && discard.Size.Y > 1f
            ? discard.Size
            : new Vector2(Math.Max(discard.CustomMinimumSize.X, 120f), rowH);

        // Copy layout mode from Discard, then claim its slot for Sell.
        sell.AnchorLeft = discard.AnchorLeft;
        sell.AnchorTop = discard.AnchorTop;
        sell.AnchorRight = discard.AnchorRight;
        sell.AnchorBottom = discard.AnchorBottom;
        sell.OffsetLeft = discard.OffsetLeft;
        sell.OffsetTop = discard.OffsetTop;
        sell.OffsetRight = discard.OffsetRight;
        sell.OffsetBottom = discard.OffsetBottom;
        sell.Size = discardSize;
        sell.CustomMinimumSize = discardSize;
        // Position after anchors/offsets so free-layout nodes land correctly.
        sell.Position = discardPos;
        sell.Size = discardSize;

        float dy = rowH + gap;
        // Prefer offset shift when the control is offset-driven; else Position only.
        // Do not apply both — that double-moves Discard and can re-overlap.
        bool offsetDriven = Math.Abs(discard.OffsetTop) > 0.5f
                            || Math.Abs(discard.OffsetBottom) > 0.5f
                            || discard.AnchorTop > 0.001f
                            || discard.AnchorBottom > 0.001f;
        if (offsetDriven)
        {
            discard.OffsetTop += dy;
            discard.OffsetBottom += dy;
        }
        else
        {
            discard.Position = new Vector2(discardPos.X, discardPos.Y + dy);
        }

        GrowPotionPopupForExtraRow(popup, parent, dy);
    }

    private static void GrowPotionPopupForExtraRow(NPotionPopup popup, Node parent, float dy)
    {
        // Grow the immediate parent and the popup root so Discard isn't clipped.
        foreach (Node n in new Node?[] { parent, popup, FindNamedDescendant(popup, "Container")
            ?? FindNamedDescendant(popup, "%Container") }.OfType<Node>())
        {
            if (n is not Control c || !GodotObject.IsInstanceValid(c))
            {
                continue;
            }
            if (c.CustomMinimumSize.Y > 0f)
            {
                c.CustomMinimumSize = new Vector2(c.CustomMinimumSize.X, c.CustomMinimumSize.Y + dy);
            }
            if (c.Size.Y > 0f)
            {
                c.Size = new Vector2(c.Size.X, c.Size.Y + dy);
            }
            // Bottom-anchored panels: extend bottom offset
            if (c.AnchorBottom > 0.01f || Math.Abs(c.OffsetBottom) > 0.01f)
            {
                c.OffsetBottom += dy;
            }
        }
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

        if (!MerchantContext.IsInShop())
        {
            return;
        }
        if (relic == null)
        {
            return;
        }
        // Inventory inspect can open for non-owned encyclopedia relics — only sell owned.
        bool mine = false;
        try
        {
            mine = LocalContext.IsMine(relic) || relic.Owner != null && LocalContext.IsMe(relic.Owner);
        }
        catch
        {
            mine = relic.Owner != null;
        }
        if (!mine)
        {
            MainFile.Logger.Info($"Relic sell skipped (not owned): {relic.Id}");
            return;
        }
        if (!SellPricing.CanSellRelic(relic))
        {
            MainFile.Logger.Info(
                $"Relic sell skipped (not tradable or no merchant cost): {relic.Id} " +
                $"tradable={relic.IsTradable} cost={relic.MerchantCost}");
            return;
        }

        int gold = SellPricing.RelicSellPrice(relic);
        // Parent to the full inspect screen (not the inner popup) so we aren't clipped
        // and sit in a reliable bottom-center screen spot.
        Button sell = TradeUi.MakePaintedButton($"Sell  {gold}g", () => OnRelicSellPressed(screen, relic),
            minWidth: 300, minHeight: 92);
        sell.Name = RelicSellButtonName;
        sell.ZIndex = 200;
        sell.MouseFilter = Control.MouseFilterEnum.Stop;
        screen.AddChild(sell);

        // Bottom-center of the inspect overlay — always on-screen.
        sell.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        sell.AnchorLeft = 0.5f;
        sell.AnchorRight = 0.5f;
        sell.AnchorTop = 1f;
        sell.AnchorBottom = 1f;
        sell.GrowHorizontal = Control.GrowDirection.Both;
        sell.GrowVertical = Control.GrowDirection.Begin;
        sell.OffsetLeft = -150;
        sell.OffsetRight = 150;
        sell.OffsetTop = -150;
        sell.OffsetBottom = -36;

        // After layout settles, nudge under the description if we can find it.
        TaskHelper.RunSafely(DeferRelicSellPosition(screen, sell));

        MainFile.Logger.Info($"Relic sell button injected for {relic.Id} ({gold}g) on inspect screen.");
    }

    private static async Task DeferRelicSellPosition(NInspectRelicScreen screen, Button sell)
    {
        try
        {
            SceneTree? tree = screen.GetTree();
            if (tree != null)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
            else
            {
                await Task.Delay(30);
            }
        }
        catch
        {
            await Task.Delay(30);
        }
        PositionRelicSellUnderContent(screen, sell);
    }

    private static void PositionRelicSellUnderContent(NInspectRelicScreen screen, Button sell)
    {
        if (!GodotObject.IsInstanceValid(screen) || !GodotObject.IsInstanceValid(sell))
        {
            return;
        }
        try
        {
            // Prefer sitting just below the description / popup content in screen space.
            Control? desc = AccessTools.Field(typeof(NInspectRelicScreen), "_description")
                ?.GetValue(screen) as Control;
            Control? popup = AccessTools.Field(typeof(NInspectRelicScreen), "_popup")
                ?.GetValue(screen) as Control;
            Control? anchor = desc ?? popup;
            if (anchor == null || !GodotObject.IsInstanceValid(anchor))
            {
                return;
            }

            // Convert anchor bottom-center to screen local coords for the button.
            Vector2 bottomCenter = anchor.GlobalPosition
                + new Vector2(anchor.Size.X * 0.5f, anchor.Size.Y + 12f);
            Vector2 local = screen.GetGlobalTransformWithCanvas().AffineInverse() * bottomCenter;
            sell.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
            sell.AnchorLeft = 0f;
            sell.AnchorRight = 0f;
            sell.AnchorTop = 0f;
            sell.AnchorBottom = 0f;
            float w = sell.CustomMinimumSize.X;
            float h = sell.CustomMinimumSize.Y;
            sell.Position = new Vector2(local.X - w * 0.5f, local.Y);
            sell.Size = new Vector2(w, h);

            // Keep on-screen if content is near the bottom edge.
            Vector2 screenSize = screen.Size;
            if (screenSize.Y > 1f)
            {
                float maxY = screenSize.Y - h - 24f;
                if (sell.Position.Y > maxY)
                {
                    sell.Position = new Vector2(sell.Position.X, maxY);
                }
                if (sell.Position.Y < 24f)
                {
                    sell.Position = new Vector2(sell.Position.X, 24f);
                }
                float maxX = screenSize.X - w - 16f;
                float x = Math.Clamp(sell.Position.X, 16f, Math.Max(16f, maxX));
                sell.Position = new Vector2(x, sell.Position.Y);
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Relic sell reposition failed: {e.Message}");
        }
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
