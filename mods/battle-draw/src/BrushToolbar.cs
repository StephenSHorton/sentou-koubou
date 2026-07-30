using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace BattleDraw;

public enum DrawTool
{
    None = 0,
    Brush = 1,
    Eraser = 2,
    Line = 3,
    Rect = 4,
    Ellipse = 5,
    FillRect = 6,
    FillEllipse = 7,
    Stamp = 8,
    Bucket = 9,
}

/// <summary>
/// Floating draw dock (map + combat), patterned after Excalidraw / FigJam / Canva Draw:
/// icon-first tools, fill-mode toggle (outline vs solid shapes), quick swatches, slim collapsed pill.
/// </summary>
public partial class BrushToolbar : Control
{
    public static BrushToolbar? Instance { get; private set; }

    public static BrushToolbar? CombatInstance => Instance;
    public static BrushToolbar? MapInstance => Instance;

    /// <summary>Effective armed tool (includes FillRect/FillEllipse when fill mode is on).</summary>
    public DrawTool ActiveTool { get; private set; } = DrawTool.None;

    /// <summary>When true, Rect/Oval commit as filled shapes (FigJam-style fill toggle).</summary>
    public bool FillShapes { get; private set; }

    private bool _expanded;
    private bool _inCombatContext;

    private Control? _pill;
    private Button? _pillToolBtn;
    private ColorRect? _pillColor;
    private Label? _pillSize;
    private Button? _pillExpand;

    private PanelContainer? _panel;
    private Control? _combatSection;
    private Button? _lineBtn;
    private Button? _rectBtn;
    private Button? _ellipseBtn;
    private Button? _stampBtn;
    private Button? _bucketBtn;
    private Button? _fillModeBtn;
    private Button? _clearBtn;
    private Button? _hidePeersBtn;
    private ColorPickerButton? _colorPicker;
    private HSlider? _sizeSlider;
    private Label? _sizeValueLabel;
    private HBoxContainer? _swatchRow;
    private readonly List<Button> _swatches = [];
    private readonly Dictionary<DrawTool, Button> _toolButtons = new();

    private static readonly Color Accent = new(0.92f, 0.78f, 0.38f);
    private static readonly Color AccentDim = new(0.55f, 0.45f, 0.22f);
    private static readonly Color PanelBg = new(0.08f, 0.075f, 0.09f, 0.96f);
    private static readonly Color InkMuted = new(0.72f, 0.7f, 0.62f, 0.9f);

