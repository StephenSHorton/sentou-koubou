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
/// </list>
/// </summary>
public partial class DrawCanvas : Control
{
    public static DrawCanvas? Instance { get; private set; }

    private const float StrokeWidth = 3.5f;
    private const float MinPointDistance = 2.0f;
    private static readonly Color InkColor = new(1f, 0.92f, 0.35f, 0.85f); // soft yellow

    private readonly List<List<Vector2>> _strokes = new();
    private List<Vector2>? _active;
    private bool _drawing;

    public static DrawCanvas AttachTo(NCombatRoom room)
    {
        // Drop any leftover from a prior combat that didn't clean up cleanly.
        Instance?.QueueFreeSafe();

        var canvas = new DrawCanvas
        {
            Name = "BattleDrawCanvas",
            MouseFilter = MouseFilterEnum.Ignore,
            // Under Ui (hand/buttons) so ink never paints over cards.
            ZIndex = -1,
            ZAsRelative = true,
        };
        canvas.SetAnchorsPreset(LayoutPreset.FullRect);
        canvas.OffsetLeft = 0;
        canvas.OffsetTop = 0;
        canvas.OffsetRight = 0;
        canvas.OffsetBottom = 0;

        // Prefer parenting under the room so we share its lifetime; insert before Ui
        // when possible so draw order is: scene → ink → UI.
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
            _active = new List<Vector2> { pos };
            _strokes.Add(_active);
            // Do not mark as handled for Alt+LMB? We *do* want to stop card drag
            // only when intentionally drawing with Alt. Middle never conflicts.
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
        // Stop the stroke when the cursor enters the hand / play area so we never
        // scribble across someone's cards mid-drag.
        if (IsOverCardUi(pos))
        {
            _drawing = false;
            _active = null;
            return;
        }

        if (_active.Count == 0
            || _active[^1].DistanceTo(pos) >= MinPointDistance)
        {
            _active.Add(pos);
            QueueRedraw();
        }

        // Only eat motion while Alt-drawing (LMB); middle never fights the hand.
        if (mm.ButtonMask.HasFlag(MouseButtonMask.Left) && Input.IsKeyPressed(Key.Alt))
            GetViewport().SetInputAsHandled();
    }

    /// <summary>
    /// True if the pointer is over hand cards, the play-queue strip, or card previews —
    /// places where ink would both obscure and steal card UX.
    /// </summary>
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

        // Bottom strip fallback: if hand isn't ready yet, protect the lower ~28% of
        // the screen where hand cards live in the default layout.
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
        Rect2 rect = control.GetGlobalRect();
        // Slight pad so card hover lift still counts as "over cards".
        rect = rect.Grow(12f);
        return rect.HasPoint(screenPos);
    }

    public override void _Draw()
    {
        foreach (List<Vector2> stroke in _strokes)
        {
            if (stroke.Count == 1)
            {
                DrawCircle(stroke[0], StrokeWidth * 0.6f, InkColor);
                continue;
            }

            for (int i = 1; i < stroke.Count; i++)
                DrawLine(stroke[i - 1], stroke[i], InkColor, StrokeWidth, antialiased: true);
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
