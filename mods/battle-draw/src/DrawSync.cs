using Godot;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace BattleDraw;

/// <summary>
/// Multiplayer combat-draw sync (same idea as map pen): local strokes are broadcast;
/// peers reconstruct them so everyone sees the whiteboard.
/// Positions are normalized 0–1 of the viewport so resolutions can differ.
/// </summary>
public sealed class DrawSync : IDisposable
{
    public static DrawSync? Instance { get; set; }

    private readonly RunLocationTargetedMessageBuffer _buffer;
    private readonly INetGameService _net;
    private readonly ulong _localId;
    private int _nextStrokeId = 1;
    private ulong _lastPointMsec;
    private ulong _lastEraseMsec;

    public DrawSync(
        RunLocationTargetedMessageBuffer buffer,
        INetGameService net,
        ulong localId)
    {
        _buffer = buffer;
        _net = net;
        _localId = localId;
        buffer.RegisterMessageHandler<BattleDrawStrokeBeginMessage>(OnBegin);
        buffer.RegisterMessageHandler<BattleDrawStrokePointMessage>(OnPoint);
        buffer.RegisterMessageHandler<BattleDrawStrokeEndMessage>(OnEnd);
        buffer.RegisterMessageHandler<BattleDrawEraseMessage>(OnErase);
        buffer.RegisterMessageHandler<BattleDrawClearMessage>(OnClear);
        MainFile.Logger.Info("DrawSync attached (multiplayer combat doodles).");
    }

    public void Dispose()
    {
        _buffer.UnregisterMessageHandler<BattleDrawStrokeBeginMessage>(OnBegin);
        _buffer.UnregisterMessageHandler<BattleDrawStrokePointMessage>(OnPoint);
        _buffer.UnregisterMessageHandler<BattleDrawStrokeEndMessage>(OnEnd);
        _buffer.UnregisterMessageHandler<BattleDrawEraseMessage>(OnErase);
        _buffer.UnregisterMessageHandler<BattleDrawClearMessage>(OnClear);
    }

    /// <summary>True when there is more than one player in the run (co-op).</summary>
    public bool IsMultiplayer
    {
        get
        {
            try
            {
                return (RunManager.Instance?.State?.Players?.Count ?? 1) > 1;
            }
            catch
            {
                return false;
            }
        }
    }

    public int AllocStrokeId() => _nextStrokeId++;

    public void SendBegin(int strokeId, Vector2 localPos, Color color, float width)
    {
        if (!IsMultiplayer)
            return;
        Vector2 n = Normalize(localPos);
        _net.SendMessage(new BattleDrawStrokeBeginMessage
        {
            strokeId = strokeId,
            x = n.X,
            y = n.Y,
            r = color.R,
            g = color.G,
            b = color.B,
            a = color.A,
            width = width,
            Location = _buffer.CurrentLocation,
        });
        _lastPointMsec = Time.GetTicksMsec();
    }

    public void SendPoint(int strokeId, Vector2 localPos)
    {
        if (!IsMultiplayer)
            return;
        ulong now = Time.GetTicksMsec();
        // ~20 Hz like map drawing (50ms).
        if (now - _lastPointMsec < 40)
            return;
        _lastPointMsec = now;
        Vector2 n = Normalize(localPos);
        _net.SendMessage(new BattleDrawStrokePointMessage
        {
            strokeId = strokeId,
            x = n.X,
            y = n.Y,
            Location = _buffer.CurrentLocation,
        });
    }

    public void SendEnd(int strokeId)
    {
        if (!IsMultiplayer)
            return;
        _net.SendMessage(new BattleDrawStrokeEndMessage
        {
            strokeId = strokeId,
            Location = _buffer.CurrentLocation,
        });
    }

    public void SendErase(Vector2 localPos, float radius)
    {
        if (!IsMultiplayer)
            return;
        // Throttle like points (~20 Hz). Unthrottled reliable erase was a major MP hitch.
        ulong now = Time.GetTicksMsec();
        if (now - _lastEraseMsec < 50)
            return;
        _lastEraseMsec = now;
        Vector2 n = Normalize(localPos);
        Vector2 vp = ViewportSize();
        float rn = radius / Math.Max(1f, Math.Min(vp.X, vp.Y));
        _net.SendMessage(new BattleDrawEraseMessage
        {
            x = n.X,
            y = n.Y,
            radius = rn,
            Location = _buffer.CurrentLocation,
        });
    }

    public void SendClear()
    {
        if (!IsMultiplayer)
            return;
        _net.SendMessage(new BattleDrawClearMessage { Location = _buffer.CurrentLocation });
    }

    private void OnBegin(BattleDrawStrokeBeginMessage msg, ulong senderId)
    {
        if (senderId == _localId)
            return;
        DrawCanvas.Instance?.RemoteBegin(
            senderId,
            msg.strokeId,
            Denormalize(msg.x, msg.y),
            new Color(msg.r, msg.g, msg.b, msg.a),
            msg.width);
    }

    private void OnPoint(BattleDrawStrokePointMessage msg, ulong senderId)
    {
        if (senderId == _localId)
            return;
        DrawCanvas.Instance?.RemotePoint(senderId, msg.strokeId, Denormalize(msg.x, msg.y));
    }

    private void OnEnd(BattleDrawStrokeEndMessage msg, ulong senderId)
    {
        if (senderId == _localId)
            return;
        DrawCanvas.Instance?.RemoteEnd(senderId, msg.strokeId);
    }

    private void OnErase(BattleDrawEraseMessage msg, ulong senderId)
    {
        if (senderId == _localId)
            return;
        Vector2 vp = ViewportSize();
        float radius = msg.radius * Math.Min(vp.X, vp.Y);
        DrawCanvas.Instance?.RemoteErase(Denormalize(msg.x, msg.y), radius);
    }

    private void OnClear(BattleDrawClearMessage msg, ulong senderId)
    {
        if (senderId == _localId)
            return;
        DrawCanvas.Instance?.RemoteClear();
    }

    private static Vector2 ViewportSize()
    {
        Viewport? vp = (Engine.GetMainLoop() as SceneTree)?.Root?.GetViewport();
        if (vp != null)
            return vp.GetVisibleRect().Size;
        return new Vector2(1920, 1080);
    }

    private static Vector2 Normalize(Vector2 local)
    {
        Vector2 s = ViewportSize();
        return new Vector2(
            local.X / Math.Max(1f, s.X),
            local.Y / Math.Max(1f, s.Y));
    }

    private static Vector2 Denormalize(float x, float y)
    {
        Vector2 s = ViewportSize();
        return new Vector2(x * s.X, y * s.Y);
    }
}
