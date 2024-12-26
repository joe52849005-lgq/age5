using System;
using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCSetInstantTimePacket : GamePacket
{
    private readonly byte _type;

    public SCSetInstantTimePacket(byte type, DateTime time) : base(SCOffsets.SCSetInstantTimePacket, 5)
    {
        _type = type;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.WriteBc(_type);
        return stream;
    }
}
