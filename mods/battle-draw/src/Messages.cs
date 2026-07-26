using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace BattleDraw;

/// <summary>Reliable combat-draw messages (begin / end / clear / erase).</summary>
public abstract class BattleDrawReliableMessage : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
{
    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.VeryDebug;
    public bool ShouldBuffer => true;
    public RunLocation Location { get; set; }

    public abstract void Serialize(PacketWriter writer);
    public abstract void Deserialize(PacketReader reader);
}

/// <summary>High-frequency stroke points — unreliable like map drawing.</summary>
public abstract class BattleDrawUnreliableMessage : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
{
    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Unreliable;
    public LogLevel LogLevel => LogLevel.VeryDebug;
    public bool ShouldBuffer => false;
    public RunLocation Location { get; set; }

    public abstract void Serialize(PacketWriter writer);
    public abstract void Deserialize(PacketReader reader);
}

public sealed class BattleDrawStrokeBeginMessage : BattleDrawReliableMessage
{
    public int strokeId;
    public float x;
    public float y;
    public float r, g, b, a;
    public float width;

    public override void Serialize(PacketWriter writer)
    {
        writer.WriteInt(strokeId);
        writer.WriteFloat(x);
        writer.WriteFloat(y);
        writer.WriteFloat(r);
        writer.WriteFloat(g);
        writer.WriteFloat(b);
        writer.WriteFloat(a);
        writer.WriteFloat(width);
        writer.Write(Location);
    }

    public override void Deserialize(PacketReader reader)
    {
        strokeId = reader.ReadInt();
        x = reader.ReadFloat();
        y = reader.ReadFloat();
        r = reader.ReadFloat();
        g = reader.ReadFloat();
        b = reader.ReadFloat();
        a = reader.ReadFloat();
        width = reader.ReadFloat();
        Location = reader.Read<RunLocation>();
    }
}

public sealed class BattleDrawStrokePointMessage : BattleDrawUnreliableMessage
{
    public int strokeId;
    public float x;
    public float y;

    public override void Serialize(PacketWriter writer)
    {
        writer.WriteInt(strokeId);
        writer.WriteFloat(x);
        writer.WriteFloat(y);
        writer.Write(Location);
    }

    public override void Deserialize(PacketReader reader)
    {
        strokeId = reader.ReadInt();
        x = reader.ReadFloat();
        y = reader.ReadFloat();
        Location = reader.Read<RunLocation>();
    }
}

public sealed class BattleDrawStrokeEndMessage : BattleDrawReliableMessage
{
    public int strokeId;

    public override void Serialize(PacketWriter writer)
    {
        writer.WriteInt(strokeId);
        writer.Write(Location);
    }

    public override void Deserialize(PacketReader reader)
    {
        strokeId = reader.ReadInt();
        Location = reader.Read<RunLocation>();
    }
}

public sealed class BattleDrawEraseMessage : BattleDrawReliableMessage
{
    public float x;
    public float y;
    public float radius;

    public override void Serialize(PacketWriter writer)
    {
        writer.WriteFloat(x);
        writer.WriteFloat(y);
        writer.WriteFloat(radius);
        writer.Write(Location);
    }

    public override void Deserialize(PacketReader reader)
    {
        x = reader.ReadFloat();
        y = reader.ReadFloat();
        radius = reader.ReadFloat();
        Location = reader.Read<RunLocation>();
    }
}

public sealed class BattleDrawClearMessage : BattleDrawReliableMessage
{
    public override void Serialize(PacketWriter writer) => writer.Write(Location);

    public override void Deserialize(PacketReader reader) => Location = reader.Read<RunLocation>();
}
