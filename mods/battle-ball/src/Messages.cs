using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace BattleBall;

/// <summary>Reliable ball events (grab / throw / spawn / despawn / score).</summary>
public abstract class BallReliableMessage : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
{
    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.VeryDebug;
    public bool ShouldBuffer => true;
    public RunLocation Location { get; set; }

    public abstract void Serialize(PacketWriter writer);
    public abstract void Deserialize(PacketReader reader);
}

/// <summary>High-frequency free-ball / held-cursor state while someone is authority.</summary>
public abstract class BallUnreliableMessage : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
{
    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Unreliable;
    public LogLevel LogLevel => LogLevel.VeryDebug;
    public bool ShouldBuffer => false;
    public RunLocation Location { get; set; }

    public abstract void Serialize(PacketWriter writer);
    public abstract void Deserialize(PacketReader reader);
}

/// <summary>Player grabbed a ball (held at their cursor; follow-ups via BallStateMessage).</summary>
public sealed class BallGrabMessage : BallReliableMessage
{
    public int ballId;
    public float x;
    public float y;

    public override void Serialize(PacketWriter writer)
    {
        writer.WriteInt(ballId);
        writer.WriteFloat(x);
        writer.WriteFloat(y);
        writer.Write(Location);
    }

    public override void Deserialize(PacketReader reader)
    {
        ballId = reader.ReadInt();
        x = reader.ReadFloat();
        y = reader.ReadFloat();
        Location = reader.Read<RunLocation>();
    }
}

/// <summary>Player released a ball with a velocity.</summary>
public sealed class BallThrowMessage : BallReliableMessage
{
    public int ballId;
    public float x;
    public float y;
    public float vx;
    public float vy;

    public override void Serialize(PacketWriter writer)
    {
        writer.WriteInt(ballId);
        writer.WriteFloat(x);
        writer.WriteFloat(y);
        writer.WriteFloat(vx);
        writer.WriteFloat(vy);
        writer.Write(Location);
    }

    public override void Deserialize(PacketReader reader)
    {
        ballId = reader.ReadInt();
        x = reader.ReadFloat();
        y = reader.ReadFloat();
        vx = reader.ReadFloat();
        vy = reader.ReadFloat();
        Location = reader.Read<RunLocation>();
    }
}

/// <summary>
/// Periodic ball snapshot (~20 Hz). Used for free flight AND while held so remotes
/// track grab position (not frozen at grab point).
/// </summary>
public sealed class BallStateMessage : BallUnreliableMessage
{
    public int ballId;
    public float x;
    public float y;
    public float vx;
    public float vy;
    /// <summary>1 if the authority is currently holding the ball.</summary>
    public byte held;

    public override void Serialize(PacketWriter writer)
    {
        writer.WriteInt(ballId);
        writer.WriteFloat(x);
        writer.WriteFloat(y);
        writer.WriteFloat(vx);
        writer.WriteFloat(vy);
        writer.WriteByte(held);
        writer.Write(Location);
    }

    public override void Deserialize(PacketReader reader)
    {
        ballId = reader.ReadInt();
        x = reader.ReadFloat();
        y = reader.ReadFloat();
        vx = reader.ReadFloat();
        vy = reader.ReadFloat();
        held = reader.ReadByte();
        Location = reader.Read<RunLocation>();
    }
}

/// <summary>Someone scored a basket.</summary>
public sealed class BallScoreMessage : BallReliableMessage
{
    public int ballId;
    public int side;
    public float x;
    public float y;

    public override void Serialize(PacketWriter writer)
    {
        writer.WriteInt(ballId);
        writer.WriteInt(side);
        writer.WriteFloat(x);
        writer.WriteFloat(y);
        writer.Write(Location);
    }

    public override void Deserialize(PacketReader reader)
    {
        ballId = reader.ReadInt();
        side = reader.ReadInt();
        x = reader.ReadFloat();
        y = reader.ReadFloat();
        Location = reader.Read<RunLocation>();
    }
}

/// <summary>Spawn an extra ball at a position.</summary>
public sealed class BallSpawnMessage : BallReliableMessage
{
    public int ballId;
    public float x;
    public float y;

    public override void Serialize(PacketWriter writer)
    {
        writer.WriteInt(ballId);
        writer.WriteFloat(x);
        writer.WriteFloat(y);
        writer.Write(Location);
    }

    public override void Deserialize(PacketReader reader)
    {
        ballId = reader.ReadInt();
        x = reader.ReadFloat();
        y = reader.ReadFloat();
        Location = reader.Read<RunLocation>();
    }
}

/// <summary>Remove a ball by id.</summary>
public sealed class BallDespawnMessage : BallReliableMessage
{
    public int ballId;

    public override void Serialize(PacketWriter writer)
    {
        writer.WriteInt(ballId);
        writer.Write(Location);
    }

    public override void Deserialize(PacketReader reader)
    {
        ballId = reader.ReadInt();
        Location = reader.Read<RunLocation>();
    }
}
