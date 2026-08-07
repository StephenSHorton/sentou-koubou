using Godot;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace BattleBall;

/// <summary>
/// Multiplayer ball bus. Local player is authority while holding or after they throw
/// until someone else grabs; snapshots keep free-ball and held-cursor peers aligned.
/// </summary>
public sealed class BallSync : IDisposable
{
    public static BallSync? Instance { get; set; }

    private readonly RunLocationTargetedMessageBuffer _buffer;
    private readonly INetGameService _net;
    private readonly ulong _localId;
    /// <summary>Per-ball last snapshot time so multi-ball streams don't starve each other.</summary>
    private readonly Dictionary<int, ulong> _lastSnapshotByBall = new();

    public BallSync(RunLocationTargetedMessageBuffer buffer, INetGameService net, ulong localId)
    {
        _buffer = buffer;
        _net = net;
        _localId = localId;
        buffer.RegisterMessageHandler<BallGrabMessage>(OnGrab);
        buffer.RegisterMessageHandler<BallThrowMessage>(OnThrow);
        buffer.RegisterMessageHandler<BallStateMessage>(OnState);
        buffer.RegisterMessageHandler<BallScoreMessage>(OnScore);
        buffer.RegisterMessageHandler<BallSpawnMessage>(OnSpawn);
        buffer.RegisterMessageHandler<BallDespawnMessage>(OnDespawn);
        MainFile.Logger.Info("BallSync attached.");
    }

    public void Dispose()
    {
        _buffer.UnregisterMessageHandler<BallGrabMessage>(OnGrab);
        _buffer.UnregisterMessageHandler<BallThrowMessage>(OnThrow);
        _buffer.UnregisterMessageHandler<BallStateMessage>(OnState);
        _buffer.UnregisterMessageHandler<BallScoreMessage>(OnScore);
        _buffer.UnregisterMessageHandler<BallSpawnMessage>(OnSpawn);
        _buffer.UnregisterMessageHandler<BallDespawnMessage>(OnDespawn);
    }

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

    public ulong LocalId => _localId;

    public void SendGrab(int ballId, Vector2 worldPos)
    {
        if (!IsMultiplayer)
            return;
        Vector2 n = Normalize(worldPos);
        _net.SendMessage(new BallGrabMessage
        {
            ballId = ballId,
            x = n.X,
            y = n.Y,
            Location = _buffer.CurrentLocation,
        });
    }

    public void SendThrow(int ballId, Vector2 worldPos, Vector2 velocity)
    {
        if (!IsMultiplayer)
            return;
        Vector2 n = Normalize(worldPos);
        Vector2 nv = NormalizeVelocity(velocity);
        _net.SendMessage(new BallThrowMessage
        {
            ballId = ballId,
            x = n.X,
            y = n.Y,
            vx = nv.X,
            vy = nv.Y,
            Location = _buffer.CurrentLocation,
        });
    }

    public void SendState(int ballId, Vector2 worldPos, Vector2 velocity, bool held, bool force = false)
    {
        if (!IsMultiplayer)
            return;
        ulong now = Time.GetTicksMsec();
        // Held tracking a bit snappier (~30 Hz); free flight ~20 Hz.
        ulong minGap = held ? 33u : 50u;
        if (!force
            && _lastSnapshotByBall.TryGetValue(ballId, out ulong last)
            && now - last < minGap)
            return;
        _lastSnapshotByBall[ballId] = now;
        Vector2 n = Normalize(worldPos);
        Vector2 nv = NormalizeVelocity(velocity);
        _net.SendMessage(new BallStateMessage
        {
            ballId = ballId,
            x = n.X,
            y = n.Y,
            vx = nv.X,
            vy = nv.Y,
            held = held ? (byte)1 : (byte)0,
            Location = _buffer.CurrentLocation,
        });
    }

