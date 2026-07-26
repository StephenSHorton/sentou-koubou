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
/// Single compact toolbar for map + combat (no separate submenu panel).
/// ColorPickerButton popup gets a Size slider injected into it.
/// </summary>
public partial class BrushToolbar : Control
{
    /// <summary>One shared instance for map and combat.</summary>
    public static BrushToolbar? Instance { get; private set; }

    // Back-compat aliases
    public static BrushToolbar? CombatInstance => Instance;
    public static BrushToolbar? MapInstance => Instance;

    public DrawTool ActiveTool { get; private set; } = DrawTool.None;

    private HBoxContainer? _row;
    private Button? _brushBtn;
    private Button? _eraserBtn;
    private Button? _clearBtn;
    private Button? _hidePeersBtn;
    private ColorPickerButton? _colorPicker;
    private HSlider? _sizeSlider;
    private Label? _sizeLabel;
    private bool _injectedPickerExtras;
    private bool _inCombatContext;

    /// <summary>
    /// One global toolbar on a high CanvasLayer under the scene root.
    /// Survives combat/map transitions; visibility swaps via context.
    /// </summary>
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
        bar.OffsetLeft = bar.OffsetRight - 420;
        bar.OffsetTop = bar.OffsetBottom - 52;
        layer.AddChild(bar);
        bar.BuildUi();
        bar.SetProcess(true);
        BrushConfig.SettingsChanged += bar.OnConfigChanged;
        Instance = bar;
        MainFile.Logger.Info("Battle Draw unified toolbar ready (color picker + tools).");
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

    public static void DetachCombat()
    {
        Instance?.SetCombatContext(false);
    }

    public static void DetachMap()
    {
        // Global bar stays; visibility polls map/combat.
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
        if (RectHits(bar, 4f))
            return true;
        // Color picker popup (viewport root).
        if (bar._colorPicker != null && IsOverColorPickerPopup(bar))
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
        // Combat-only tools
        if (_brushBtn != null) _brushBtn.Visible = combat;
        if (_eraserBtn != null) _eraserBtn.Visible = combat;
        if (_clearBtn != null) _clearBtn.Visible = combat;
        if (_hidePeersBtn != null) _hidePeersBtn.Visible = combat;
        // Offset above hand in combat
        OffsetBottom = combat ? -96 : -36;
        OffsetTop = OffsetBottom - 52;
        if (!combat && ActiveTool != DrawTool.None)
        {
            ActiveTool = DrawTool.None;
            RefreshToolVisuals();
            DrawCanvas.Instance?.OnToolChanged(DrawTool.None);
        }
        RefreshHidePeersButton();
    }

    private void OnConfigChanged()
    {
        SyncSizeSlider();
        SyncColorPicker();
    }

    private void BuildUi()
    {
        _row = new HBoxContainer
        {
            Name = "ToolRow",
            MouseFilter = MouseFilterEnum.Stop,
            Alignment = BoxContainer.AlignmentMode.End,
        };
        _row.AddThemeConstantOverride("separation", 6);
        _row.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_row);

        // Small text buttons — high contrast, no generated plates.
        _brushBtn = MakeToolButton("Brush", "Brush (B) — LMB when armed; RMB always draws");
        _brushBtn.Pressed += () => SetTool(DrawTool.Brush);
        _row.AddChild(_brushBtn);

        _eraserBtn = MakeToolButton("Erase", "Eraser (E) — LMB when armed; MMB always erases");
        _eraserBtn.Pressed += () => SetTool(DrawTool.Eraser);
        _row.AddChild(_eraserBtn);

        _clearBtn = MakeToolButton("Clear", "Clear all combat doodles");
        _clearBtn.Pressed += () => DrawCanvas.Instance?.ClearAll();
        _row.AddChild(_clearBtn);

        _hidePeersBtn = MakeToolButton("Peers", "Toggle other players' combat drawings");
        _hidePeersBtn.Pressed += ToggleHidePeers;
        _row.AddChild(_hidePeersBtn);

        _colorPicker = new ColorPickerButton
        {
            CustomMinimumSize = new Vector2(48, 40),
            Color = BrushConfig.CurrentColor,
            EditAlpha = true,
            TooltipText = "Ink color (opens picker — size slider is inside)",
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop,
        };
        _colorPicker.ColorChanged += c =>
        {
            BrushConfig.SetColor(c);
            DrawCanvas.Instance?.RefreshCursor();
        };
        // Inject size controls when the picker popup opens.
        _colorPicker.Pressed += OnColorPickerPressed;
        _row.AddChild(_colorPicker);

