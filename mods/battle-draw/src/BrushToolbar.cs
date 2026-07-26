using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace BattleDraw;

public enum DrawTool
{
    None = 0,
    Brush = 1,
    Eraser = 2,
}

/// <summary>
/// One global collapsible draw menu for map + combat.
/// Collapsed = pen tab. Expanded = tools panel (flat dark chrome — no generated plate art).
/// Color picker stays a normal ColorPickerButton; size lives in the same expanded panel.
/// </summary>
public partial class BrushToolbar : Control
{
    public static BrushToolbar? Instance { get; private set; }

    public static BrushToolbar? CombatInstance => Instance;
    public static BrushToolbar? MapInstance => Instance;

    public DrawTool ActiveTool { get; private set; } = DrawTool.None;

    private bool _expanded;
    private bool _inCombatContext;
    private Button? _tabButton;
    private PanelContainer? _panel;
    private Button? _brushBtn;
    private Button? _eraserBtn;
    private Button? _clearBtn;
    private Button? _hidePeersBtn;
    private Control? _combatToolsRow;
    private ColorPickerButton? _colorPicker;
    private HSlider? _sizeSlider;
    private Label? _sizeValueLabel;

    public static void EnsureGlobal()
    {
        if (Instance != null && GodotObject.IsInstanceValid(Instance))
            return;

        SceneTree? tree = Engine.GetMainLoop() as SceneTree;
        Node? root = tree?.Root;
        if (root == null)
            return;

        var layer = new CanvasLayer
        {
            Name = "BattleDrawGlobalUi",
            Layer = 120,
        };
        root.AddChild(layer);

        var bar = new BrushToolbar
        {
            Name = "BattleDrawToolbar",
            MouseFilter = MouseFilterEnum.Stop,
            ZIndex = 30,
        };
        bar.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
        bar.GrowHorizontal = GrowDirection.Begin;
        bar.GrowVertical = GrowDirection.Begin;
        bar.OffsetRight = -16;
        bar.OffsetBottom = -40;
        bar.OffsetLeft = bar.OffsetRight - 56;
        bar.OffsetTop = bar.OffsetBottom - 56;
        layer.AddChild(bar);
        bar.BuildUi();
        bar.SetProcess(true);
        BrushConfig.SettingsChanged += bar.OnConfigChanged;
        Instance = bar;
        MainFile.Logger.Info("Battle Draw collapsible toolbar ready (map + combat).");
    }

    public static void AttachCombat(CanvasLayer _)
    {
        EnsureGlobal();
        Instance?.SetCombatContext(true);
    }

    public static void AttachMap(Node _)
    {
        EnsureGlobal();
        Instance?.SetCombatContext(false);
    }

    public static void DetachCombat() => Instance?.SetCombatContext(false);

    public static void DetachMap()
    {
        // Global bar stays; visibility is polled.
    }

    public static void Detach()
    {
        if (Instance != null && GodotObject.IsInstanceValid(Instance))
        {
            BrushConfig.SettingsChanged -= Instance.OnConfigChanged;
            Node? layer = Instance.GetParent();
            Instance.QueueFree();
            if (layer != null && layer.Name == "BattleDrawGlobalUi" && GodotObject.IsInstanceValid(layer))
                layer.QueueFree();
        }
        Instance = null;
    }

    public static void SyncAllSizeSliders() => Instance?.SyncSizeSlider();

    public static bool HitsPointer()
    {
        BrushToolbar? bar = Instance;
        if (bar == null || !GodotObject.IsInstanceValid(bar) || !bar.Visible)
            return false;
        if (bar._tabButton != null && RectHits(bar._tabButton, 6f))
            return true;
        if (bar._expanded && bar._panel is { Visible: true } panel && RectHits(panel, 8f))
            return true;
        if (bar._expanded && bar._colorPicker != null && IsOverColorPickerPopup(bar))
            return true;
        return false;
    }