    public void SendScore(int ballId, int side, Vector2 confettiAt)
    {
        if (!IsMultiplayer)
            return;
        Vector2 n = Normalize(confettiAt);
        _net.SendMessage(new BallScoreMessage
        {
            ballId = ballId,
            side = side,
            x = n.X,
            y = n.Y,
            Location = _buffer.CurrentLocation,
        });
    }

    public void SendSpawn(int ballId, Vector2 worldPos)
    {
        if (!IsMultiplayer)
            return;
        Vector2 n = Normalize(worldPos);
        _net.SendMessage(new BallSpawnMessage
        {
            ballId = ballId,
            x = n.X,
            y = n.Y,
            Location = _buffer.CurrentLocation,
        });
    }

    public void SendDespawn(int ballId)
    {
        if (!IsMultiplayer)
            return;
        _net.SendMessage(new BallDespawnMessage
        {
            ballId = ballId,
            Location = _buffer.CurrentLocation,
        });
    }

    private void OnGrab(BallGrabMessage msg, ulong senderId)
    {
        if (senderId == _localId)
            return;
        BallWorld.Instance?.ApplyRemoteGrab(
            msg.ballId, senderId, Denormalize(new Vector2(msg.x, msg.y)));
    }

    private void OnThrow(BallThrowMessage msg, ulong senderId)
    {
        if (senderId == _localId)
            return;
        BallWorld.Instance?.ApplyRemoteThrow(
            msg.ballId,
            senderId,
            Denormalize(new Vector2(msg.x, msg.y)),
            DenormalizeVelocity(new Vector2(msg.vx, msg.vy)));
    }

    private void OnState(BallStateMessage msg, ulong senderId)
    {
        if (senderId == _localId)
            return;
        BallWorld.Instance?.ApplyRemoteState(
            msg.ballId,
            senderId,
            Denormalize(new Vector2(msg.x, msg.y)),
            DenormalizeVelocity(new Vector2(msg.vx, msg.vy)),
            held: msg.held != 0);
    }

    private void OnScore(BallScoreMessage msg, ulong senderId)
    {
        if (senderId == _localId)
            return;
        BallWorld.Instance?.ApplyRemoteScore(
            msg.ballId, msg.side, Denormalize(new Vector2(msg.x, msg.y)));
    }

    private void OnSpawn(BallSpawnMessage msg, ulong senderId)
    {
        if (senderId == _localId)
            return;
        BallWorld.Instance?.ApplyRemoteSpawn(
            msg.ballId, Denormalize(new Vector2(msg.x, msg.y)));
    }

    private void OnDespawn(BallDespawnMessage msg, ulong senderId)
    {
        if (senderId == _localId)
            return;
        BallWorld.Instance?.ApplyRemoteDespawn(msg.ballId);
    }

    private static Vector2 ViewportSize()
    {
        try
        {
            var vp = (Engine.GetMainLoop() as SceneTree)?.Root?.GetViewport();
            if (vp != null)
                return vp.GetVisibleRect().Size;
        }
        catch
        {
            // fall through
        }
        return new Vector2(1920, 1080);
    }

    private static Vector2 Normalize(Vector2 world)
    {
        Vector2 s = ViewportSize();
        if (s.X < 1f || s.Y < 1f)
            return Vector2.Zero;
        return new Vector2(world.X / s.X, world.Y / s.Y);
    }

    private static Vector2 Denormalize(Vector2 n)
    {
        Vector2 s = ViewportSize();
        return new Vector2(n.X * s.X, n.Y * s.Y);
    }

    /// <summary>Velocity relative to a 1080p-tall frame so aspect ratios stay sane.</summary>
    private static Vector2 NormalizeVelocity(Vector2 v)
    {
        Vector2 s = ViewportSize();
        float scale = Math.Max(1f, s.Y);
        return new Vector2(v.X / scale, v.Y / scale);
    }

    private static Vector2 DenormalizeVelocity(Vector2 nv)
    {
        Vector2 s = ViewportSize();
        float scale = Math.Max(1f, s.Y);
        return new Vector2(nv.X * scale, nv.Y * scale);
    }
}
