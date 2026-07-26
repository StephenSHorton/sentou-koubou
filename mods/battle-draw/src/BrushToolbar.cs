using Godot;

namespace BattleDraw;

public enum DrawTool
{
    None = 0,
    Brush = 1,
    Eraser = 2,
}

public enum DrawSurfaceKind
{
    Combat = 0,
    Map = 1,
}

/// <summary>
/// Collapsible palette for combat (full tools) or map (color + size only).
/// Shared <see cref="BrushConfig"/> drives both.
/// </summary>
public partial class BrushToolbar : Control
{
    public static BrushToolbar? CombatInstance { get; private set; }
    public static BrushToolbar? MapInstance { get; private set; }

    public DrawTool ActiveTool { get; private set; } = DrawTool.None;
    public DrawSurfaceKind Kind { get; private set; }

    private bool _expanded;
    private PanelContainer? _panel;
    private Button? _tabButton;
    private Button? _brushBtn;
    private Button? _eraserBtn;
    private HSlider? _sizeSlider;
    private ColorPickerButton? _colorPicker;
    private Label? _sizeValueLabel;
    private Button? _hidePeersBtn;

    public static void AttachCombat(CanvasLayer layer)
    {
        DetachCombat();
        var bar = Create(DrawSurfaceKind.Combat);
        layer.AddChild(bar);
        CombatInstance = bar;
        MainFile.Logger.Info("Combat brush toolbar ready.");
    }

    public static void AttachMap(Node parent)
    {
        DetachMap();
        var bar = Create(DrawSurfaceKind.Map);
        parent.AddChild(bar);
        MapInstance = bar;
        MainFile.Logger.Info("Map brush toolbar ready (color + size).");
    }

    private static BrushToolbar Create(DrawSurfaceKind kind)
    {
        var bar = new BrushToolbar
        {
            Name = kind == DrawSurfaceKind.Combat ? "BattleDrawToolbar" : "BattleDrawMapToolbar",
            MouseFilter = MouseFilterEnum.Stop,
            ZIndex = 20,
            Kind = kind,
        };
        bar.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
        bar.GrowHorizontal = GrowDirection.Begin;
        bar.GrowVertical = GrowDirection.Begin;
        bar.OffsetRight = -20;
        bar.OffsetBottom = kind == DrawSurfaceKind.Combat ? -96 : -48;
        bar.OffsetLeft = bar.OffsetRight - 56;
        bar.OffsetTop = bar.OffsetBottom - 56;
        bar.BuildUi();
        BrushConfig.SettingsChanged += bar.OnConfigChanged;
        return bar;
    }

    public static void DetachCombat()
    {
        if (CombatInstance != null && GodotObject.IsInstanceValid(CombatInstance))
        {
            BrushConfig.SettingsChanged -= CombatInstance.OnConfigChanged;
            CombatInstance.QueueFree();
        }

        CombatInstance = null;
    }

    public static void DetachMap()
    {
        if (MapInstance != null && GodotObject.IsInstanceValid(MapInstance))
        {
            BrushConfig.SettingsChanged -= MapInstance.OnConfigChanged;
            MapInstance.QueueFree();
        }

        MapInstance = null;
    }

    /// <summary>Back-compat for older call sites.</summary>
    public static void Detach() => DetachCombat();

    public static void SyncAllSizeSliders()
    {
        CombatInstance?.SyncSizeSlider();
        MapInstance?.SyncSizeSlider();
    }

    public static bool HitsPointer()
    {
        if (Hits(CombatInstance))
            return true;
        if (Hits(MapInstance))
            return true;
        return false;
    }

    private static bool Hits(BrushToolbar? bar)
    {
        if (bar == null || !GodotObject.IsInstanceValid(bar) || !bar.IsVisibleInTree())
            return false;
        if (bar._tabButton != null && RectHits(bar._tabButton, 6f))
            return true;
        if (bar._panel is { Visible: true } panel && RectHits(panel, 8f))
            return true;
        if (bar._expanded && IsOverColorPickerPopup(bar))
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
        if (CombatInstance == this)
            CombatInstance = null;
        if (MapInstance == this)
            MapInstance = null;
        base._ExitTree();
    }

    private void OnConfigChanged()
    {
        SyncSizeSlider();
        SyncColorPicker();
    }

