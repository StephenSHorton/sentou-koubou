using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MpTeammateView.Utils;

namespace MpTeammateView;

public static class MiniHandCard
{
    public static Control Create(CardModel card, Player? player, Action<InputEvent>? dragInputHandler = null)
    {
        var size = HandDisplaySettings.GetScaledCardSize();
        var wrapper = new Control
        {
            CustomMinimumSize = size,
            Size = size,
            MouseFilter = Control.MouseFilterEnum.Stop,
            ClipContents = false,
        };

        NCard? nCard = null;
        Control? highlightOverlay = null;

        try
        {
            nCard = NCard.Create(card);
            if (nCard != null)
            {
                nCard.PivotOffset = Vector2.Zero;
                nCard.Scale = Vector2.One * HandDisplaySettings.GetMiniCardScale();
                nCard.Position = size / 2f;
                nCard.MouseFilter = Control.MouseFilterEnum.Ignore;
                wrapper.AddChild(nCard);
                Callable.From(() =>
                {
                    if (nCard == null || !GodotObject.IsInstanceValid(nCard))
                        return;
                    try
                    {
                        nCard.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
                        ApplyMiniTeammateCardDescription(nCard, card);
                        PropagateMouseIgnore(nCard);
                    }
                    catch (Exception e)
                    {
                        MainFile.Logger.Warn($"Mini card visual update: {e.Message}");
                    }
                }).CallDeferred();
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"NCard.Create failed: {e.Message}");
        }

        if (HandDisplaySettings.TryGetHighlightColor(card, out var color))
        {
            highlightOverlay = CreateHighlightOverlay(color);
            wrapper.AddChild(highlightOverlay);
        }

        wrapper.MouseEntered += () =>
        {
            if (TeammateViewHost.HandsHidden)
                return;
            try
            {
                var tip = NHoverTipSet.CreateAndShow(wrapper, new CardHoverTip(card), HoverTipAlignment.Right);
                tip?.SetFollowOwner();
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"Card tip failed: {e.Message}");
            }
        };
        wrapper.MouseExited += () =>
        {
            try
            {
                if (GodotObject.IsInstanceValid(wrapper))
                    NHoverTipSet.Remove(wrapper);
            }
            catch
            {
                // ignore
            }
        };

        wrapper.GuiInput += @event =>
        {
            if (TryHandleChatClick(@event, card, player, wrapper))
                return;
            dragInputHandler?.Invoke(@event);
        };

        wrapper.TreeExiting += () =>
        {
            try
            {
                if (nCard != null && GodotObject.IsInstanceValid(nCard))
                    nCard.QueueFree();
                if (highlightOverlay != null && GodotObject.IsInstanceValid(highlightOverlay))
                    highlightOverlay.QueueFree();
            }
            catch
            {
                // ignore
            }
        };

        return wrapper;
    }

    private static bool TryHandleChatClick(InputEvent @event, CardModel card, Player? player, Control wrapper)
    {
        if (@event is not InputEventMouseButton
            {
                Pressed: true,
                AltPressed: true,
                ButtonIndex: var buttonIndex,
            })
            return false;

        var handled = false;
        if (buttonIndex is MouseButton.Left or MouseButton.Right && player != null)
        {
            handled |= LemonSpireInterop.TrySendHandCardToChat(player, card);
            handled |= LemonSpireInterop.TryRequestHandCardFlash(player, card);
        }

        if (buttonIndex == MouseButton.Left)
            handled |= TypingInterop.TrySendCardLink(card);

        if (!handled) return false;
        wrapper.GetViewport()?.SetInputAsHandled();
        return true;
    }

    private static void ApplyMiniTeammateCardDescription(NCard nCard, CardModel model)
    {
        if (!nCard.IsNodeReady())
            return;
        var label = nCard.GetNodeOrNull<MegaRichTextLabel>("%DescriptionLabel");
        if (label == null)
            return;
        var text = model.GetDescriptionForPile(PileType.Hand, model.CurrentTarget);
        label.SetTextAutoSize("[center]" + text + "[/center]");
    }

    private static Control CreateHighlightOverlay(Color color)
    {
        var overlay = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore };
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        overlay.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            DrawCenter = false,
            BorderColor = color,
            BorderWidthLeft = 3,
            BorderWidthTop = 3,
            BorderWidthRight = 3,
            BorderWidthBottom = 3,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
        });
        return overlay;
    }

    private static void PropagateMouseIgnore(Control node)
    {
        node.MouseFilter = Control.MouseFilterEnum.Ignore;
        foreach (var child in node.GetChildren())
        {
            if (child is Control c)
                PropagateMouseIgnore(c);
        }
    }
}
