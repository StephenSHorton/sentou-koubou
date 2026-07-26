using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace MpTeammateView;

public static class PotionSlot
{
    public static Control Create(PotionModel potion)
    {
        var slot = new Control
        {
            CustomMinimumSize = Vector2.One * DisplayConfig.PotionSlotPx,
            Size = Vector2.One * DisplayConfig.PotionSlotPx,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };

        var image = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Texture = potion.Image,
        };
        image.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        slot.AddChild(image);

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

        return slot;
    }
}