    private void BuildUi()
    {
        Texture2D? tabIcon = DrawAssets.IconTab ?? DrawAssets.IconBrush;
        _tabButton = new Button
        {
            Name = "DrawToolsTab",
            TooltipText = Kind == DrawSurfaceKind.Map ? "Pen color & size (map)" : "Show draw tools",
            FocusMode = FocusModeEnum.None,
            ExpandIcon = true,
            CustomMinimumSize = new Vector2(56, 56),
            MouseFilter = MouseFilterEnum.Stop,
            MouseDefaultCursorShape = CursorShape.PointingHand,
        };
        if (tabIcon != null)
            _tabButton.Icon = tabIcon;
        else
            _tabButton.Text = "✎";
        _tabButton.Pressed += ToggleExpanded;
        _tabButton.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
        _tabButton.OffsetLeft = -56;
        _tabButton.OffsetTop = -56;
        _tabButton.OffsetRight = 0;
        _tabButton.OffsetBottom = 0;
        AddChild(_tabButton);

        float panelH = Kind == DrawSurfaceKind.Combat ? 250f : 160f;
        _panel = new PanelContainer
        {
            Name = "ToolPanel",
            MouseFilter = MouseFilterEnum.Stop,
            Visible = false,
        };
        _panel.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
        _panel.OffsetRight = 0;
        _panel.OffsetBottom = -64;
        _panel.OffsetLeft = -300;
        _panel.OffsetTop = _panel.OffsetBottom - panelH;

        StyleBoxTexture? plate = DrawAssets.MakeNineSlice(DrawAssets.PanelTools);
        if (plate != null)
            _panel.AddThemeStyleboxOverride("panel", plate);
        else
        {
            _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = new Color(0.12f, 0.1f, 0.08f, 0.94f),
                BorderColor = new Color(0.55f, 0.45f, 0.25f, 1f),
                BorderWidthBottom = 2,
                BorderWidthTop = 2,
                BorderWidthLeft = 2,
                BorderWidthRight = 2,
                ContentMarginLeft = 12,
                ContentMarginRight = 12,
                ContentMarginTop = 10,
                ContentMarginBottom = 10,
            });
        }

        var vbox = new VBoxContainer { MouseFilter = MouseFilterEnum.Stop };
        vbox.AddThemeConstantOverride("separation", 8);
        _panel.AddChild(vbox);

        var header = new HBoxContainer();
        vbox.AddChild(header);
        var title = new Label
        {
            Text = Kind == DrawSurfaceKind.Map ? "Map pen" : "Battle Draw",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        title.AddThemeColorOverride("font_color", new Color(0.95f, 0.9f, 0.7f));
        header.AddChild(title);
        var hideBtn = new Button
        {
            Text = "▾",
            TooltipText = "Collapse",
            CustomMinimumSize = new Vector2(28, 28),
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop,
        };
        hideBtn.Pressed += () => SetExpanded(false);
        header.AddChild(hideBtn);

        if (Kind == DrawSurfaceKind.Combat)
        {
            var tools = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
            tools.AddThemeConstantOverride("separation", 8);
            vbox.AddChild(tools);

            _brushBtn = MakeIconButton(DrawAssets.IconBrush, "Brush — left-drag on field");
            _brushBtn.Pressed += () => SetTool(DrawTool.Brush);
            tools.AddChild(_brushBtn);

            _eraserBtn = MakeIconButton(DrawAssets.IconEraser, "Eraser — left-drag on field");
            _eraserBtn.Pressed += () => SetTool(DrawTool.Eraser);
            tools.AddChild(_eraserBtn);

            var clearBtn = MakeIconButton(DrawAssets.IconClear, "Clear all doodles");
            clearBtn.Pressed += () => DrawCanvas.Instance?.ClearAll();
            tools.AddChild(clearBtn);
        }

        // Color
        var colorRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        colorRow.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(colorRow);
        var colorLabel = new Label { Text = "Color", MouseFilter = MouseFilterEnum.Ignore };
        colorLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        colorRow.AddChild(colorLabel);
        _colorPicker = new ColorPickerButton
        {
            CustomMinimumSize = new Vector2(120, 32),
            Color = BrushConfig.CurrentColor,
            EditAlpha = true,
            TooltipText = Kind == DrawSurfaceKind.Map
                ? "Map pen color (your lines)"
                : "Combat ink color",
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop,
        };
        _colorPicker.ColorChanged += OnColorChanged;
        colorRow.AddChild(_colorPicker);

        // Size
        var sizeRow = new HBoxContainer();
        sizeRow.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(sizeRow);
        var sizeLabel = new Label { Text = "Size", MouseFilter = MouseFilterEnum.Ignore };
        sizeLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        sizeRow.AddChild(sizeLabel);
        _sizeSlider = new HSlider
        {
            MinValue = 1,
            MaxValue = 24,
            Step = 0.5,
            Value = BrushConfig.ClampedSize,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(140, 24),
            TooltipText = "Brush size",
            MouseFilter = MouseFilterEnum.Stop,
        };
        _sizeSlider.ValueChanged += OnSizeChanged;
        sizeRow.AddChild(_sizeSlider);
        _sizeValueLabel = new Label
        {
            Text = $"{BrushConfig.ClampedSize:0.#}",
            CustomMinimumSize = new Vector2(28, 0),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        sizeRow.AddChild(_sizeValueLabel);

        if (Kind == DrawSurfaceKind.Combat)
        {
            _hidePeersBtn = new Button
            {
                Text = "Hide others' drawings",
                TooltipText = "Toggle co-op partners' combat doodles",
                FocusMode = FocusModeEnum.None,
                MouseFilter = MouseFilterEnum.Stop,
                CustomMinimumSize = new Vector2(0, 32),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            _hidePeersBtn.Pressed += ToggleHidePeers;
            vbox.AddChild(_hidePeersBtn);
            RefreshHidePeersButton();

            var tip = new Label
            {
                Text = "Tip: RMB pen · MMB erase · no ink on hand",
                HorizontalAlignment = HorizontalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            tip.AddThemeColorOverride("font_color", new Color(0.75f, 0.72f, 0.6f, 0.9f));
            tip.AddThemeFontSizeOverride("font_size", 12);
            vbox.AddChild(tip);
        }
        else
        {
            var tip = new Label
            {
                Text = "Applies to your map pen · [ ] size",
                HorizontalAlignment = HorizontalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            tip.AddThemeColorOverride("font_color", new Color(0.75f, 0.72f, 0.6f, 0.9f));
            tip.AddThemeFontSizeOverride("font_size", 12);
            vbox.AddChild(tip);
        }

        AddChild(_panel);
        RefreshToolVisuals();
        SetExpanded(false);
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
        _hidePeersBtn.Modulate = hidden ? new Color(1.15f, 1.05f, 0.55f) : Colors.White;
    }

    private void OnColorChanged(Color c)
    {
        BrushConfig.SetColor(c);
        DrawCanvas.Instance?.RefreshCursor();
    }

    private void OnSizeChanged(double v) => BrushConfig.SetSize((float)v);

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

    private static Button MakeIconButton(Texture2D? icon, string tip)
    {
        var btn = new Button
        {
            CustomMinimumSize = new Vector2(48, 48),
            TooltipText = tip,
            FocusMode = FocusModeEnum.None,
            ExpandIcon = true,
            MouseFilter = MouseFilterEnum.Stop,
            MouseDefaultCursorShape = CursorShape.PointingHand,
        };
        if (icon != null)
            btn.Icon = icon;
        else
            btn.Text = tip.Length > 0 ? tip[..1] : "?";
        return btn;
    }

    private void ToggleExpanded() => SetExpanded(!_expanded);

    public void SetExpanded(bool expanded)
    {
        _expanded = expanded;
        if (_panel != null)
            _panel.Visible = expanded;

        if (expanded)
        {
            OffsetLeft = OffsetRight - 310;
            OffsetTop = OffsetBottom - (Kind == DrawSurfaceKind.Combat ? 320 : 200);
            RefreshHidePeersButton();
        }
        else
        {
            OffsetLeft = OffsetRight - 56;
            OffsetTop = OffsetBottom - 56;
        }

        if (_tabButton != null)
        {
            _tabButton.TooltipText = expanded ? "Hide" : (Kind == DrawSurfaceKind.Map ? "Pen color & size" : "Show draw tools");
            _tabButton.Modulate = expanded ? new Color(1.15f, 1.1f, 0.7f) : Colors.White;
            Texture2D? tab = DrawAssets.IconTab ?? DrawAssets.IconBrush;
            if (tab != null)
                _tabButton.Icon = tab;
        }
    }

    public void SetTool(DrawTool tool)
    {
        if (Kind != DrawSurfaceKind.Combat)
            return;
        ActiveTool = ActiveTool == tool ? DrawTool.None : tool;
        if (ActiveTool != DrawTool.None)
            SetExpanded(true);
        RefreshToolVisuals();
        DrawCanvas.Instance?.OnToolChanged(ActiveTool);
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
        btn.Modulate = on ? new Color(1.2f, 1.15f, 0.65f) : Colors.White;
    }
}
