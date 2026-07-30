using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace CharacterCursors;

/// <summary>Broadcast local cursor tint so teammates see your custom color (cosmetic).</summary>
public sealed class CursorColorMessage : INetMessage, IPacketSerializable
{
    public float r, g, b, a;
    /// <summary>When false, peers should fall back to character NameColor.</summary>
    public bool useCustom;

    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.VeryDebug;
    public bool ShouldBuffer => false;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteFloat(r);
        writer.WriteFloat(g);
        writer.WriteFloat(b);
        writer.WriteFloat(a);
        writer.WriteBool(useCustom);
    }

    public void Deserialize(PacketReader reader)
    {
        r = reader.ReadFloat();
        g = reader.ReadFloat();
        b = reader.ReadFloat();
        a = reader.ReadFloat();
        useCustom = reader.ReadBool();
    }
}
