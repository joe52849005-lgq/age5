using System;
using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCFamilyExpChangeNotifyPacket : GamePacket
{
    private readonly uint _familyId;
    private readonly uint _ownerId;
    private readonly int _level;
    private readonly int _exp;

    public SCFamilyExpChangeNotifyPacket(uint familyId, uint ownerId, int level, int exp)
        : base(SCOffsets.SCFamilyExpChangeNotifyPacket, 5)
    {
        _familyId = familyId;
        _ownerId = ownerId;
        _level = level;
        _exp = exp;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(_familyId);
        stream.Write(_ownerId);
        stream.Write(_level);
        stream.Write(_exp);
        return stream;
    }
}
