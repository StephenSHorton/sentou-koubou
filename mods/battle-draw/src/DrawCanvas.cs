using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace BattleDraw;

/// <summary>
/// Combat doodle surface — event-driven <see cref="_Input"/> (zero idle ticks).
/// Finished ink is baked into a half-res <see cref="InkSurface"/> (map-style);
/// only the active stroke is a live <see cref="Line2D"/>.
/// </summary>
public partial class DrawCanvas : Control
{
    public static DrawCanvas? Instance { get; private set; }

    private const float MinPointDistance = 2.5f;
    public const float HandNoDrawBandFrac = 0.30f;
    private const string QuillCursorPath = "res://images/packed/common_ui/cursor_quill.png";
    private const string QuillTiltedPath = "res://images/packed/common_ui/cursor_quill_tilted.png";
    private const string EraserCursorPath = "res://images/packed/common_ui/cursor_eraser.png";

    private readonly InkSurface _ink = new();
    private readonly Dictionary<(ulong Owner, int Id), List<Vector2>> _openRemote = new();
    private readonly Dictionary<(ulong Owner, int Id), (Color Color, float Width)> _remoteStyles = new();

    private Line2D? _activeLine;
    private readonly List<Vector2> _activePoints = new();
    private Color _activeColor = Colors.White;
    private float _activeWidth = 3.5f;
    private bool _drawing;
    private DrawTool _tool = DrawTool.None;
    private DrawTool _strokeTool;
    private int _activeStrokeId;
    private bool _cursorOverridden;
    private static Texture2D? _quillTex;
    private static Texture2D? _eraserTex;
    private static readonly Vector2 QuillHotspot = new(2f, 56f);
    private int _strokeLogBudget = 3;

    public bool HideRemoteStrokes { get; private set; }

    public static void AttachTo(NCombatRoom room)
    {
        Instance?.Teardown();
        BrushToolbar.DetachCombat();

        var layer = new CanvasLayer
        {
            Name = "BattleDrawUiLayer",
            Layer = 100,
            ProcessMode = ProcessModeEnum.Inherit,
        };
        room.AddChild(layer);

        var inkRoot = new Node2D
        {
            Name = "BattleDrawInk",
            ZIndex = 0,
            ProcessMode = ProcessModeEnum.Inherit,
        };
        layer.AddChild(inkRoot);

        var canvas = new DrawCanvas
        {
            Name = "BattleDrawCanvas",
            MouseFilter = MouseFilterEnum.Ignore,
            ProcessMode = ProcessModeEnum.Inherit,
            CustomMinimumSize = Vector2.Zero,
        };
        layer.AddChild(canvas);
        canvas.SetProcess(false);
        canvas.SetProcessInput(true);
        canvas._ink.Attach(inkRoot);

        Viewport? vp = room.GetViewport();
        if (vp != null)
            canvas._ink.EnsureSize(vp.GetVisibleRect().Size);

        BrushToolbar.AttachCombat(layer);

        Instance = canvas;
        MainFile.Logger.Info(
            "Battle Draw surface ready — baked half-res ink, event input only (v0.5).");
    }

    public override void _Input(InputEvent e)
    {
        switch (e)
        {
            case InputEventKey { Pressed: true, Echo: false } key:
                HandleHotkey(key.Keycode);
                return;
            case InputEventMouseButton mb:
                HandleMouseButton(mb);
                return;
            case InputEventMouseMotion mm:
                if (!_drawing)
                {
                    MouseButtonMask mask = mm.ButtonMask;
                    if (mask == 0)
                        return;
                    if (mask == MouseButtonMask.Left && _tool == DrawTool.None)
                        return;
                    if (IntentFromMask(mask) == null)
                        return;
                }

                HandleMouseMotion(mm);
                return;
        }
    }

    private void HandleHotkey(Key key)
    {
        switch (key)
        {
            case Key.Bracketleft:
                BrushConfig.NudgeSize(-0.5f);
                BrushToolbar.SyncAllSizeSliders();
                break;
            case Key.Bracketright:
                BrushConfig.NudgeSize(0.5f);
                BrushToolbar.SyncAllSizeSliders();
                break;
            case Key.B:
                BrushToolbar.CombatInstance?.SetTool(DrawTool.Brush);
                break;
            case Key.E:
                BrushToolbar.CombatInstance?.SetTool(DrawTool.Eraser);
                break;
        }
    }

    private void HandleMouseButton(InputEventMouseButton mb)
    {
        Viewport? vp = GetViewport();
        if (vp == null)
            return;

        if (!mb.Pressed)
        {
            if (mb.ButtonIndex is MouseButton.Left or MouseButton.Right or MouseButton.Middle)
            {
                if (_drawing)
                    EndStroke();
            }

            return;
        }

        DrawTool? intent = IntentFromButton(mb.ButtonIndex);
        if (intent == null)
            return;

        if (GetBlockReason() != null)
        {
            if (_drawing)
                EndStroke();
            return;
        }

        StartStroke(intent.Value, GetInkMousePos(vp));
    }

