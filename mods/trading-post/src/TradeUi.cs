using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace TradingPost;

/// <summary>
/// Game-native UI for the Trading Post. Consent prompts and notifications use the
/// game's own NGenericPopup scene; the trade menu is a custom overlay dressed in
/// styles (fonts, panel, buttons) stolen at runtime from that same popup, so it
/// matches the game's look without shipping any assets.
/// </summary>
public static class TradeUi
{
    private static CanvasLayer? _openMenu;

    private static bool _stylesLoaded;

    private static StyleBox? _panelStyle;

    private static Font? _titleFont, _bodyFont, _buttonFont;

    private static int _titleSize = 34, _bodySize = 22, _buttonSize = 24;

    private static Color _titleColor = Colors.White, _bodyColor = new(0.85f, 0.82f, 0.74f);

    private static StyleBox? _btnNormal, _btnHover, _btnPressed;

    private static Node? Root => (Engine.GetMainLoop() as SceneTree)?.Root;

    // ------------------------------------------------------------ native popups

    /// <summary>Fire-and-forget notification using the game's own popup scene.</summary>
    public static void Notify(string text)
    {
        TaskHelper.RunSafely(ShowGamePopup(text, confirmOnly: true, null));
    }

    /// <summary>Accept/decline prompt using the game's own popup scene.</summary>
    public static void Confirm(string text, Action<bool> onAnswer)
    {
        TaskHelper.RunSafely(ShowGamePopup(text, confirmOnly: false, onAnswer));
    }

    private static async Task ShowGamePopup(string body, bool confirmOnly, Action<bool>? onAnswer)
    {
        NGenericPopup? popup = NGenericPopup.Create();
        NModalContainer? container = NModalContainer.Instance;
        if (popup == null || container == null)
        {
            onAnswer?.Invoke(false);
            return;
        }
        // The modal stack fits one screen at a time; wait politely for it to free up.
        for (int i = 0; i < 400 && container.OpenModal != null; i++)
        {
            await Task.Delay(300);
        }
        container.Add(popup);
        bool answer = await popup.WaitForConfirmation(
            Loc.Dynamic(body),
            Loc.Get("TITLE"),
            confirmOnly ? null : Loc.Get("DECLINE"),
            Loc.Get(confirmOnly ? "OK" : "ACCEPT"));
        onAnswer?.Invoke(answer);
    }

    // ------------------------------------------------------------ style stealing

    /// <summary>
    /// Instantiates the game's generic popup off-screen once and lifts its resolved
    /// theme pieces so our custom screens render with the game's exact styling.
    /// </summary>
    private static void EnsureStyles()
    {
        if (_stylesLoaded)
        {
            return;
        }
        try
        {
            NGenericPopup? donor = NGenericPopup.Create();
            if (donor == null || Root == null)
            {
                return;
            }
            donor.Visible = false;
            Root.AddChild(donor);
            // _verticalPopup is only assigned lazily inside WaitForConfirmation; fetch the node directly.
            NVerticalPopup vp = donor.GetNode<NVerticalPopup>("VerticalPopup");
            var title = vp.TitleLabel;
            var body = vp.BodyLabel;
            _titleFont = title.GetThemeFont("normal_font");
            _titleSize = title.GetThemeFontSize("normal_font_size");
            _titleColor = title.GetThemeColor("default_color");
            _bodyFont = body.GetThemeFont("font");
            _bodySize = body.GetThemeFontSize("font_size");
            _bodyColor = body.GetThemeColor("font_color");
            _buttonFont = _bodyFont;
            _buttonSize = _bodySize + 2;
            BuildButtonStyles();
            _panelStyle = FindPanelStyle(donor);
            donor.QueueFree();
            _stylesLoaded = true;
            MainFile.Logger.Info("Trade UI adopted the game's popup styling.");
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Style stealing failed, using fallback look: {e.Message}");
            BuildButtonStyles();
            _stylesLoaded = true;
        }
    }

    /// <summary>Dark panels with gold trim, matching the game's dialog language.</summary>
    private static void BuildButtonStyles()
    {
        StyleBoxFlat Make(Color bg, Color border)
        {
            var style = new StyleBoxFlat
            {
                BgColor = bg,
                BorderColor = border,
                BorderWidthBottom = 2, BorderWidthTop = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
                CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
                CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
                ContentMarginLeft = 24, ContentMarginRight = 24,
                ContentMarginTop = 10, ContentMarginBottom = 10,
            };
            return style;
        }
        _btnNormal = Make(new Color(0.086f, 0.078f, 0.11f, 0.92f), new Color(0.54f, 0.45f, 0.20f));
        _btnHover = Make(new Color(0.161f, 0.14f, 0.19f, 0.95f), new Color(0.83f, 0.71f, 0.35f));
        _btnPressed = Make(new Color(0.055f, 0.047f, 0.07f, 0.95f), new Color(0.42f, 0.35f, 0.16f));
    }