        // Always-visible compact size (also mirrored inside picker popup).
        _sizeLabel = new Label
        {
            Text = $"Sz {BrushConfig.ClampedSize:0.#}",
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(52, 0),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _sizeLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.92f, 0.8f));
        _sizeLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.85f));
        _sizeLabel.AddThemeConstantOverride("shadow_offset_x", 1);
        _sizeLabel.AddThemeConstantOverride("shadow_offset_y", 1);
        _row.AddChild(_sizeLabel);

        _sizeSlider = new HSlider
        {
            MinValue = 1,
            MaxValue = 24,
            Step = 0.5,
            Value = BrushConfig.ClampedSize,
            CustomMinimumSize = new Vector2(110, 28),
            TooltipText = "Brush size  [  ]",
            MouseFilter = MouseFilterEnum.Stop,
        };
        _sizeSlider.ValueChanged += v => BrushConfig.SetSize((float)v);
        _row.AddChild(_sizeSlider);

        RefreshToolVisuals();
        SetCombatContext(false);
        Visible = false;
    }

    private void OnColorPickerPressed()
    {
        // Defer until ColorPickerButton has spawned its popup.
        Callable.From(TryInjectSizeIntoColorPicker).CallDeferred();
        Callable.From(TryInjectSizeIntoColorPicker).CallDeferred();
    }

    private void TryInjectSizeIntoColorPicker()
    {
        if (_injectedPickerExtras || _colorPicker == null)
        {
            // Still update value if already injected.
            UpdateInjectedSliderValue();
            return;
        }

        SceneTree? tree = GetTree();
        if (tree?.Root == null)
            return;

        Popup? popup = FindColorPickerPopup(tree.Root);
        if (popup == null)
            return;

        // Find a VBox inside the popup to append to.
        Control host = FindBestHost(popup);
        var sep = new HSeparator();
        host.AddChild(sep);

        var sizeRow = new HBoxContainer();
        sizeRow.AddThemeConstantOverride("separation", 8);
        host.AddChild(sizeRow);

        var lab = new Label { Text = "Size", VerticalAlignment = VerticalAlignment.Center };
        sizeRow.AddChild(lab);

        var slider = new HSlider
        {
            Name = "BattleDrawPickerSize",
            MinValue = 1,
            MaxValue = 24,
            Step = 0.5,
            Value = BrushConfig.ClampedSize,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(160, 24),
        };
        slider.ValueChanged += v =>
        {
            BrushConfig.SetSize((float)v);
            if (_sizeSlider != null)
                _sizeSlider.SetValueNoSignal(v);
            if (_sizeLabel != null)
                _sizeLabel.Text = $"Sz {BrushConfig.ClampedSize:0.#}";
        };
        sizeRow.AddChild(slider);

        var val = new Label { Name = "BattleDrawPickerSizeVal", Text = $"{BrushConfig.ClampedSize:0.#}" };
        sizeRow.AddChild(val);

        _injectedPickerExtras = true;
        // Reset flag when popup closes so we can re-inject next open if tree rebuilt.
        popup.VisibilityChanged += () =>
        {
            if (!popup.Visible)
                _injectedPickerExtras = false;
        };
    }

    private void UpdateInjectedSliderValue()
    {
        // no-op if not open
    }

    private static Popup? FindColorPickerPopup(Node node)
    {
        if (node is Popup { Visible: true } popup)
        {
            foreach (Node child in popup.GetChildren())
            {
                if (child is ColorPicker || HasDescendantColorPicker(child))
                    return popup;
            }
        }

        foreach (Node child in node.GetChildren())
        {
            Popup? found = FindColorPickerPopup(child);
            if (found != null)
                return found;
        }

        return null;
    }

    private static Control FindBestHost(Popup popup)
    {
        Control? vbox = FindFirstVBox(popup);
        if (vbox != null)
            return vbox;
        // Popup is a Window in Godot 4 — add a small footer container as child.
        var footer = new VBoxContainer { Name = "BattleDrawPickerFooter" };
        popup.AddChild(footer);
        return footer;
    }

    private static Control? FindFirstVBox(Node node)
    {
        if (node is VBoxContainer v)
            return v;
        foreach (Node child in node.GetChildren())
        {
            Control? found = FindFirstVBox(child);
            if (found != null)
                return found;
        }

        return null;
    }

    private static Button MakeToolButton(string text, string tip)
    {
        var btn = new Button
        {
            Text = text,
            TooltipText = tip,
            FocusMode = FocusModeEnum.None,
            CustomMinimumSize = new Vector2(64, 40),
            MouseFilter = MouseFilterEnum.Stop,
            MouseDefaultCursorShape = CursorShape.PointingHand,
        };
        // Dark readable chrome so labels contrast.
        StyleBoxFlat Make(Color bg, Color border) => new()
        {
            BgColor = bg,
            BorderColor = border,
            BorderWidthBottom = 2, BorderWidthTop = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            ContentMarginLeft = 10, ContentMarginRight = 10,
            ContentMarginTop = 6, ContentMarginBottom = 6,
        };
        btn.AddThemeStyleboxOverride("normal", Make(new Color(0.08f, 0.07f, 0.09f, 0.92f), new Color(0.75f, 0.65f, 0.35f)));
        btn.AddThemeStyleboxOverride("hover", Make(new Color(0.14f, 0.12f, 0.1f, 0.95f), new Color(0.95f, 0.85f, 0.45f)));
        btn.AddThemeStyleboxOverride("pressed", Make(new Color(0.05f, 0.04f, 0.06f, 0.95f), new Color(0.5f, 0.42f, 0.22f)));
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
        _hidePeersBtn.Text = hidden ? "Peers: off" : "Peers";
        _hidePeersBtn.Modulate = hidden ? new Color(1.2f, 1.05f, 0.5f) : Colors.White;
    }

    public void SyncSizeSlider()
    {
        if (_sizeSlider != null)
            _sizeSlider.SetValueNoSignal(BrushConfig.ClampedSize);
        if (_sizeLabel != null)
            _sizeLabel.Text = $"Sz {BrushConfig.ClampedSize:0.#}";
    }

    public void SyncColorPicker()
    {
        if (_colorPicker != null)
            _colorPicker.Color = BrushConfig.CurrentColor;
    }

    public void SetTool(DrawTool tool)
    {
        ActiveTool = ActiveTool == tool ? DrawTool.None : tool;
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