    private void HandleMouseMotion(InputEventMouseMotion mm)
    {
        Viewport? vp = GetViewport();
        if (vp == null)
            return;

        DrawTool? intent = IntentFromMask(mm.ButtonMask);
        if (intent == null)
        {
            if (_drawing)
                EndStroke();
            return;
        }

        if (GetBlockReason() != null)
        {
            if (_drawing)
                EndStroke();
            return;
        }

        Vector2 pos = GetInkMousePos(vp);
        if (!_drawing || _strokeTool != intent.Value)
            StartStroke(intent.Value, pos);
        else
            ContinueStroke(pos);
    }

    private DrawTool? IntentFromButton(MouseButton button) => button switch
    {
        MouseButton.Middle => DrawTool.Eraser,
        MouseButton.Right => DrawTool.Brush,
        MouseButton.Left when _tool is DrawTool.Brush or DrawTool.Eraser => _tool,
        _ => null,
    };

    private DrawTool? IntentFromMask(MouseButtonMask mask)
    {
        if ((mask & MouseButtonMask.Middle) != 0)
            return DrawTool.Eraser;
        if ((mask & MouseButtonMask.Right) != 0)
            return DrawTool.Brush;
        if ((mask & MouseButtonMask.Left) != 0 && _tool is DrawTool.Brush or DrawTool.Eraser)
            return _tool;
        return null;
    }

    private static Vector2 GetInkMousePos(Viewport vp)
    {
        Rect2 vis = vp.GetVisibleRect();
        return vp.GetMousePosition() - vis.Position;
    }

    public void OnToolChanged(DrawTool tool)
    {
        _tool = tool;
        EndStroke();
        RefreshCursor();
    }

    public void RefreshCursor() => ApplyCursorForTool(_tool);

    public void SetHideRemoteStrokes(bool hide)
    {
        HideRemoteStrokes = hide;
        _ink.SetRemoteVisible(!hide);
        MainFile.Logger.Info(hide ? "Hiding peer combat doodles." : "Showing peer combat doodles.");
    }

    private void ContinueStroke(Vector2 local)
    {
        Viewport? vp = GetViewport();
        if (vp != null)
            _ink.EnsureSize(vp.GetVisibleRect().Size);

        if (_strokeTool == DrawTool.Eraser)
        {
            float radius = BrushConfig.ClampedSize * 2.8f;
            _ink.EraseCircleScreen(local, radius);
            DrawSync.Instance?.SendErase(local, radius);
            return;
        }

        if (_activeLine == null || _activePoints.Count == 0)
            return;

        if (_activePoints[^1].DistanceTo(local) < MinPointDistance)
            return;

        Vector2 prev = _activePoints[^1];
        _activePoints.Add(local);
        _activeLine.AddPoint(local);
        DrawSync.Instance?.SendPoint(_activeStrokeId, local);
    }

    private void StartStroke(DrawTool tool, Vector2 localPos)
    {
        Viewport? vp = GetViewport();
        if (vp != null)
            _ink.EnsureSize(vp.GetVisibleRect().Size);

        _drawing = true;
        _strokeTool = tool;
        ApplyCursorForTool(tool);

        if (tool == DrawTool.Eraser)
        {
            FreeActiveLine();
            float radius = BrushConfig.ClampedSize * 2.8f;
            _ink.EraseCircleScreen(localPos, radius);
            DrawSync.Instance?.SendErase(localPos, radius);
            return;
        }

        FreeActiveLine();
        _activeStrokeId = DrawSync.Instance?.AllocStrokeId() ?? (int)Time.GetTicksMsec();
        _activeColor = MakeInkColor(BrushConfig.CurrentColor);
        _activeWidth = Math.Max(2f, BrushConfig.ClampedSize);
        _activePoints.Clear();
        _activePoints.Add(localPos);

        // Live preview only — baked on EndStroke.
        _activeLine = new Line2D
        {
            DefaultColor = _activeColor,
            Width = _activeWidth,
            Antialiased = false, // AA on bake; live line is temporary
            JointMode = Line2D.LineJointMode.Round,
            BeginCapMode = Line2D.LineCapMode.Round,
            EndCapMode = Line2D.LineCapMode.Round,
            ZIndex = 2,
        };
        _activeLine.AddPoint(localPos);
        _activeLine.AddPoint(localPos + new Vector2(0.4f, 0f));
        (_ink.Root ?? (Node)this).AddChild(_activeLine);

        DrawSync.Instance?.SendBegin(_activeStrokeId, localPos, _activeColor, _activeWidth);

        if (_strokeLogBudget > 0)
        {
            _strokeLogBudget--;
            MainFile.Logger.Info($"Stroke begin id={_activeStrokeId} w={_activeWidth:0.#}");
        }
    }

    private void EndStroke()
    {
        if (_drawing && _strokeTool == DrawTool.Brush && _activeStrokeId != 0)
        {
            DrawSync.Instance?.SendEnd(_activeStrokeId);
            if (_activePoints.Count > 0)
                _ink.StampPolyline(_activePoints, _activeColor, _activeWidth, remote: false);
        }

        FreeActiveLine();
        _drawing = false;
        _activeStrokeId = 0;
        _strokeTool = DrawTool.None;
        RefreshCursor();
    }

