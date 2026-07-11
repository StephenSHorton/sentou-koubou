using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace TradingPost;

/// <summary>
/// Base for all Trading Post messages: reliable, broadcast to every peer, buffered
/// per run-location so peers who haven't reached the shop yet process them on arrival.
/// </summary>
public abstract class TradeMessageBase : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
{
    public bool ShouldBroadcast => true;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    public LogLevel LogLevel => LogLevel.Debug;

    public bool ShouldBuffer => true;

    public RunLocation Location { get; set; }

    public abstract void Serialize(PacketWriter writer);

    public abstract void Deserialize(PacketReader reader);
}

/// <summary>Sender gives <see cref="amount" /> gold to <see cref="targetNetId" />. No strings attached.</summary>
public class GiftGoldMessage : TradeMessageBase
{
    public ulong targetNetId;

    public int amount;

    public override void Serialize(PacketWriter writer)
    {
        writer.WriteULong(targetNetId);
        writer.WriteInt(amount);
        writer.Write(Location);
    }

    public override void Deserialize(PacketReader reader)
    {
        targetNetId = reader.ReadULong();
        amount = reader.ReadInt();
        Location = reader.Read<RunLocation>();
    }
}

/// <summary>Sender gives one card from their deck to <see cref="targetNetId" />. No strings attached.</summary>
public class GiftCardMessage : TradeMessageBase
{
    public ulong targetNetId;

    public string category = "";

    public string entry = "";

    public int upgradeLevel;

    public override void Serialize(PacketWriter writer)
    {
        writer.WriteULong(targetNetId);
        writer.WriteString(category);
        writer.WriteString(entry);
        writer.WriteInt(upgradeLevel);
        writer.Write(Location);
    }

    public override void Deserialize(PacketReader reader)
    {
        targetNetId = reader.ReadULong();
        category = reader.ReadString();
        entry = reader.ReadString();
        upgradeLevel = reader.ReadInt();
        Location = reader.Read<RunLocation>();
    }
}

/// <summary>
/// Sender asks <see cref="targetNetId" /> for one of their relics, offering ALL of the
/// sender's gold in return. Only the target acts on this (shows an accept/decline prompt).
/// </summary>
public class RelicRequestMessage : TradeMessageBase
{
    public ulong targetNetId;

    public string category = "";

    public string entry = "";

    public override void Serialize(PacketWriter writer)
    {
        writer.WriteULong(targetNetId);
        writer.WriteString(category);
        writer.WriteString(entry);
        writer.Write(Location);
    }

    public override void Deserialize(PacketReader reader)
    {
        targetNetId = reader.ReadULong();
        category = reader.ReadString();
        entry = reader.ReadString();
        Location = reader.Read<RunLocation>();
    }
}

/// <summary>
/// The relic owner's answer. If accepted, every client transfers the relic to
/// <see cref="requesterNetId" /> and burns all of the requester's gold.
/// </summary>
public class RelicResponseMessage : TradeMessageBase
{
    public ulong requesterNetId;

    public string category = "";

    public string entry = "";

    public bool accepted;

    public override void Serialize(PacketWriter writer)
    {
        writer.WriteULong(requesterNetId);
        writer.WriteString(category);
        writer.WriteString(entry);
        writer.WriteBool(accepted);
        writer.Write(Location);
    }

    public override void Deserialize(PacketReader reader)
    {
        requesterNetId = reader.ReadULong();
        category = reader.ReadString();
        entry = reader.ReadString();
        accepted = reader.ReadBool();
        Location = reader.Read<RunLocation>();
    }
}
