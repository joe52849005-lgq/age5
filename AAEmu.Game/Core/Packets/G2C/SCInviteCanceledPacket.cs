using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCInviteCanceledPacket : GamePacket
{
    private readonly byte _type;

    public SCInviteCanceledPacket(byte type) : base(SCOffsets.SCInviteCanceledPacket, 5)
    {
        _type = type;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.WriteBc(_type);
        return stream;
    }
}