    private static StyleBox? FindPanelStyle(Node node)
    {
        if (node is PanelContainer pc)
        {
            return pc.GetThemeStylebox("panel");
        }
        if (node is Panel p)
        {
            return p.GetThemeStylebox("panel");
        }
        foreach (Node child in node.GetChildren())
        {
            StyleBox? found = FindPanelStyle(child);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    private static StyleBox PanelStyleOrFallback()
    {
        if (_panelStyle != null)
        {
            return _panelStyle;
        }
        return new StyleBoxFlat
        {
            BgColor = new Color(0.09f, 0.08f, 0.11f, 0.98f),
            BorderColor = new Color(0.78f, 0.66f, 0.29f),
            BorderWidthBottom = 3, BorderWidthTop = 3, BorderWidthLeft = 3, BorderWidthRight = 3,
            CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
        };
    }

    // ------------------------------------------------------------ widget factory

    private static Label MakeLabel(string text, bool isTitle, bool dim = false)
    {
        var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
        Font? font = isTitle ? _titleFont : _bodyFont;
        if (font != null)
        {
            label.AddThemeFontOverride("font", font);
        }
        label.AddThemeFontSizeOverride("font_size", isTitle ? _titleSize + 8 : _bodySize);
        Color color = isTitle ? _titleColor : _bodyColor;
        if (dim)
        {
            color.A = 0.65f;
        }
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static Button MakeButton(string text, Action onPressed, float minWidth = 620, float minHeight = 64)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(minWidth, minHeight) };
        if (_buttonFont != null)
        {
            button.AddThemeFontOverride("font", _buttonFont);
        }
        button.AddThemeFontSizeOverride("font_size", _buttonSize);
        if (_btnNormal != null)
        {
            button.AddThemeStyleboxOverride("normal", _btnNormal);
        }
        if (_btnHover != null)
        {
            button.AddThemeStyleboxOverride("hover", _btnHover);
        }
        if (_btnPressed != null)
        {
            button.AddThemeStyleboxOverride("pressed", _btnPressed);
        }
        button.Pressed += () => onPressed();
        return button;
    }

    /// <summary>Dimmed full-screen overlay with a centered game-styled panel.</summary>
    private static VBoxContainer OpenShell(string title, string? subtitle)
    {
        CloseMenu();
        EnsureStyles();

        var layer = new CanvasLayer { Name = "TradingPostOverlay", Layer = 80 };
        var blocker = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
        blocker.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        layer.AddChild(blocker);

        var dim = new ColorRect { Color = new Color(0f, 0f, 0f, 0.62f), MouseFilter = Control.MouseFilterEnum.Stop };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        blocker.AddChild(dim);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        blocker.AddChild(center);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", PanelStyleOrFallback());
        center.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 48);
        margin.AddThemeConstantOverride("margin_right", 48);
        margin.AddThemeConstantOverride("margin_top", 36);
        margin.AddThemeConstantOverride("margin_bottom", 36);
        panel.AddChild(margin);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 18);
        margin.AddChild(content);

        content.AddChild(MakeLabel(title, isTitle: true));
        if (subtitle != null)
        {
            content.AddChild(MakeLabel(subtitle, isTitle: false, dim: true));
        }

