using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace BattleDraw;

/// <summary>
/// Full-screen combat doodle layer.
/// <list type="bullet">
/// <item>MouseFilter = Ignore so hand / end-turn / piles always receive clicks.</item>
/// <item>Only paints while middle-mouse is held, or Alt+left-drag (not plain LMB).</item>
/// <item>Refuses strokes that start or run over the hand / play-container hit areas.</item>
/// <item>Z-index sits under combat UI so cards render on top of ink.</item>
/// <item>Color/size from <see cref="BrushConfig"/> (mod settings + hotkeys).</item>
/// </list>
/// </summary>
public partial class DrawCanvas : Control
{
    public static DrawCanvas? Instance { get; private set; }

    private const float MinPointDistance = 2.0f;

    private sealed class Stroke
    {
        public required List<Vector2> Points;
        public required Color Color;
        public required float Width;
    }

    private readonly List<Stroke> _strokes = new();
    private Stroke? _active;
    private bool _drawing;

    public static DrawCanvas AttachTo(NCombatRoom room)
    {
        Instance?.QueueFreeSafe();

        var canvas = new DrawCanvas
        {
            Name = "BattleDrawCanvas",
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = -1,
            ZAsRelative = true,
        };
        canvas.SetAnchorsPreset(LayoutPreset.FullRect);
        canvas.OffsetLeft = 0;
        canvas.OffsetTop = 0;
        canvas.OffsetRight = 0;
        canvas.OffsetBottom = 0;

        if (room.Ui != null)
        {
            int uiIndex = room.Ui.GetIndex();
            room.AddChild(canvas);
            room.MoveChild(canvas, Math.Max(0, uiIndex));
        }
        else
        {
            room.AddChild(canvas);
        }

        Instance = canvas;
        MainFile.Logger.Info("Draw canvas attached to combat room.");
        return canvas;
    }

    public void ClearAll()
    {
        _strokes.Clear();
        _active = null;
        _drawing = false;
        QueueRedraw();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
        base._ExitTree();
    }

    public override void _Input(InputEvent @event)
    {
        if (!IsVisibleInTree())
            return;

        // Brush hotkeys (work in combat even when not drawing).
        if (@event is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (key.Keycode == Key.Bracketleft)
            {
                BrushConfig.NudgeSize(-0.5f);
                MainFile.Logger.Info($"Brush size {BrushConfig.ClampedSize:0.#}");
                GetViewport().SetInputAsHandled();
                return;
            }
            if (key.Keycode == Key.Bracketright)
            {
                BrushConfig.NudgeSize(0.5f);
                MainFile.Logger.Info($"Brush size {BrushConfig.ClampedSize:0.#}");
                GetViewport().SetInputAsHandled();
                return;
            }
            if (key.Keycode == Key.Semicolon)
            {
                BrushConfig.CycleColor(-1);
                MainFile.Logger.Info($"Brush color {BrushConfig.ColorPreset}");
                GetViewport().SetInputAsHandled();
                return;
            }
            if (key.Keycode == Key.Apostrophe)
            {
                BrushConfig.CycleColor(1);
                MainFile.Logger.Info($"Brush color {BrushConfig.ColorPreset}");
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        switch (@event)
        {
            case InputEventMouseButton mb:
                HandleMouseButton(mb);
                break;
            case InputEventMouseMotion mm when _drawing:
                HandleMouseMotion(mm);
                break;
        }
    }

    private void HandleMouseButton(InputEventMouseButton mb)
    {
        bool drawButton =
            mb.ButtonIndex == MouseButton.Middle
            || (mb.ButtonIndex == MouseButton.Left && mb.AltPressed);

        if (!drawButton)
            return;

        if (mb.Pressed)
        {
            Vector2 pos = mb.Position;
            if (IsOverCardUi(pos))
                return;

            _drawing = true;
            _active = new Stroke
            {
                Points = new List<Vector2> { pos },
                Color = BrushConfig.CurrentColor,
                Width = BrushConfig.ClampedSize,
            };
            _strokes.Add(_active);
            if (mb.ButtonIndex == MouseButton.Left && mb.AltPressed)
                GetViewport().SetInputAsHandled();
            QueueRedraw();
        }
        else if (_drawing)
        {
            _drawing = false;
            _active = null;
            if (mb.ButtonIndex == MouseButton.Left && mb.AltPressed)
                GetViewport().SetInputAsHandled();
        }
    }

    private void HandleMouseMotion(InputEventMouseMotion mm)
    {
        if (_active == null)
            return;

        Vector2 pos = mm.Position;
        if (IsOverCardUi(pos))
        {
            _drawing = false;
            _active = null;
            return;
        }

        if (_active.Points.Count == 0
            || _active.Points[^1].DistanceTo(pos) >= MinPointDistance)
        {
            _active.Points.Add(pos);
            QueueRedraw();
        }

        if (mm.ButtonMask.HasFlag(MouseButtonMask.Left) && Input.IsKeyPressed(Key.Alt))
            GetViewport().SetInputAsHandled();
    }

    private static bool IsOverCardUi(Vector2 screenPos)
    {
        NCombatRoom? room = NCombatRoom.Instance;
        NCombatUi? ui = room?.Ui;
        if (ui == null)
            return false;

        if (ControlContainsScreenPoint(ui.Hand, screenPos))
            return true;
        if (ControlContainsScreenPoint(ui.PlayContainer, screenPos))
            return true;
        if (ControlContainsScreenPoint(ui.PlayQueue, screenPos))
            return true;
        if (ControlContainsScreenPoint(ui.CardPreviewContainer, screenPos))
            return true;
        if (ControlContainsScreenPoint(ui.MessyCardPreviewContainer, screenPos))
            return true;

        if (ui.Hand == null || !ui.Hand.IsVisibleInTree())
        {
            Rect2 vp = ui.GetViewport().GetVisibleRect();
            if (screenPos.Y >= vp.Size.Y * 0.72f)
                return true;
        }

        return false;
    }

    private static bool ControlContainsScreenPoint(Control? control, Vector2 screenPos)
    {
        if (control == null || !control.IsVisibleInTree())
            return false;
        Rect2 rect = control.GetGlobalRect().Grow(12f);
        return rect.HasPoint(screenPos);
    }

    public override void _Draw()
    {
        foreach (Stroke stroke in _strokes)
        {
            if (stroke.Points.Count == 1)
            {
                DrawCircle(stroke.Points[0], stroke.Width * 0.55f, stroke.Color);
                continue;
            }

            for (int i = 1; i < stroke.Points.Count; i++)
                DrawLine(
                    stroke.Points[i - 1],
                    stroke.Points[i],
                    stroke.Color,
                    stroke.Width,
                    antialiased: true);
        }
    }

    private void QueueFreeSafe()
    {
        if (IsInsideTree())
            QueueFree();
        else
            Free();
    }
}
