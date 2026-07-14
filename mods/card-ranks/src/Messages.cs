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

/// <summary>
/// Combine three matching cards: keep survivor, remove two sacrifices.
/// </summary>
public sealed class CombineCardsMessage : CombineMessageBase
{
    public ulong ownerNetId;
    public string category = "";
    public string entry = "";
    public int sacrifice1Rank;
    public int sacrifice1Upgrade;
    public int sacrifice2Rank;
    public int sacrifice2Upgrade;
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
        writer.WriteInt(sacrifice1Rank);
        writer.WriteInt(sacrifice1Upgrade);
        writer.WriteInt(sacrifice2Rank);
        writer.WriteInt(sacrifice2Upgrade);
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
        sacrifice1Rank = reader.ReadInt();
        sacrifice1Upgrade = reader.ReadInt();
        sacrifice2Rank = reader.ReadInt();
        sacrifice2Upgrade = reader.ReadInt();
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