    private static readonly Color[] QuickSwatches =
    [
        new(1f, 1f, 1f),
        new(0.15f, 0.15f, 0.16f),
        new(0.92f, 0.28f, 0.25f),
        new(0.98f, 0.72f, 0.2f),
        new(0.35f, 0.82f, 0.45f),
        new(0.3f, 0.65f, 0.98f),
        new(0.75f, 0.45f, 0.95f),
        new(0.98f, 0.55f, 0.75f),
    ];

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
        bar.OffsetRight = -18;
        bar.OffsetBottom = -40;
        bar.OffsetLeft = bar.OffsetRight - 200;
        bar.OffsetTop = bar.OffsetBottom - 52;
        layer.AddChild(bar);
        bar.BuildUi();
        bar.SetProcess(true);
        BrushConfig.SettingsChanged += bar.OnConfigChanged;
        Instance = bar;
        MainFile.Logger.Info("Battle Draw dock ready (icon rail + fill mode + swatches).");
    }

    public static void AttachCombat(Node? _)
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
        if (bar._pill != null && bar._pill.Visible && RectHits(bar._pill, 6f))
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
        Vector2 vp = c.GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920, 1080);
        if (rect.Size.X * rect.Size.Y > vp.X * vp.Y * 0.35f)
            return false;
        return rect.Grow(pad).HasPoint(c.GetGlobalMousePosition());
    }

    private static bool IsOverColorPickerPopup(BrushToolbar bar)
    {
        SceneTree? tree = bar.GetTree();
        return tree?.Root != null && PopupContainsColorPickerUnderMouse(tree.Root);
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
        if (_combatSection != null)
            _combatSection.Visible = combat;

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
        RefreshPill();
    }

    private void OnConfigChanged()
    {
        SyncSizeSlider();
        SyncColorPicker();
        RefreshSwatchSelection();
        RefreshPill();
    }

    private void BuildUi()
    {
        BuildCollapsedPill();
        BuildExpandedPanel();
        RefreshToolVisuals();
        SetExpanded(false);
        SetCombatContext(false);
        Visible = false;
    }

    // ── Collapsed pill (always-available status) ──────────────────────────

    private void BuildCollapsedPill()
    {
        _pill = new PanelContainer
        {
            Name = "DrawPill",
            MouseFilter = MouseFilterEnum.Stop,
        };
        _pill.AddThemeStyleboxOverride("panel", MakePillStyle());
        _pill.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
        AddChild(_pill);

        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 8);
        _pill.AddChild(row);

        _pillToolBtn = MakeIconButton(ToolGlyph(DrawTool.None), "Open draw tools · current tool", 40);
        _pillToolBtn.Pressed += () => SetExpanded(true);
        row.AddChild(_pillToolBtn);

        var sep = MakeVSep();
        row.AddChild(sep);

        _pillColor = new ColorRect
        {
            CustomMinimumSize = new Vector2(22, 22),
            Color = BrushConfig.CurrentColor,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        // Clip via panel
        var colorWrap = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
        colorWrap.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = Colors.Transparent,
            BorderColor = new Color(1f, 1f, 1f, 0.35f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginLeft = 2,
            ContentMarginRight = 2,
            ContentMarginTop = 2,
            ContentMarginBottom = 2,
        });
        colorWrap.AddChild(_pillColor);
        row.AddChild(colorWrap);

        _pillSize = new Label
        {
            Text = $"{BrushConfig.ClampedSize:0}",
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(22, 0),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        StyleMutedLabel(_pillSize);
        row.AddChild(_pillSize);

        _pillExpand = MakeIconButton("▴", "Expand tool dock", 36);
        _pillExpand.Pressed += () => SetExpanded(true);
        row.AddChild(_pillExpand);
    }

    // ── Expanded dock ─────────────────────────────────────────────────────

    private void BuildExpandedPanel()
    {
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
        vbox.AddThemeConstantOverride("separation", 12);
        _panel.AddChild(vbox);

        // Header
        var header = new HBoxContainer();
        vbox.AddChild(header);
        var title = new Label
        {
            Text = "Draw",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        StyleTitle(title);
        header.AddChild(title);
        var collapse = MakeIconButton("▾", "Collapse", 32);
        collapse.Pressed += () => SetExpanded(false);
        header.AddChild(collapse);

        // Combat-only tools
        _combatSection = new VBoxContainer { MouseFilter = MouseFilterEnum.Stop };
        _combatSection.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(_combatSection);

        _combatSection.AddChild(MakeSectionLabel("TOOLS"));

        // Row 1: primary geometry
        var row1 = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row1.AddThemeConstantOverride("separation", 6);
        _combatSection.AddChild(row1);

        _lineBtn = AddToolButton(row1, DrawTool.Line, "／", "Line  (L)\nDrag to draw a straight stroke");
        _rectBtn = AddToolButton(row1, DrawTool.Rect, "□", "Rectangle  (R)\nDrag · toggle Fill for solid");
        _ellipseBtn = AddToolButton(row1, DrawTool.Ellipse, "○", "Ellipse  (O)\nDrag · toggle Fill for solid");
        _stampBtn = AddToolButton(row1, DrawTool.Stamp, "◉", "Stamp\nClick to place a filled blob");

        // Row 2: fill modes + actions
        var row2 = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row2.AddThemeConstantOverride("separation", 6);
        _combatSection.AddChild(row2);

        _bucketBtn = AddToolButton(row2, DrawTool.Bucket, "▣", "Bucket  (G)\nClick inside a closed drawing only");
        _fillModeBtn = MakeIconButton("▤", "Fill shapes\nWhen on, Rect/Oval are solid (not outline)", 44);
        _fillModeBtn.Pressed += ToggleFillMode;
        row2.AddChild(_fillModeBtn);

        _clearBtn = MakeIconButton("⌫", "Clear all combat doodles", 44);
        _clearBtn.Pressed += () => DrawCanvas.Instance?.ClearAll();
        row2.AddChild(_clearBtn);

        _hidePeersBtn = MakeIconButton("👁", "Hide or show teammates' doodles", 44);
        _hidePeersBtn.Pressed += ToggleHidePeers;
        row2.AddChild(_hidePeersBtn);

        // Shared appearance (map + combat)
        vbox.AddChild(MakeSectionLabel("INK"));

        _swatchRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _swatchRow.AddThemeConstantOverride("separation", 6);
        vbox.AddChild(_swatchRow);
        foreach (Color sw in QuickSwatches)
        {
            Color capture = sw;
            var b = new Button
            {
                CustomMinimumSize = new Vector2(28, 28),
                FocusMode = FocusModeEnum.None,
                TooltipText = "Quick color",
                MouseDefaultCursorShape = CursorShape.PointingHand,
            };
            ApplySwatchStyle(b, capture, selected: false);
            b.Pressed += () =>
            {
                BrushConfig.SetColor(capture);
                DrawCanvas.Instance?.RefreshCursor();
                SyncColorPicker();
                RefreshSwatchSelection();
                RefreshPill();
            };
            _swatchRow.AddChild(b);
            _swatches.Add(b);
        }

        var colorRow = new HBoxContainer();
        colorRow.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(colorRow);
        var more = new Label { Text = "More", VerticalAlignment = VerticalAlignment.Center };
        StyleMutedLabel(more);
        colorRow.AddChild(more);
        _colorPicker = new ColorPickerButton
        {
            CustomMinimumSize = new Vector2(120, 32),
            Color = BrushConfig.CurrentColor,
            EditAlpha = true,
            TooltipText = "Custom ink color",
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _colorPicker.ColorChanged += c =>
        {
            BrushConfig.SetColor(c);
            DrawCanvas.Instance?.RefreshCursor();
            RefreshSwatchSelection();
            RefreshPill();
        };
        colorRow.AddChild(_colorPicker);

        var sizeRow = new HBoxContainer();
        sizeRow.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(sizeRow);
        var thin = new Label { Text = "Thin", VerticalAlignment = VerticalAlignment.Center };
        StyleMutedLabel(thin);
        sizeRow.AddChild(thin);
        _sizeSlider = new HSlider
        {
            MinValue = 1,
            MaxValue = 24,
            Step = 0.5,
            Value = BrushConfig.ClampedSize,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(140, 28),
            TooltipText = "Stroke width   [  ]",
            MouseFilter = MouseFilterEnum.Stop,
        };
        _sizeSlider.ValueChanged += v =>
        {
            BrushConfig.SetSize((float)v);
            RefreshPill();
        };
        sizeRow.AddChild(_sizeSlider);
        var thick = new Label { Text = "Thick", VerticalAlignment = VerticalAlignment.Center };
        StyleMutedLabel(thick);
        sizeRow.AddChild(thick);
        _sizeValueLabel = new Label
        {
            Text = $"{BrushConfig.ClampedSize:0.#}",
            CustomMinimumSize = new Vector2(28, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        StyleMutedLabel(_sizeValueLabel);
        sizeRow.AddChild(_sizeValueLabel);

        var tip = new Label
        {
            Text = "RMB pen · MMB erase · [ ] size · armed tool = LMB",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        tip.AddThemeColorOverride("font_color", InkMuted);
        tip.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(tip);
    }

    private Button AddToolButton(Control parent, DrawTool tool, string glyph, string tip)
    {
        var btn = MakeIconButton(glyph, tip, 44);
        btn.Pressed += () => SetTool(tool);
        parent.AddChild(btn);
        _toolButtons[tool] = btn;
        return btn;
    }

    // ── Tool selection ────────────────────────────────────────────────────

    public void SetTool(DrawTool tool)
    {
        if (tool == DrawTool.Eraser)
            tool = DrawTool.None;

        tool = ApplyFillMode(tool);

        // Toggle off if same effective family selected again
        if (SameToolFamily(ActiveTool, tool))
            tool = DrawTool.None;
        else if (ActiveTool == tool)
            tool = DrawTool.None;

        ActiveTool = tool;
        if (ActiveTool != DrawTool.None)
            SetExpanded(true);
        RefreshToolVisuals();
        RefreshPill();
        DrawCanvas.Instance?.OnToolChanged(ActiveTool);
        MainFile.Logger.Info($"Draw tool: {ActiveTool} (fillShapes={FillShapes})");
    }

    /// <summary>Hotkey entry that maps R/O through fill mode.</summary>
    public void SetToolFromHotkey(DrawTool tool) => SetTool(tool);

    public void ToggleFillMode()
    {
        FillShapes = !FillShapes;
        // Re-map active shape tool to filled/outline counterpart.
        ActiveTool = ApplyFillMode(StripFill(ActiveTool));
        if (ActiveTool is DrawTool.Rect or DrawTool.Ellipse or DrawTool.FillRect or DrawTool.FillEllipse)
            DrawCanvas.Instance?.OnToolChanged(ActiveTool);
        RefreshToolVisuals();
        MainFile.Logger.Info($"Shape fill mode: {FillShapes}");
    }

    private DrawTool ApplyFillMode(DrawTool tool)
    {
        if (!FillShapes)
        {
            return tool switch
            {
                DrawTool.FillRect => DrawTool.Rect,
                DrawTool.FillEllipse => DrawTool.Ellipse,
                _ => tool,
            };
        }

        return tool switch
        {
            DrawTool.Rect => DrawTool.FillRect,
            DrawTool.Ellipse => DrawTool.FillEllipse,
            _ => tool,
        };
    }

    private static DrawTool StripFill(DrawTool tool) => tool switch
    {
        DrawTool.FillRect => DrawTool.Rect,
        DrawTool.FillEllipse => DrawTool.Ellipse,
        _ => tool,
    };

    private static bool SameToolFamily(DrawTool a, DrawTool b)
    {
        a = a is DrawTool.FillRect ? DrawTool.Rect : a is DrawTool.FillEllipse ? DrawTool.Ellipse : a;
        b = b is DrawTool.FillRect ? DrawTool.Rect : b is DrawTool.FillEllipse ? DrawTool.Ellipse : b;
        return a == b && a != DrawTool.None;
    }

    private void RefreshToolVisuals()
    {
        SetToolSelected(_lineBtn, ActiveTool == DrawTool.Line);
        SetToolSelected(_rectBtn,
            ActiveTool is DrawTool.Rect or DrawTool.FillRect);
        SetToolSelected(_ellipseBtn,
            ActiveTool is DrawTool.Ellipse or DrawTool.FillEllipse);
        SetToolSelected(_stampBtn, ActiveTool == DrawTool.Stamp);
        SetToolSelected(_bucketBtn, ActiveTool == DrawTool.Bucket);
        SetToolSelected(_fillModeBtn, FillShapes);
        if (_fillModeBtn != null)
            _fillModeBtn.TooltipText = FillShapes
                ? "Fill shapes ON — Rect/Oval are solid\nClick to use outlines"
                : "Fill shapes OFF — Rect/Oval are outlines\nClick for solid fills";
    }

    private void RefreshPill()
    {
        if (_pillToolBtn != null)
            _pillToolBtn.Text = ToolGlyph(ActiveTool);
        if (_pillColor != null)
            _pillColor.Color = BrushConfig.CurrentColor;
        if (_pillSize != null)
            _pillSize.Text = $"{BrushConfig.ClampedSize:0}";
        if (_pillToolBtn != null)
        {
            _pillToolBtn.TooltipText = ActiveTool == DrawTool.None
                ? "Open draw tools · RMB pen always works"
                : $"Armed: {ActiveTool} · click to open dock";
            SetToolSelected(_pillToolBtn, ActiveTool != DrawTool.None);
        }
    }

    private static string ToolGlyph(DrawTool t) => t switch
    {
        DrawTool.Line => "／",
        DrawTool.Rect or DrawTool.FillRect => "□",
        DrawTool.Ellipse or DrawTool.FillEllipse => "○",
        DrawTool.Stamp => "◉",
        DrawTool.Bucket => "▣",
        DrawTool.Brush => "✎",
        DrawTool.Eraser => "⌫",
        _ => "✎",
    };

    // ── Expand / layout ───────────────────────────────────────────────────

    public void SetExpanded(bool expanded)
    {
        _expanded = expanded;
        if (_panel != null)
            _panel.Visible = expanded;
        if (_pill != null)
            _pill.Visible = !expanded;

        if (expanded)
            ApplyExpandedOffsets();
        else
            ApplyCollapsedOffsets();

        if (expanded)
        {
            RefreshHidePeersButton();
            RefreshSwatchSelection();
        }

        RefreshPill();
    }

    private void ApplyCollapsedOffsets()
    {
        OffsetLeft = OffsetRight - 200;
        OffsetTop = OffsetBottom - 52;
        if (_pill != null)
        {
            _pill.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _pill.OffsetLeft = 0;
            _pill.OffsetRight = 0;
            _pill.OffsetTop = 0;
            _pill.OffsetBottom = 0;
        }
    }

    private void ApplyExpandedOffsets()
    {
        float h = _inCombatContext ? 380f : 220f;
        OffsetLeft = OffsetRight - 300;
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

    // ── Misc actions ──────────────────────────────────────────────────────

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
        _hidePeersBtn.Text = hidden ? "🚫" : "👁";
        _hidePeersBtn.TooltipText = hidden
            ? "Show teammates' doodles"
            : "Hide teammates' doodles";
        SetToolSelected(_hidePeersBtn, hidden);
    }

    public void SyncSizeSlider()
    {
        if (_sizeSlider != null)
            _sizeSlider.SetValueNoSignal(BrushConfig.ClampedSize);
        if (_sizeValueLabel != null)
            _sizeValueLabel.Text = $"{BrushConfig.ClampedSize:0.#}";
        RefreshPill();
    }

    public void SyncColorPicker()
    {
        if (_colorPicker != null)
            _colorPicker.Color = BrushConfig.CurrentColor;
        RefreshPill();
    }

    private void RefreshSwatchSelection()
    {
        Color cur = BrushConfig.CurrentColor;
        for (int i = 0; i < _swatches.Count && i < QuickSwatches.Length; i++)
        {
            bool sel = ColorsNear(cur, QuickSwatches[i]);
            ApplySwatchStyle(_swatches[i], QuickSwatches[i], sel);
        }
    }

    private static bool ColorsNear(Color a, Color b) =>
        Mathf.Abs(a.R - b.R) < 0.04f
        && Mathf.Abs(a.G - b.G) < 0.04f
        && Mathf.Abs(a.B - b.B) < 0.04f;

    // ── Visual helpers ────────────────────────────────────────────────────

    private static Label MakeSectionLabel(string text)
    {
        var lab = new Label
        {
            Text = text,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        lab.AddThemeColorOverride("font_color", new Color(0.55f, 0.52f, 0.45f));
        lab.AddThemeFontSizeOverride("font_size", 11);
        return lab;
    }

    private static void StyleTitle(Label label)
    {
        label.AddThemeColorOverride("font_color", new Color(0.98f, 0.94f, 0.82f));
        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.85f));
        label.AddThemeConstantOverride("shadow_offset_x", 1);
        label.AddThemeConstantOverride("shadow_offset_y", 1);
    }

    private static void StyleMutedLabel(Label label)
    {
        label.AddThemeColorOverride("font_color", InkMuted);
        label.AddThemeFontSizeOverride("font_size", 12);
    }

    private static StyleBoxFlat MakePanelStyle() => new()
    {
        BgColor = PanelBg,
        BorderColor = AccentDim,
        BorderWidthBottom = 1,
        BorderWidthTop = 1,
        BorderWidthLeft = 1,
        BorderWidthRight = 1,
        CornerRadiusBottomLeft = 14,
        CornerRadiusBottomRight = 14,
        CornerRadiusTopLeft = 14,
        CornerRadiusTopRight = 14,
        ContentMarginLeft = 14,
        ContentMarginRight = 14,
        ContentMarginTop = 12,
        ContentMarginBottom = 12,
        ShadowColor = new Color(0f, 0f, 0f, 0.45f),
        ShadowSize = 8,
        ShadowOffset = new Vector2(0, 4),
    };

    private static StyleBoxFlat MakePillStyle() => new()
    {
        BgColor = PanelBg,
        BorderColor = AccentDim,
        BorderWidthBottom = 1,
        BorderWidthTop = 1,
        BorderWidthLeft = 1,
        BorderWidthRight = 1,
        CornerRadiusBottomLeft = 22,
        CornerRadiusBottomRight = 22,
        CornerRadiusTopLeft = 22,
        CornerRadiusTopRight = 22,
        ContentMarginLeft = 10,
        ContentMarginRight = 10,
        ContentMarginTop = 8,
        ContentMarginBottom = 8,
        ShadowColor = new Color(0f, 0f, 0f, 0.4f),
        ShadowSize = 6,
        ShadowOffset = new Vector2(0, 3),
    };

    private static Control MakeVSep()
    {
        var c = new ColorRect
        {
            CustomMinimumSize = new Vector2(1, 22),
            Color = new Color(1f, 1f, 1f, 0.12f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        return c;
    }

    private static Button MakeIconButton(string text, string tip, float size)
    {
        var btn = new Button
        {
            Text = text,
            TooltipText = tip,
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop,
            MouseDefaultCursorShape = CursorShape.PointingHand,
            CustomMinimumSize = new Vector2(size, size),
        };
        ApplyToolChrome(btn, selected: false);
        return btn;
    }

    private static void ApplyToolChrome(Button btn, bool selected)
    {
        Color bg = selected
            ? new Color(0.22f, 0.18f, 0.1f, 0.98f)
            : new Color(0.12f, 0.11f, 0.13f, 0.96f);
        Color border = selected ? Accent : new Color(0.35f, 0.32f, 0.28f);
        StyleBoxFlat Make(Color b, Color br) => new()
        {
            BgColor = b,
            BorderColor = br,
            BorderWidthBottom = selected ? 2 : 1,
            BorderWidthTop = selected ? 2 : 1,
            BorderWidthLeft = selected ? 2 : 1,
            BorderWidthRight = selected ? 2 : 1,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            ContentMarginLeft = 6,
            ContentMarginRight = 6,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
        };
        btn.AddThemeStyleboxOverride("normal", Make(bg, border));
        btn.AddThemeStyleboxOverride("hover",
            Make(new Color(0.18f, 0.16f, 0.14f, 0.98f), Accent));
        btn.AddThemeStyleboxOverride("pressed",
            Make(new Color(0.08f, 0.07f, 0.09f, 0.98f), AccentDim));
        btn.AddThemeColorOverride("font_color", selected ? Accent : new Color(0.95f, 0.93f, 0.88f));
        btn.AddThemeColorOverride("font_hover_color", Colors.White);
        btn.AddThemeFontSizeOverride("font_size", 16);
    }

    private static void SetToolSelected(Button? btn, bool on)
    {
        if (btn == null)
            return;
        ApplyToolChrome(btn, on);
    }

    private static void ApplySwatchStyle(Button btn, Color fill, bool selected)
    {
        StyleBoxFlat box = new()
        {
            BgColor = fill,
            BorderColor = selected ? Accent : new Color(1f, 1f, 1f, 0.25f),
            BorderWidthBottom = selected ? 2 : 1,
            BorderWidthTop = selected ? 2 : 1,
            BorderWidthLeft = selected ? 2 : 1,
            BorderWidthRight = selected ? 2 : 1,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
        };
        btn.AddThemeStyleboxOverride("normal", box);
        btn.AddThemeStyleboxOverride("hover", box);
        btn.AddThemeStyleboxOverride("pressed", box);
        btn.Text = "";
    }
}