    private static bool RectHits(Control c, float pad)
    {
        if (!GodotObject.IsInstanceValid(c) || !c.IsVisibleInTree())
            return false;
        Rect2 rect = c.GetGlobalRect();
        if (rect.Size.X < 2f || rect.Size.Y < 2f)
            return false;
        // Guard near-fullscreen false positives.
        Vector2 vp = c.GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920, 1080);
        if (rect.Size.X * rect.Size.Y > vp.X * vp.Y * 0.35f)
            return false;
        return rect.Grow(pad).HasPoint(c.GetGlobalMousePosition());
    }

    private static bool IsOverColorPickerPopup(BrushToolbar bar)
    {
        SceneTree? tree = bar.GetTree();
        if (tree?.Root == null)
            return false;
        return PopupContainsColorPickerUnderMouse(tree.Root);
    }

    private static bool PopupContainsColorPickerUnderMouse(Node node)
    {
        if (node is Popup { Visible: true } popup)
        {
            bool hasPicker = false;
            foreach (Node child in popup.GetChildren())
            {
                if (child is ColorPicker || HasDescendantColorPicker(child))
                {
                    hasPicker = true;
                    break;
                }
            }

            if (hasPicker)
            {
                Vector2 mouse = popup.GetMousePosition();
                if (new Rect2(Vector2.Zero, popup.Size).Grow(8f).HasPoint(mouse))
                    return true;
            }
        }

        foreach (Node child in node.GetChildren())
        {
            if (PopupContainsColorPickerUnderMouse(child))
                return true;
        }

        return false;
    }

    private static bool HasDescendantColorPicker(Node node)
    {
        if (node is ColorPicker)
            return true;
        foreach (Node child in node.GetChildren())
        {
            if (HasDescendantColorPicker(child))
                return true;
        }

        return false;
    }

    public override void _ExitTree()
    {
        BrushConfig.SettingsChanged -= OnConfigChanged;
        if (Instance == this)
            Instance = null;
        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        bool show = ShouldShow();
        if (Visible != show)
            Visible = show;

        bool combat = IsCombatActive();
        if (combat != _inCombatContext)
            SetCombatContext(combat);
    }

    private static bool IsCombatActive()
    {
        try
        {
            var combat = NCombatRoom.Instance;
            return combat != null && GodotObject.IsInstanceValid(combat)
                   && combat.IsInsideTree() && combat.IsVisibleInTree();
        }
        catch
        {
            return false;
        }
    }

    private static bool ShouldShow()
    {
        try
        {
            if (IsCombatActive())
                return true;
            var map = NMapScreen.Instance;
            return map != null && GodotObject.IsInstanceValid(map)
                   && map.IsVisibleInTree() && map.Visible;
        }
        catch
        {
            return false;
        }
    }

    public void SetCombatContext(bool combat)
    {
        _inCombatContext = combat;
        if (_combatToolsRow != null)
            _combatToolsRow.Visible = combat;
        if (_hidePeersBtn != null)
            _hidePeersBtn.Visible = combat;

        // Sit above the hand strip in combat.
        OffsetBottom = combat ? -96 : -36;
        if (_expanded)
            ApplyExpandedOffsets();
        else
            ApplyCollapsedOffsets();

        if (!combat && ActiveTool != DrawTool.None)
        {
            ActiveTool = DrawTool.None;
            RefreshToolVisuals();
            DrawCanvas.Instance?.OnToolChanged(DrawTool.None);
        }

        RefreshHidePeersButton();
        if (_tabButton != null)
        {
            _tabButton.TooltipText = combat
                ? "Battle Draw tools"
                : "Map pen color & size";
        }
    }

    private void OnConfigChanged()
    {
        SyncSizeSlider();
        SyncColorPicker();
    }

    private void BuildUi()
    {
        // --- Collapsed tab ---
        _tabButton = MakeDarkButton("✎", "Open draw tools");
        _tabButton.CustomMinimumSize = new Vector2(56, 56);
        _tabButton.Pressed += () => SetExpanded(!_expanded);
        _tabButton.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
        _tabButton.OffsetLeft = -56;
        _tabButton.OffsetTop = -56;
        _tabButton.OffsetRight = 0;
        _tabButton.OffsetBottom = 0;
        AddChild(_tabButton);

        // --- Expanded panel (flat dark chrome, NOT generated art) ---
        _panel = new PanelContainer
        {
            Name = "ToolPanel",
            MouseFilter = MouseFilterEnum.Stop,
            Visible = false,
        };
        _panel.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
        _panel.AddThemeStyleboxOverride("panel", MakePanelStyle());
        AddChild(_panel);

        var vbox = new VBoxContainer { MouseFilter = MouseFilterEnum.Stop };
        vbox.AddThemeConstantOverride("separation", 10);
        _panel.AddChild(vbox);

        var header = new HBoxContainer();
        vbox.AddChild(header);
        var title = new Label
        {
            Text = "Draw tools",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        title.AddThemeColorOverride("font_color", new Color(0.98f, 0.94f, 0.8f));
        title.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.9f));
        title.AddThemeConstantOverride("shadow_offset_x", 1);
        title.AddThemeConstantOverride("shadow_offset_y", 1);
        header.AddChild(title);
        var collapse = MakeDarkButton("▾", "Collapse");
        collapse.CustomMinimumSize = new Vector2(36, 32);
        collapse.Pressed += () => SetExpanded(false);
        header.AddChild(collapse);

        // Combat tools row
        _combatToolsRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _combatToolsRow.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(_combatToolsRow);

        _brushBtn = MakeDarkButton("Brush", "Brush (B) — LMB when armed; RMB always draws");
        _brushBtn.CustomMinimumSize = new Vector2(88, 40);
        _brushBtn.Pressed += () => SetTool(DrawTool.Brush);
        _combatToolsRow.AddChild(_brushBtn);

        _eraserBtn = MakeDarkButton("Erase", "Eraser (E) — LMB when armed; MMB always erases");
        _eraserBtn.CustomMinimumSize = new Vector2(88, 40);
        _eraserBtn.Pressed += () => SetTool(DrawTool.Eraser);
        _combatToolsRow.AddChild(_eraserBtn);

        _clearBtn = MakeDarkButton("Clear", "Clear all combat doodles");
        _clearBtn.CustomMinimumSize = new Vector2(88, 40);
        _clearBtn.Pressed += () => DrawCanvas.Instance?.ClearAll();
        _combatToolsRow.AddChild(_clearBtn);

        // Color + size (map + combat)
        var colorRow = new HBoxContainer();
        colorRow.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(colorRow);
        var colorLab = new Label { Text = "Color", VerticalAlignment = VerticalAlignment.Center };
        StyleLabel(colorLab);
        colorRow.AddChild(colorLab);
        _colorPicker = new ColorPickerButton
        {
            CustomMinimumSize = new Vector2(140, 36),
            Color = BrushConfig.CurrentColor,
            EditAlpha = true,
            TooltipText = "Ink color",
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop,
        };
        _colorPicker.ColorChanged += c =>
        {
            BrushConfig.SetColor(c);
            DrawCanvas.Instance?.RefreshCursor();
        };
        colorRow.AddChild(_colorPicker);

        var sizeRow = new HBoxContainer();
        sizeRow.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(sizeRow);
        var sizeLab = new Label { Text = "Size", VerticalAlignment = VerticalAlignment.Center };
        StyleLabel(sizeLab);
        sizeRow.AddChild(sizeLab);
        _sizeSlider = new HSlider
        {
            MinValue = 1,
            MaxValue = 24,
            Step = 0.5,
            Value = BrushConfig.ClampedSize,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(160, 28),
            TooltipText = "Brush size  [  ]",
            MouseFilter = MouseFilterEnum.Stop,
        };
        _sizeSlider.ValueChanged += v => BrushConfig.SetSize((float)v);
        sizeRow.AddChild(_sizeSlider);
        _sizeValueLabel = new Label
        {
            Text = $"{BrushConfig.ClampedSize:0.#}",
            CustomMinimumSize = new Vector2(36, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        StyleLabel(_sizeValueLabel);
        sizeRow.AddChild(_sizeValueLabel);

        _hidePeersBtn = MakeDarkButton("Hide others' drawings", "Toggle co-op partners' combat doodles");
        _hidePeersBtn.CustomMinimumSize = new Vector2(0, 36);
        _hidePeersBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _hidePeersBtn.Pressed += ToggleHidePeers;
        vbox.AddChild(_hidePeersBtn);

        var tip = new Label
        {
            Text = "RMB pen · MMB erase · [ ] size",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        tip.AddThemeColorOverride("font_color", new Color(0.8f, 0.78f, 0.65f, 0.95f));
        tip.AddThemeFontSizeOverride("font_size", 13);
        vbox.AddChild(tip);

        RefreshToolVisuals();
        SetExpanded(false);
        SetCombatContext(false);
        Visible = false;
    }

    private static void StyleLabel(Label label)
    {
        label.AddThemeColorOverride("font_color", new Color(0.96f, 0.93f, 0.82f));
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.9f));
        label.AddThemeConstantOverride("shadow_offset_x", 1);
        label.AddThemeConstantOverride("shadow_offset_y", 1);
    }

    private static StyleBoxFlat MakePanelStyle() => new()
    {
        BgColor = new Color(0.07f, 0.06f, 0.08f, 0.94f),
        BorderColor = new Color(0.78f, 0.66f, 0.32f),
        BorderWidthBottom = 2,
        BorderWidthTop = 2,
        BorderWidthLeft = 2,
        BorderWidthRight = 2,
        CornerRadiusBottomLeft = 12,
        CornerRadiusBottomRight = 12,
        CornerRadiusTopLeft = 12,
        CornerRadiusTopRight = 12,
        ContentMarginLeft = 14,
        ContentMarginRight = 14,
        ContentMarginTop = 12,
        ContentMarginBottom = 12,
    };

    private static Button MakeDarkButton(string text, string tip)
    {
        var btn = new Button
        {
            Text = text,
            TooltipText = tip,
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop,
            MouseDefaultCursorShape = CursorShape.PointingHand,
        };
        StyleBoxFlat Make(Color bg, Color border) => new()
        {
            BgColor = bg,
            BorderColor = border,
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
        };
        btn.AddThemeStyleboxOverride("normal",
            Make(new Color(0.1f, 0.09f, 0.11f, 0.96f), new Color(0.72f, 0.6f, 0.32f)));
        btn.AddThemeStyleboxOverride("hover",
            Make(new Color(0.16f, 0.14f, 0.12f, 0.98f), new Color(0.95f, 0.82f, 0.42f)));
        btn.AddThemeStyleboxOverride("pressed",
            Make(new Color(0.06f, 0.05f, 0.07f, 0.98f), new Color(0.45f, 0.38f, 0.2f)));
        btn.AddThemeColorOverride("font_color", new Color(0.98f, 0.95f, 0.85f));
        btn.AddThemeColorOverride("font_hover_color", Colors.White);
        btn.AddThemeColorOverride("font_pressed_color", new Color(0.9f, 0.88f, 0.75f));
        return btn;
    }

    private void ToggleHidePeers()
    {
        DrawCanvas? canvas = DrawCanvas.Instance;
        if (canvas == null)
            return;
        canvas.SetHideRemoteStrokes(!canvas.HideRemoteStrokes);
        RefreshHidePeersButton();
    }

    private void RefreshHidePeersButton()
    {
        if (_hidePeersBtn == null)
            return;
        bool hidden = DrawCanvas.Instance?.HideRemoteStrokes ?? false;
        _hidePeersBtn.Text = hidden ? "Show others' drawings" : "Hide others' drawings";
        _hidePeersBtn.Modulate = hidden ? new Color(1.2f, 1.08f, 0.5f) : Colors.White;
    }

    public void SyncSizeSlider()
    {
        if (_sizeSlider != null)
            _sizeSlider.SetValueNoSignal(BrushConfig.ClampedSize);
        if (_sizeValueLabel != null)
            _sizeValueLabel.Text = $"{BrushConfig.ClampedSize:0.#}";
    }

    public void SyncColorPicker()
    {
        if (_colorPicker != null)
            _colorPicker.Color = BrushConfig.CurrentColor;
    }

    public void SetExpanded(bool expanded)
    {
        _expanded = expanded;
        if (_panel != null)
            _panel.Visible = expanded;
        if (_tabButton != null)
        {
            _tabButton.Visible = !expanded;
            _tabButton.Modulate = expanded ? new Color(1.15f, 1.1f, 0.7f) : Colors.White;
        }

        if (expanded)
            ApplyExpandedOffsets();
        else
            ApplyCollapsedOffsets();

        if (expanded)
            RefreshHidePeersButton();
    }

    private void ApplyCollapsedOffsets()
    {
        OffsetLeft = OffsetRight - 56;
        OffsetTop = OffsetBottom - 56;
        if (_panel != null)
        {
            _panel.OffsetLeft = -56;
            _panel.OffsetRight = 0;
            _panel.OffsetTop = -56;
            _panel.OffsetBottom = 0;
        }
    }

    private void ApplyExpandedOffsets()
    {
        float h = _inCombatContext ? 280f : 200f;
        OffsetLeft = OffsetRight - 320;
        OffsetTop = OffsetBottom - h;
        if (_panel != null)
        {
            _panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _panel.OffsetLeft = 0;
            _panel.OffsetRight = 0;
            _panel.OffsetTop = 0;
            _panel.OffsetBottom = 0;
        }
    }

    public void SetTool(DrawTool tool)
    {
        ActiveTool = ActiveTool == tool ? DrawTool.None : tool;
        if (ActiveTool != DrawTool.None)
            SetExpanded(true);
        RefreshToolVisuals();
        DrawCanvas.Instance?.OnToolChanged(ActiveTool);
        MainFile.Logger.Info($"Draw tool: {ActiveTool}");
    }

    private void RefreshToolVisuals()
    {
        Highlight(_brushBtn, ActiveTool == DrawTool.Brush);
        Highlight(_eraserBtn, ActiveTool == DrawTool.Eraser);
    }

    private static void Highlight(Button? btn, bool on)
    {
        if (btn == null)
            return;
        btn.Modulate = on ? new Color(1.25f, 1.15f, 0.55f) : Colors.White;
    }
}
