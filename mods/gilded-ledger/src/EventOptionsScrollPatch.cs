using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Events;

namespace GildedLedger;

/// <summary>
/// Vanilla event options live in a plain VBox with no scroll — long lists (e.g. every
/// enchantment on Gilded Ledger) clip off-screen. Wrap <c>%OptionsContainer</c> in a
/// <see cref="ScrollContainer"/> so excess options can be scrolled.
/// </summary>
[HarmonyPatch(typeof(NEventLayout), nameof(NEventLayout.AddOptions))]
internal static class EventOptionsScrollPatch
{
    private const string ScrollName = "GildedLedgerOptionsScroll";

    /// <summary>Fraction of viewport height reserved for the option list.</summary>
    private const float MaxHeightViewportFraction = 0.48f;

    /// <summary>Absolute floor/ceiling so tiny/huge windows still work.</summary>
    private const float MinScrollHeight = 160f;
    private const float MaxScrollHeight = 560f;

    private static readonly FieldInfo? OptionsField =
        AccessTools.Field(typeof(NEventLayout), "_optionsContainer");

    [HarmonyPostfix]
    private static void Postfix(NEventLayout __instance)
    {
        try
        {
            EnsureScrollable(__instance);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Event options scroll wrap failed: {e.Message}");
        }
    }

    private static void EnsureScrollable(NEventLayout layout)
    {
        VBoxContainer? options = OptionsField?.GetValue(layout) as VBoxContainer;
        if (options == null || !GodotObject.IsInstanceValid(options))
        {
            return;
        }

        ScrollContainer scroll;
        if (options.GetParent() is ScrollContainer existing
            && existing.Name == ScrollName)
        {
            scroll = existing;
        }
        else
        {
            Node? parent = options.GetParent();
            if (parent == null)
            {
                return;
            }

            // Preserve layout slot (index + size flags) when inserting the scroll wrapper.
            int index = options.GetIndex();
            Control.SizeFlags hFlags = options.SizeFlagsHorizontal;
            Control.SizeFlags vFlags = options.SizeFlagsVertical;

            parent.RemoveChild(options);

            scroll = new ScrollContainer
            {
                Name = ScrollName,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
                SizeFlagsHorizontal = hFlags,
                SizeFlagsVertical = vFlags,
                // Clip content so buttons don't paint outside the scroll area.
                ClipContents = true,
            };

            // VBox sizes to children; horizontal fill matches option button width.
            options.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            options.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;

            parent.AddChild(scroll);
            parent.MoveChild(scroll, index);
            scroll.AddChild(options);
        }

        float viewportH = layout.GetViewportRect().Size.Y;
        if (viewportH <= 1f)
        {
            viewportH = 720f;
        }

        float maxH = Mathf.Clamp(
            viewportH * MaxHeightViewportFraction,
            MinScrollHeight,
            MaxScrollHeight);

        // Fixed height so ScrollContainer scrolls when the VBox is taller than this.
        // Width follows parent (expand-fill); height is the scroll viewport.
        scroll.CustomMinimumSize = new Vector2(0, maxH);
        // Prefer a hard height so the list doesn't grow past the screen.
        scroll.Size = new Vector2(scroll.Size.X, maxH);

        // After buttons are added, grow scroll slightly if content is short (avoids
        // a huge empty scroll area on 2-option pages). Cap at maxH.
        int count = options.GetChildCount();
        if (count > 0)
        {
            // Defer so button minimum sizes are resolved.
            Callable.From(() => FitScrollHeight(scroll, options, maxH)).CallDeferred();
        }
    }

    private static void FitScrollHeight(ScrollContainer scroll, VBoxContainer options, float maxH)
    {
        if (!GodotObject.IsInstanceValid(scroll) || !GodotObject.IsInstanceValid(options))
        {
            return;
        }

        // Sum child minimum heights + separations for a content-sized viewport when short.
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
                h = 48f; // fallback estimate for option buttons
            }
            contentH += h;
        }

        if (visible > 1)
        {
            // VBox theme separation is typically a few pixels; small fudge is fine.
            contentH += (visible - 1) * 6f;
        }

        // Small padding so the last button isn't flush against the clip edge.
        contentH += 8f;

        float hFinal = Mathf.Min(contentH, maxH);
        hFinal = Mathf.Max(hFinal, MinScrollHeight * 0.5f);
        scroll.CustomMinimumSize = new Vector2(scroll.CustomMinimumSize.X, hFinal);
    }
}
