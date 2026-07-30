using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;

namespace CharacterCursors;

/// <summary>
/// Compact in-run cursor color control (not BaseLib settings).
/// Collapsed chip bottom-left; expand for color picker + reset to character color.
/// </summary>
public partial class CursorColorHud : Control
{
    public static CursorColorHud? Instance { get; private set; }

    private bool _expanded;
    private Button? _chip;
    private PanelContainer? _panel;
    private ColorPickerButton? _picker;
    private Button? _characterBtn;

    public static void Ensure()
    {
        if (Instance != null && GodotObject.IsInstanceValid(Instance))
            return;

        SceneTree? tree = Engine.GetMainLoop() as SceneTree;
        Node? root = tree?.Root;
        if (root == null)
            return;

        var layer = new CanvasLayer
        {
            Name = "CharacterCursorsHudLayer",
            Layer = 115,
        };
        root.AddChild(layer);

        var hud = new CursorColorHud
        {
            Name = "CursorColorHud",
            MouseFilter = MouseFilterEnum.Stop,
        };
        hud.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomLeft);
        hud.GrowHorizontal = GrowDirection.End;
        hud.GrowVertical = GrowDirection.Begin;
        hud.OffsetLeft = 16;
        hud.OffsetBottom = -40;
        hud.OffsetRight = hud.OffsetLeft + 48;
        hud.OffsetTop = hud.OffsetBottom - 48;
        layer.AddChild(hud);
        hud.Build();
        hud.SetProcess(true);
        Instance = hud;
        MainFile.Logger.Info("In-run cursor color HUD ready (chip bottom-left).");
    }

    public static void Teardown()
    {
        if (Instance != null && GodotObject.IsInstanceValid(Instance))
        {
            Node? layer = Instance.GetParent();
            Instance.QueueFree();
            if (layer != null && layer.Name == "CharacterCursorsHudLayer"
                && GodotObject.IsInstanceValid(layer))
                layer.QueueFree();
        }

        Instance = null;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        bool show = ShouldShow();
        if (Visible != show)
            Visible = show;
        if (show)
            CursorColorSync.EnsureHandlers();
    }

    private static bool ShouldShow()
    {
        if (!CursorConfig.EnableTint)
            return false;
        try
        {
            return RunManager.Instance?.IsInProgress == true
                   && NRun.Instance != null
                   && GodotObject.IsInstanceValid(NRun.Instance);
        }
        catch
        {
            return false;
        }
    }

    private void Build()
    {
        _chip = new Button
        {
            Text = "🖱",
            TooltipText = "Cursor color (in-run). Click to open picker.",
            FocusMode = FocusModeEnum.None,
            CustomMinimumSize = new Vector2(48, 48),
            MouseDefaultCursorShape = CursorShape.PointingHand,
        };
        StyleChip(_chip);
        _chip.Pressed += () => SetExpanded(!_expanded);
        _chip.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomLeft);
        _chip.OffsetRight = 48;
        _chip.OffsetTop = -48;
        _chip.OffsetBottom = 0;
        AddChild(_chip);
        RefreshChipColor();

        _panel = new PanelContainer
        {
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop,
        };
        _panel.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomLeft);
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.07f, 0.06f, 0.08f, 0.94f),
            BorderColor = new Color(0.55f, 0.7f, 0.95f),
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
        });
        AddChild(_panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        _panel.AddChild(vbox);

        var title = new Label
        {
            Text = "Cursor color",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.93f, 1f));
        vbox.AddChild(title);

        _picker = new ColorPickerButton
        {
            CustomMinimumSize = new Vector2(180, 36),
            Color = CursorConfig.UseCustomColor
                ? CursorConfig.CustomColor
                : (CursorTint.TryGetLocalPrimaryColor() ?? Colors.White),
            EditAlpha = false,
            TooltipText = "Pick a cursor tint for this run (and saves to settings)",
            FocusMode = FocusModeEnum.None,
        };
        _picker.ColorChanged += OnColorPicked;
        vbox.AddChild(_picker);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(row);

        _characterBtn = new Button
        {
            Text = "Character color",
            TooltipText = "Reset to your character NameColor",
            FocusMode = FocusModeEnum.None,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        StyleChip(_characterBtn);
        _characterBtn.Pressed += OnResetCharacter;
        row.AddChild(_characterBtn);

        var close = new Button
        {
            Text = "Close",
            FocusMode = FocusModeEnum.None,
            CustomMinimumSize = new Vector2(64, 0),
        };
        StyleChip(close);
        close.Pressed += () => SetExpanded(false);
        row.AddChild(close);

        CursorConfig.SettingsChanged += OnSettingsChanged;
        SetExpanded(false);
        Visible = false;
    }

    private void OnSettingsChanged()
    {
        if (_picker != null)
        {
            _picker.Color = CursorConfig.UseCustomColor
                ? CursorConfig.CustomColor
                : (CursorTint.TryGetLocalPrimaryColor() ?? Colors.White);
        }

        RefreshChipColor();
    }

    private void OnColorPicked(Color c)
    {
        CursorColorSync.SetLocalAndBroadcast(c, useCustom: true);
        RefreshChipColor();
    }

    private void OnResetCharacter()
    {
        Color c = CursorTint.TryGetLocalPrimaryColor() ?? Colors.White;
        if (_picker != null)
            _picker.Color = c;
        CursorColorSync.SetLocalAndBroadcast(c, useCustom: false);
        RefreshChipColor();
    }

    private void RefreshChipColor()
    {
        if (_chip == null)
            return;
        Color c = CursorTint.ResolveLocalTintColor() ?? Colors.White;
        _chip.Modulate = new Color(
            Mathf.Clamp(c.R * 1.1f + 0.15f, 0.3f, 1f),
            Mathf.Clamp(c.G * 1.1f + 0.15f, 0.3f, 1f),
            Mathf.Clamp(c.B * 1.1f + 0.15f, 0.3f, 1f));
    }

    private void SetExpanded(bool expanded)
    {
        _expanded = expanded;
        if (_panel != null)
            _panel.Visible = expanded;
        if (_chip != null)
            _chip.Visible = !expanded;

        if (expanded)
        {
            OffsetRight = OffsetLeft + 240;
            OffsetTop = OffsetBottom - 160;
            if (_panel != null)
            {
                _panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                _panel.OffsetLeft = 0;
                _panel.OffsetRight = 0;
                _panel.OffsetTop = 0;
                _panel.OffsetBottom = 0;
            }
        }
        else
        {
            OffsetRight = OffsetLeft + 48;
            OffsetTop = OffsetBottom - 48;
        }
    }

    private static void StyleChip(Button btn)
    {
        StyleBoxFlat Make(Color bg) => new()
        {
            BgColor = bg,
            BorderColor = new Color(0.55f, 0.7f, 0.95f),
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
        };
        btn.AddThemeStyleboxOverride("normal", Make(new Color(0.1f, 0.1f, 0.14f, 0.95f)));
        btn.AddThemeStyleboxOverride("hover", Make(new Color(0.16f, 0.16f, 0.22f, 0.98f)));
        btn.AddThemeStyleboxOverride("pressed", Make(new Color(0.06f, 0.06f, 0.1f, 0.98f)));
        btn.AddThemeColorOverride("font_color", new Color(0.92f, 0.94f, 1f));
    }
}
