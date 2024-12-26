using System;
using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCFamilyNameChangeNotifyPacket : GamePacket
{
    private readonly string _changeName;

    public SCFamilyNameChangeNotifyPacket(string changeName) : base(SCOffsets.SCFamilyNameChangeNotifyPacket, 5)
    {
        _changeName = changeName;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(_changeName);
        return stream;
    }
}
