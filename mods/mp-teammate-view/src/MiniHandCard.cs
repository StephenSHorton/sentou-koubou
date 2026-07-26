using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace MpTeammateView;

public static class MiniHandCard
{
    public static Control Create(CardModel card)
    {
        var size = DisplayConfig.ScaledCardSize;
        var wrapper = new Control
        {
            CustomMinimumSize = size,
            Size = size,
            MouseFilter = Control.MouseFilterEnum.Stop,
            ClipContents = false,
        };

        NCard? nCard = null;
        try
        {
            nCard = NCard.Create(card);
            if (nCard != null)
            {
                nCard.PivotOffset = Vector2.Zero;
                nCard.Scale = Vector2.One * DisplayConfig.MiniCardScale;
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

        wrapper.MouseEntered += () =>
        {
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

        wrapper.TreeExiting += () =>
        {
            try
            {
                if (nCard != null && GodotObject.IsInstanceValid(nCard))
                    nCard.QueueFree();
            }
            catch
            {
                // ignore
            }
        };

        return wrapper;
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