        Root?.AddChild(layer);
        _openMenu = layer;
        return content;
    }

    private static void CloseMenu()
    {
        if (_openMenu != null && GodotObject.IsInstanceValid(_openMenu))
        {
            _openMenu.QueueFree();
        }
        _openMenu = null;
    }

    // ------------------------------------------------------------ trade flow

    /// <summary>Adds the Trade button to the merchant room screen.</summary>
    public static void AddTradeButton(Control shopScreen)
    {
        EnsureStyles();
        Button button = MakeButton("Trade", OpenMenu, minWidth: 240, minHeight: 64);
        button.Name = "TradingPostButton";
        shopScreen.AddChild(button);
        button.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomLeft);
        button.OffsetLeft = 36;
        button.OffsetRight = 276;
        button.OffsetTop = -132;
        button.OffsetBottom = -68;
    }

    private static void OpenMenu()
    {
        if (TradeSynchronizer.Instance == null)
        {
            return;
        }
        VBoxContainer content = OpenShell("TRADING POST",
            "Gold flows freely at the shop. Cards trade at campfires.");
        content.AddChild(MakeButton("Give Gold  —  a gift, no strings attached", () =>
            PickTarget("Send gold to whom?", PickGoldAmount)));
        content.AddChild(MakeButton("Never Mind", CloseMenu, minWidth: 300, minHeight: 52));
    }

    /// <summary>
    /// Campfire card trade, opened by <see cref="TradeRestSiteOption" />. Resolves the outcome
    /// task with true only when a trade actually completed — that consumes the campfire action.
    /// </summary>
    public static void OpenCampfireMenu(TaskCompletionSource<bool> outcome)
    {
        TradeSynchronizer? sync = TradeSynchronizer.Instance;
        if (sync == null)
        {
            outcome.TrySetResult(false);
            return;
        }
        PickTarget("Give a card to whom? This spends your time at the campfire.", target =>
            TaskHelper.RunSafely(RunAndResolve(() => sync.GiftCardLocal(target), outcome)),
            () => outcome.TrySetResult(false));
    }

    private static async Task RunAndResolve(Func<Task<bool>> flow, TaskCompletionSource<bool> outcome)
    {
        bool result;
        try
        {
            result = await flow();
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Campfire trade flow failed: {e}");
            result = false;
        }
        outcome.TrySetResult(result);
    }

    /// <summary>Shows the player chooser; reports cancellation so campfire flows can refund.</summary>
    private static void PickTarget(string prompt, Action<Player> onPicked, Action? onCancelled = null)
    {
        TradeSynchronizer? sync = TradeSynchronizer.Instance;
        if (sync == null)
        {
            onCancelled?.Invoke();
            return;
        }
        IReadOnlyList<Player> others = sync.OtherPlayers;
        if (others.Count == 0)
        {
            CloseMenu();
            Notify("No one to trade with — you're climbing solo.");
            onCancelled?.Invoke();
            return;
        }
        VBoxContainer content = OpenShell("TRADING POST", prompt);
        foreach (Player other in others)
        {
            Player captured = other;
            content.AddChild(MakeButton(TradeSynchronizer.NameOf(captured), () =>
            {
                CloseMenu();
                onPicked(captured);
            }, minWidth: 460));
        }
        content.AddChild(MakeButton("Never Mind", () =>
        {
            CloseMenu();
            onCancelled?.Invoke();
        }, minWidth: 300, minHeight: 52));
    }

    private static void PickGoldAmount(Player target)
    {
        TradeSynchronizer? sync = TradeSynchronizer.Instance;
        if (sync == null)
        {
            return;
        }
        int max = LocalGold();
        if (max <= 0)
        {
            Notify("You have no gold to give.");
            return;
        }

        VBoxContainer content = OpenShell("TRADING POST",
            $"How much gold do you send to {TradeSynchronizer.NameOf(target)}?");

        int start = Math.Min(50, max);
        var amountEdit = new LineEdit
        {
            Text = start.ToString(),
            Alignment = HorizontalAlignment.Center,
            CustomMinimumSize = new Vector2(200, 56),
        };
        if (_titleFont != null)
        {
            amountEdit.AddThemeFontOverride("font", _titleFont);
        }
        amountEdit.AddThemeFontSizeOverride("font_size", _titleSize);
        var slider = new HSlider
        {
            MinValue = 0,
            MaxValue = max,
            Step = 1,
            Value = start,
            CustomMinimumSize = new Vector2(560, 32),
        };
        slider.ValueChanged += v => amountEdit.Text = ((int)v).ToString();
        amountEdit.TextChanged += t =>
        {
            if (int.TryParse(t, out int typed))
            {
                slider.SetValueNoSignal(Math.Clamp(typed, 0, max));
            }
        };
        var amountRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        amountRow.AddThemeConstantOverride("separation", 16);
        amountRow.AddChild(amountEdit);
        amountRow.AddChild(MakeLabel("gold", isTitle: false, dim: true));
        content.AddChild(amountRow);
        content.AddChild(slider);

        var presets = new HBoxContainer();
        presets.AddThemeConstantOverride("separation", 12);
        presets.Alignment = BoxContainer.AlignmentMode.Center;
        foreach (int pct in new[] { 10, 25, 50, 100 })
        {
            int amount = pct == 100 ? max : max * pct / 100;
            presets.AddChild(MakeButton(pct == 100 ? "ALL" : $"{pct}%", () => slider.Value = amount,
                minWidth: 120, minHeight: 48));
        }
        content.AddChild(presets);

        content.AddChild(MakeButton("Send It", () =>
        {
            int amount = (int)slider.Value;
            CloseMenu();
            if (amount > 0)
            {
                TaskHelper.RunSafely(sync.GiftGoldLocal(target, amount));
            }
        }));
        content.AddChild(MakeButton("Never Mind", CloseMenu, minWidth: 300, minHeight: 52));
    }

    private static int LocalGold()
    {
        var state = MegaCrit.Sts2.Core.Runs.RunManager.Instance.State;
        if (state == null)
        {
            return 0;
        }
        return MegaCrit.Sts2.Core.Context.LocalContext.GetMe(state)?.Gold ?? 0;
    }
}
