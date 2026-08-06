using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;

namespace GildedLedger;

/// <summary>
/// Vanilla event options live in a plain VBox with no scroll — long lists (e.g. every
/// enchantment on Gilded Ledger) clip off-screen. Only for those long lists, wrap
/// <c>%OptionsContainer</c> in a <see cref="ScrollContainer"/>.
/// <para>
/// Critical: do <b>not</b> wrap short lists (Neow, shops, 2-choice events). A forced
/// scroll viewport steals vertical space and can push options/continue off-screen.
/// </para>
/// </summary>
[HarmonyPatch(typeof(NEventLayout), nameof(NEventLayout.AddOptions))]
internal static class EventOptionsScrollPatch
{
    private const string ScrollName = "GildedLedgerOptionsScroll";

    /// <summary>
    /// Only engage scroll when the option count is at least this high.
    /// Gilded Ledger's enchant page is large; Neow/vanilla events stay untouched.
    /// </summary>
    private const int ScrollThreshold = 6;

    /// <summary>Fraction of viewport height reserved for a long option list.</summary>
    private const float MaxHeightViewportFraction = 0.42f;

    private const float MinScrollHeight = 200f;
    private const float MaxScrollHeight = 520f;

    private static readonly FieldInfo? OptionsField =
        AccessTools.Field(typeof(NEventLayout), "_optionsContainer");

    private static readonly FieldInfo? EventField =
        AccessTools.Field(typeof(NEventLayout), "_event");

    [HarmonyPostfix]
    private static void Postfix(NEventLayout __instance)
    {
        try
        {
            AfterAddOptions(__instance);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Event options scroll wrap failed: {e.Message}");
        }
    }

    private static void AfterAddOptions(NEventLayout layout)
    {
        VBoxContainer? options = OptionsField?.GetValue(layout) as VBoxContainer;
        if (options == null || !GodotObject.IsInstanceValid(options))
        {
            return;
        }

        int count = options.GetChildCount();
        bool wantScroll = count >= ScrollThreshold && IsGildedLedgerEnchantPage(layout);

        if (!wantScroll)
        {
            // Restore vanilla layout for Neow / short pages (also undoes a prior wrap).
            UnwrapIfNeeded(options);
            return;
        }

        ScrollContainer scroll = EnsureWrapped(options);
        float maxH = ComputeMaxHeight(layout);

        // Cap only — do not reserve maxH when content is shorter (deferred measure).
        scroll.CustomMinimumSize = new Vector2(0, 0);
        scroll.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        Callable.From(() => FitScrollHeight(scroll, options, maxH)).CallDeferred();
    }

    /// <summary>
    /// Scroll is only for our multi-enchant page. Other long-option events (if any)
    /// keep vanilla layout so we never surprise-break Neow or custom ancients.
    /// </summary>
    private static bool IsGildedLedgerEnchantPage(NEventLayout layout)
    {
        if (EventField?.GetValue(layout) is not EventModel model)
        {
            return false;
        }

        // CustomID is "GILDED_LEDGER"; ModelId / Id.Entry varies by BaseLib version.
        string id = model.Id.Entry ?? string.Empty;
        if (id.Contains("GILDED_LEDGER", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Fallback: concrete type from this assembly.
        return model is GildedLedgerEvent;
    }

    private static float ComputeMaxHeight(NEventLayout layout)
    {
        float viewportH = layout.GetViewportRect().Size.Y;
        if (viewportH <= 1f)
        {
            viewportH = 720f;
        }

        return Mathf.Clamp(
            viewportH * MaxHeightViewportFraction,
            MinScrollHeight,
            MaxScrollHeight);
    }

    private static ScrollContainer EnsureWrapped(VBoxContainer options)
    {
        if (options.GetParent() is ScrollContainer existing
            && existing.Name == ScrollName)
        {
            return existing;
        }

        Node parent = options.GetParent()
            ?? throw new InvalidOperationException("Options container has no parent.");

        int index = options.GetIndex();
        Control.SizeFlags hFlags = options.SizeFlagsHorizontal;
        // Remember vanilla vertical flags on the VBox via meta so unwrap can restore.
        if (!options.HasMeta("gl_orig_vflags"))
        {
            options.SetMeta("gl_orig_vflags", (int)options.SizeFlagsVertical);
            options.SetMeta("gl_orig_hflags", (int)options.SizeFlagsHorizontal);
        }

        parent.RemoveChild(options);

        var scroll = new ScrollContainer
        {
            Name = ScrollName,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            SizeFlagsHorizontal = hFlags,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
            ClipContents = true,
        };

        options.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        options.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;

        parent.AddChild(scroll);
        parent.MoveChild(scroll, index);
        scroll.AddChild(options);
        return scroll;
    }

    private static void UnwrapIfNeeded(VBoxContainer options)
    {
        if (options.GetParent() is not ScrollContainer scroll
            || scroll.Name != ScrollName)
        {
            return;
        }

        Node? parent = scroll.GetParent();
        if (parent == null)
        {
            return;
        }

        int index = scroll.GetIndex();
        scroll.RemoveChild(options);
        parent.RemoveChild(scroll);
        scroll.QueueFree();

        if (options.HasMeta("gl_orig_vflags"))
        {
            options.SizeFlagsVertical = (Control.SizeFlags)(int)options.GetMeta("gl_orig_vflags");
            options.RemoveMeta("gl_orig_vflags");
        }

        if (options.HasMeta("gl_orig_hflags"))
        {
            options.SizeFlagsHorizontal = (Control.SizeFlags)(int)options.GetMeta("gl_orig_hflags");
            options.RemoveMeta("gl_orig_hflags");
        }

        parent.AddChild(options);
        parent.MoveChild(options, index);
    }

    private static void FitScrollHeight(ScrollContainer scroll, VBoxContainer options, float maxH)
    {
        if (!GodotObject.IsInstanceValid(scroll) || !GodotObject.IsInstanceValid(options))
        {
            return;
        }

        // If options were unwrapped since defer, do nothing.
        if (options.GetParent() != scroll)
        {
            return;
        }

        float contentH = 0f;
        int visible = 0;
        foreach (Node child in options.GetChildren())
        {
            if (child is not Control c || !c.Visible)
            {
                continue;
            }

            visible++;
            float h = c.GetCombinedMinimumSize().Y;
            if (h < 1f)
            {
                h = c.Size.Y;
            }

            if (h < 1f)
            {
                h = 52f;
            }

            contentH += h;
        }

        if (visible > 1)
        {
            contentH += (visible - 1) * 6f;
        }

        contentH += 12f;

        // Scroll viewport = min(content, max). Only caps overflow; no giant empty panel.
        float hFinal = Mathf.Min(contentH, maxH);
        scroll.CustomMinimumSize = new Vector2(0, hFinal);
    }
}
