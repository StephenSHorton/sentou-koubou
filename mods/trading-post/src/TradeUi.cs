using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace TradingPost;

/// <summary>
/// Game-native UI for the Trading Post. Consent prompts use <see cref="NGenericPopup"/>;
/// trade menus are custom overlays with STS2 fonts plus mod PNG art (banner + row icons)
/// so they read as painted STS2 chrome rather than stock Godot widgets.
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

    public static void Notify(string text)
    {
        TaskHelper.RunSafely(ShowGamePopup(text, confirmOnly: true, null));
    }

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
                BuildButtonStyles();
                _stylesLoaded = true;
                return;
            }
            donor.Visible = false;
            Root.AddChild(donor);
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

    private static void BuildButtonStyles()
    {
        // Prefer painted STS2 button plate; fall back to flat gold-trim chrome.
        StyleBoxTexture? painted = TradeAssets.MakeNineSlice(TradeAssets.BtnPlate, margin: 36f, content: 20f);
        if (painted != null)
        {
            _btnNormal = painted;
            // Same plate for hover/pressed — brightness is handled by the engine's
            // button draw modes; StyleBoxTexture has no modulate in this Godot API.
            _btnHover = painted;
            _btnPressed = painted;
            return;
        }

        StyleBoxFlat Make(Color bg, Color border)
        {
            return new StyleBoxFlat
            {
                BgColor = bg,
                BorderColor = border,
                BorderWidthBottom = 2, BorderWidthTop = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
                CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
                CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
                ContentMarginLeft = 18, ContentMarginRight = 18,
                ContentMarginTop = 10, ContentMarginBottom = 10,
            };
        }
        _btnNormal = Make(new Color(0.086f, 0.078f, 0.11f, 0.94f), new Color(0.62f, 0.50f, 0.22f));
        _btnHover = Make(new Color(0.16f, 0.13f, 0.19f, 0.96f), new Color(0.90f, 0.76f, 0.36f));
        _btnPressed = Make(new Color(0.05f, 0.04f, 0.07f, 0.96f), new Color(0.42f, 0.34f, 0.15f));
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
        // Painted dialog panel first, then stolen game stylebox, then flat fallback.
        StyleBoxTexture? painted = TradeAssets.MakeNineSlice(TradeAssets.MenuPanel, margin: 48f, content: 28f);
        if (painted != null)
        {
            return painted;
        }
        if (_panelStyle != null)
        {
            return _panelStyle;
        }
        return new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.07f, 0.10f, 0.98f),
            BorderColor = new Color(0.78f, 0.66f, 0.29f),
            BorderWidthBottom = 3, BorderWidthTop = 3, BorderWidthLeft = 3, BorderWidthRight = 3,
            CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12,
            CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12,
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
        label.AddThemeFontSizeOverride("font_size", isTitle ? _titleSize + 6 : _bodySize);
        Color color = isTitle ? _titleColor : _bodyColor;
        if (dim)
        {
            color.A = 0.7f;
        }
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static void ApplyButtonChrome(Button button)
    {
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
    }

    private static Button MakeButton(string text, Action onPressed, float minWidth = 620, float minHeight = 64)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(minWidth, minHeight) };
        ApplyButtonChrome(button);
        button.Pressed += () => onPressed();
        return button;
    }

    /// <summary>STS2-style row: large painted icon + label on a plate button.</summary>
    private static Button MakeIconButton(string text, Texture2D? icon, Action onPressed,
        float minWidth = 640, float minHeight = 88)
    {
        var button = new Button
        {
            CustomMinimumSize = new Vector2(minWidth, minHeight),
            Text = "", // content is custom
        };
        ApplyButtonChrome(button);

        var row = new HBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Begin,
        };
        row.AddThemeConstantOverride("separation", 20);
        row.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        row.OffsetLeft = 22;
        row.OffsetRight = -22;
        button.AddChild(row);

        if (icon != null)
        {
            var tex = new TextureRect
            {
                Texture = icon,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(72, 72),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            row.AddChild(tex);
        }

        var label = new Label
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        if (_buttonFont != null)
        {
            label.AddThemeFontOverride("font", _buttonFont);
        }
        label.AddThemeFontSizeOverride("font_size", _buttonSize + 2);
        // Cream text reads better on painted plates
        label.AddThemeColorOverride("font_color", new Color(0.95f, 0.91f, 0.78f));
        row.AddChild(label);

        button.Pressed += () => onPressed();
        return button;
    }

    private static Control? MakeBanner()
    {
        Texture2D? tex = TradeAssets.MenuBanner ?? TradeAssets.OptionTrade ?? TradeAssets.IconTrade;
        if (tex == null)
        {
            return null;
        }
        var wrap = new CenterContainer();
        var image = new TextureRect
        {
            Texture = tex,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            // Full-bleed rest-option style art, large
            CustomMinimumSize = new Vector2(560, 320),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        wrap.AddChild(image);
        return wrap;
    }

    /// <summary>Dimmed full-screen overlay with a centered game-styled panel + optional banner art.</summary>
    private static VBoxContainer OpenShell(string title, string? subtitle, bool showBanner = true)
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
        margin.AddThemeConstantOverride("margin_left", 44);
        margin.AddThemeConstantOverride("margin_right", 44);
        margin.AddThemeConstantOverride("margin_top", 32);
        margin.AddThemeConstantOverride("margin_bottom", 32);
        panel.AddChild(margin);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 16);
        margin.AddChild(content);

        if (showBanner)
        {
            Control? banner = MakeBanner();
            if (banner != null)
            {
                content.AddChild(banner);
            }
        }

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

    /// <summary>Adds a painted Trade button to the merchant room screen.</summary>
    public static void AddTradeButton(Control shopScreen)
    {
        EnsureStyles();
        Button button = MakeIconButton("Trade", TradeAssets.IconTrade ?? TradeAssets.OptionTrade, OpenMenu,
            minWidth: 280, minHeight: 72);
        button.Name = "TradingPostButton";
        shopScreen.AddChild(button);
        button.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomLeft);
        button.OffsetLeft = 36;
        button.OffsetRight = 316;
        button.OffsetTop = -140;
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
        content.AddChild(MakeIconButton("Give Gold — a gift, no strings attached",
            TradeAssets.IconGold, () => PickTarget("Send gold to whom?", PickGoldAmount)));
        content.AddChild(MakeButton("Never Mind", CloseMenu, minWidth: 300, minHeight: 52));
    }

    /// <summary>
    /// Campfire card trade. Resolves true only when a trade completed (consumes rest action).
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
            () => outcome.TrySetResult(false),
            playerIcon: TradeAssets.IconCard ?? TradeAssets.IconTrade);
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

    private static void PickTarget(string prompt, Action<Player> onPicked, Action? onCancelled = null,
        Texture2D? playerIcon = null)
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
        Texture2D? icon = playerIcon ?? TradeAssets.IconTrade ?? TradeAssets.IconCard;
        VBoxContainer content = OpenShell("TRADING POST", prompt);
        foreach (Player other in others)
        {
            Player captured = other;
            content.AddChild(MakeIconButton(TradeSynchronizer.NameOf(captured), icon, () =>
                {
                    CloseMenu();
                    onPicked(captured);
                }, minWidth: 480));
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

        // Gold icon row
        if (TradeAssets.IconGold != null)
        {
            var goldIcon = new TextureRect
            {
                Texture = TradeAssets.IconGold,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(72, 72),
            };
            var iconWrap = new CenterContainer();
            iconWrap.AddChild(goldIcon);
            content.AddChild(iconWrap);
        }

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

        content.AddChild(MakeIconButton("Send It", TradeAssets.IconGold, () =>
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
