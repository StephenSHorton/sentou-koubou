using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace CardRanks;

public abstract class CombineMessageBase : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
{
    public bool ShouldBroadcast => true;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    public LogLevel LogLevel => LogLevel.Debug;

    public bool ShouldBuffer => true;

    public RunLocation Location { get; set; }

    public abstract void Serialize(PacketWriter writer);

    public abstract void Deserialize(PacketReader reader);
}

public sealed class CombineCardsMessage : CombineMessageBase
{
    public ulong ownerNetId;
    public string category = "";
    public string entry = "";
    public int sacrificeRank;
    public int sacrificeUpgrade;
    public int survivorRank;
    public int survivorUpgrade;
    public int resultRank;
    public int resultUpgradeLevel;
    public int bonusRolled;

    public override void Serialize(PacketWriter writer)
    {
        writer.WriteULong(ownerNetId);
        writer.WriteString(category);
        writer.WriteString(entry);
        writer.WriteInt(sacrificeRank);
        writer.WriteInt(sacrificeUpgrade);
        writer.WriteInt(survivorRank);
        writer.WriteInt(survivorUpgrade);
        writer.WriteInt(resultRank);
        writer.WriteInt(resultUpgradeLevel);
        writer.WriteInt(bonusRolled);
        writer.Write(Location);
    }

    public override void Deserialize(PacketReader reader)
    {
        ownerNetId = reader.ReadULong();
        category = reader.ReadString();
        entry = reader.ReadString();
        sacrificeRank = reader.ReadInt();
        sacrificeUpgrade = reader.ReadInt();
        survivorRank = reader.ReadInt();
        survivorUpgrade = reader.ReadInt();
        resultRank = reader.ReadInt();
        resultUpgradeLevel = reader.ReadInt();
        bonusRolled = reader.ReadInt();
        Location = reader.Read<RunLocation>();
    }
}

public sealed class CampfireCombineResultMessage : CombineMessageBase
{
    public bool success;

    public override void Serialize(PacketWriter writer)
    {
        writer.WriteBool(success);
        writer.Write(Location);
    }

    public override void Deserialize(PacketReader reader)
    {
        success = reader.ReadBool();
        Location = reader.Read<RunLocation>();
    }
}
