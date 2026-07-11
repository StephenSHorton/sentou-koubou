using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;

namespace TradingPost;

/// <summary>
/// Minimal Godot UI for the Trading Post: a shop-screen button, a trade menu,
/// and popup notifications. Built from stock controls so no .pck is needed.
/// </summary>
public static class TradeUi
{
    private static Window? _openMenu;

    private static Node? Root => (Engine.GetMainLoop() as SceneTree)?.Root;

    /// <summary>Fire-and-forget info popup.</summary>
    public static void Notify(string text)
    {
        Node? root = Root;
        if (root == null)
        {
            return;
        }
        var dialog = new AcceptDialog
        {
            Title = "Trading Post",
            DialogText = text,
            Exclusive = false
        };
        dialog.Confirmed += dialog.QueueFree;
        dialog.Canceled += dialog.QueueFree;
        root.AddChild(dialog);
        dialog.PopupCentered();
    }

    /// <summary>Yes/no prompt; used for relic-trade consent.</summary>
    public static void Confirm(string text, Action<bool> onAnswer)
    {
        Node? root = Root;
        if (root == null)
        {
            onAnswer(false);
            return;
        }
        var dialog = new ConfirmationDialog
        {
            Title = "Trading Post",
            DialogText = text,
            Exclusive = true
        };
        dialog.OkButtonText = "Accept";
        dialog.CancelButtonText = "Decline";
        bool answered = false;
        dialog.Confirmed += () =>
        {
            answered = true;
            onAnswer(true);
            dialog.QueueFree();
        };
        dialog.Canceled += () =>
        {
            if (!answered)
            {
                onAnswer(false);
            }
            dialog.QueueFree();
        };
        root.AddChild(dialog);
        dialog.PopupCentered();
    }

    /// <summary>Adds the Trade button to the merchant room screen.</summary>
    public static void AddTradeButton(Control shopScreen)
    {
        var button = new Button
        {
            Name = "TradingPostButton",
            Text = "Trade"
        };
        shopScreen.AddChild(button);
        button.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomLeft);
        button.OffsetLeft = 32;
        button.OffsetRight = 232;
        button.OffsetTop = -120;
        button.OffsetBottom = -64;
        button.Pressed += OpenMenu;
    }

    private static void OpenMenu()
    {
        TradeSynchronizer? sync = TradeSynchronizer.Instance;
        if (sync == null)
        {
            return;
        }
        if (sync.RelicRequestPending)
        {
            Notify("Waiting for an answer to your relic offer…");
            return;
        }
        if (sync.LocalTradeUsed)
        {
            Notify("You already made your one trade this visit. Back to shopping!");
            return;
        }
        CloseMenu();

        var menu = new PopupPanel { Name = "TradingPostMenu" };
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        menu.AddChild(vbox);

        var title = new Label { Text = "Trading Post — one trade per shop visit" };
        vbox.AddChild(title);

        AddMenuButton(vbox, "Give gold (a gift — no strings attached)", () => PickTarget(target => PickGoldAmount(target)));
        AddMenuButton(vbox, "Give a card from your deck", () => PickTarget(target =>
            TaskHelper.RunSafely(TradeSynchronizer.Instance!.GiftCardLocal(target))));
        AddMenuButton(vbox, "Request a relic (costs ALL your gold)", () => PickTarget(target =>
            TaskHelper.RunSafely(TradeSynchronizer.Instance!.RequestRelicLocal(target))));
        AddMenuButton(vbox, "Never mind", CloseMenu);

        Root?.AddChild(menu);
        _openMenu = menu;
        menu.PopupCentered();
    }

    private static void AddMenuButton(Container parent, string text, Action onPressed)
    {
        var button = new Button { Text = text };
        button.Pressed += () =>
        {
            CloseMenu();
            onPressed();
        };
        parent.AddChild(button);
    }

    private static void CloseMenu()
    {
        if (_openMenu != null && GodotObject.IsInstanceValid(_openMenu))
        {
            _openMenu.QueueFree();
        }
        _openMenu = null;
    }

    /// <summary>With one co-op partner, picks them directly; otherwise shows a chooser.</summary>
    private static void PickTarget(Action<Player> onPicked)
    {
        TradeSynchronizer? sync = TradeSynchronizer.Instance;
        if (sync == null)
        {
            return;
        }
        IReadOnlyList<Player> others = sync.OtherPlayers;
        if (others.Count == 0)
        {
            Notify("No one to trade with — you're climbing solo.");
            return;
        }
        if (others.Count == 1)
        {
            onPicked(others[0]);
            return;
        }
        var menu = new PopupPanel { Name = "TradingPostTargetMenu" };
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        menu.AddChild(vbox);
        vbox.AddChild(new Label { Text = "Trade with whom?" });
        foreach (Player other in others)
        {
            Player captured = other;
            AddMenuButton(vbox, TradeSynchronizer.NameOf(captured), () => onPicked(captured));
        }
        AddMenuButton(vbox, "Never mind", CloseMenu);
        Root?.AddChild(menu);
        _openMenu = menu;
        menu.PopupCentered();
    }

    private static void PickGoldAmount(Player target)
    {
        TradeSynchronizer? sync = TradeSynchronizer.Instance;
        if (sync == null)
        {
            return;
        }
        int max = RunLocalGold();
        if (max <= 0)
        {
            Notify("You have no gold to give.");
            return;
        }

        var menu = new PopupPanel { Name = "TradingPostGoldMenu" };
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        vbox.CustomMinimumSize = new Vector2(360, 0);
        menu.AddChild(vbox);

        var label = new Label { Text = $"Give {TradeSynchronizer.NameOf(target)} how much gold?" };
        vbox.AddChild(label);

        var amountLabel = new Label { Text = "0" };
        var slider = new HSlider
        {
            MinValue = 0,
            MaxValue = max,
            Step = 5,
            Value = Math.Min(50, max)
        };
        amountLabel.Text = ((int)slider.Value).ToString();
        slider.ValueChanged += v => amountLabel.Text = ((int)v).ToString();
        vbox.AddChild(slider);
        vbox.AddChild(amountLabel);

        var confirm = new Button { Text = "Send it" };
        confirm.Pressed += () =>
        {
            int amount = (int)slider.Value;
            CloseMenu();
            if (amount > 0)
            {
                TaskHelper.RunSafely(sync.GiftGoldLocal(target, amount));
            }
        };
        vbox.AddChild(confirm);

        var cancel = new Button { Text = "Never mind" };
        cancel.Pressed += CloseMenu;
        vbox.AddChild(cancel);

        Root?.AddChild(menu);
        _openMenu = menu;
        menu.PopupCentered();
    }

    private static int RunLocalGold()
    {
        var state = MegaCrit.Sts2.Core.Runs.RunManager.Instance.State;
        if (state == null)
        {
            return 0;
        }
        return MegaCrit.Sts2.Core.Context.LocalContext.GetMe(state)?.Gold ?? 0;
    }
}
