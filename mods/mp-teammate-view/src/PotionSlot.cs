using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MpTeammateView.Utils;

namespace MpTeammateView;

public static class PotionSlot
{
    public static Control Create(Player player, PotionModel potion)
    {
        var slotSize = PotionDisplaySettings.GetSlotSize();
        var slot = new Control
        {
            CustomMinimumSize = slotSize,
            Size = slotSize,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };

        var image = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Texture = potion.Image,
            SelfModulate = Colors.White,
        };
        image.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        slot.AddChild(image);

        if (PotionDisplaySettings.TryGetHighlightColor(potion, out var color))
        {
            var highlightBorder = new Panel
            {
                Visible = true,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            highlightBorder.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            highlightBorder.AddThemeStyleboxOverride("panel", CreateHighlightStyle(color));
            slot.AddChild(highlightBorder);
        }

        NHoverTipSet? tip = null;
        slot.MouseEntered += () =>
        {
            try
            {
                tip = NHoverTipSet.CreateAndShow(slot, potion.HoverTips.ToArray());
                if (tip != null)
                    tip.GlobalPosition = slot.GlobalPosition + Vector2.Down * 40f;
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"Potion tip failed: {e.Message}");
            }
        };
        slot.MouseExited += () =>
        {
            try
            {
                NHoverTipSet.Remove(slot);
            }
            catch
            {
                // ignore
            }

            tip = null;
        };

        slot.GuiInput += @event =>
        {
            if (@event is not InputEventMouseButton
                {
                    Pressed: true,
                    AltPressed: true,
                    ButtonIndex: var buttonIndex,
                })
                return;

            var handled = false;
            if (buttonIndex is MouseButton.Left or MouseButton.Right)
                handled |= LemonSpireInterop.TrySendPotionToChat(player, potion);
            if (buttonIndex == MouseButton.Left)
                handled |= TypingInterop.TrySendPotionLink(potion);
            if (!handled) return;
            slot.GetViewport()?.SetInputAsHandled();
        };

        return slot;
    }

    private static StyleBoxFlat CreateHighlightStyle(Color borderColor) => new()
    {
        DrawCenter = false,
        BorderColor = borderColor,
        BorderWidthLeft = 2,
        BorderWidthTop = 2,
        BorderWidthRight = 2,
        BorderWidthBottom = 2,
        CornerRadiusTopLeft = 6,
        CornerRadiusTopRight = 6,
        CornerRadiusBottomLeft = 6,
        CornerRadiusBottomRight = 6,
    };
}
