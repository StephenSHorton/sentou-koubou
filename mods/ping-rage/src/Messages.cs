using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace PingRage;

/// <summary>
/// Carries the chosen ping one-liner index + rage so every peer shows the same bubble.
/// Vanilla <c>EndTurnPingMessage</c> is empty — phrase choice was local-only before.
/// </summary>
public sealed class PingRageBubbleMessage : INetMessage, IPacketSerializable
{
    public int lineIndex;
    public float rage;

    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.VeryDebug;
    public bool ShouldBuffer => false;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(lineIndex);
        writer.WriteFloat(rage);
    }

    public void Deserialize(PacketReader reader)
    {
        lineIndex = reader.ReadInt();
        rage = reader.ReadFloat();
    }
}