    private void FreeActiveLine()
    {
        if (_activeLine != null && GodotObject.IsInstanceValid(_activeLine))
            _activeLine.QueueFree();
        _activeLine = null;
        _activePoints.Clear();
    }

    public void ClearAll(bool network = true)
    {
        FreeActiveLine();
        _openRemote.Clear();
        _ink.ClearAll();
        _drawing = false;
        _activeStrokeId = 0;
        _strokeTool = DrawTool.None;
        if (network)
            DrawSync.Instance?.SendClear();
    }

    public void Teardown()
    {
        SetProcess(false);
        SetProcessInput(false);
        ClearAll(network: false);
        RestoreCursor();
        if (GodotObject.IsInstanceValid(this) && IsInsideTree())
        {
            Node? uiLayer = GetParent();
            if (uiLayer != null && uiLayer.Name == "BattleDrawUiLayer")
                uiLayer.QueueFree();
            else
                QueueFree();
        }

        if (Instance == this)
            Instance = null;
    }

    // --- multiplayer reconstruct: bake remote strokes, keep only open poly for late points ---

    public void RemoteBegin(ulong ownerId, int strokeId, Vector2 pos, Color color, float width)
    {
        _openRemote.Remove((ownerId, strokeId));
        _openRemote[(ownerId, strokeId)] = [pos];
        // Tiny seed mark
        _ink.StampPolyline([pos], MakeInkColor(color), Math.Max(2f, width), remote: true);
        // stash width/color on list via parallel dict would be cleaner — store in first point meta:
        // Use a side dictionary for style
        _remoteStyles[(ownerId, strokeId)] = (MakeInkColor(color), Math.Max(2f, width));
    }

    public void RemotePoint(ulong ownerId, int strokeId, Vector2 pos)
    {
        if (!_openRemote.TryGetValue((ownerId, strokeId), out List<Vector2>? pts))
        {
            pts = [pos];
            _openRemote[(ownerId, strokeId)] = pts;
            var (c, w) = _remoteStyles.GetValueOrDefault((ownerId, strokeId), (Colors.White, 3f));
            _ink.StampPolyline([pos], c, w, remote: true);
            return;
        }

        Vector2 prev = pts[^1];
        pts.Add(pos);
        var style = _remoteStyles.GetValueOrDefault((ownerId, strokeId), (Colors.White, 3f));
        _ink.StampSegmentScreen(prev, pos, style.Item1, style.Item2, remote: true);
    }

    public void RemoteEnd(ulong ownerId, int strokeId)
    {
        _openRemote.Remove((ownerId, strokeId));
        _remoteStyles.Remove((ownerId, strokeId));
    }

    public void RemoteErase(Vector2 pos, float radius) => _ink.EraseCircleScreen(pos, radius);

    public void RemoteClear()
    {
        _openRemote.Clear();
        _remoteStyles.Clear();
        _ink.ClearRemote();
    }

    private static Color MakeInkColor(Color c)
    {
        if (c.A < 0.5f)
            c.A = 1f;
        return c;
    }

    private string? GetBlockReason()
    {
        if (BrushToolbar.HitsPointer())
            return "toolbar";
        if (IsCardInteractionBusy())
            return "card-busy";

        Viewport? vp = GetViewport();
        if (vp == null)
            return null;

        Vector2 inkPos = GetInkMousePos(vp);
        Vector2 size = vp.GetVisibleRect().Size;
        float handTop = size.Y * (1f - HandNoDrawBandFrac);
        if (inkPos.Y >= handTop)
            return "hand-band";
        return null;
    }

    private static bool IsCardInteractionBusy()
    {
        try
        {
            NPlayerHand? hand = NCombatRoom.Instance?.Ui?.Hand;
            if (hand == null || !GodotObject.IsInstanceValid(hand))
                return false;
            if (hand.HasDraggedHolder)
                return true;
            if (hand.InCardPlay)
                return true;
        }
        catch
        {
            // ignore
        }

        return false;
    }

    private void ApplyCursorForTool(DrawTool tool)
    {
        EnsureCursorTextures();
        switch (tool)
        {
            case DrawTool.Brush when _quillTex != null:
                Input.SetCustomMouseCursor(_quillTex, Input.CursorShape.Arrow, QuillHotspot);
                _cursorOverridden = true;
                return;
            case DrawTool.Eraser when _eraserTex != null:
                Input.SetCustomMouseCursor(_eraserTex, Input.CursorShape.Arrow, new Vector2(8, 8));
                _cursorOverridden = true;
                return;
        }

        RestoreCursor();
    }

    private void RestoreCursor()
    {
        if (!_cursorOverridden)
            return;
        Input.SetCustomMouseCursor(null);
        _cursorOverridden = false;
    }

    private static void EnsureCursorTextures()
    {
        _quillTex ??= LoadTex(QuillCursorPath) ?? LoadTex(QuillTiltedPath);
        _eraserTex ??= LoadTex(EraserCursorPath);
    }

    private static Texture2D? LoadTex(string path)
    {
        try
        {
            return ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
        }
        catch
        {
            return null;
        }
    }
}
