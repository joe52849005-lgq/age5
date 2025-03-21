﻿using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCFamilyOwnerChangedPacket : GamePacket
{
    private readonly uint _familyId;
    private readonly uint _oldOwnerId;
    private readonly uint _newOwnerId;

    public SCFamilyOwnerChangedPacket(uint familyId, uint oldOwnerId, uint newOwnerId) : base(SCOffsets.SCFamilyOwnerChangedPacket, 5)
    {
        _familyId = familyId;
        _oldOwnerId = oldOwnerId;
        _newOwnerId = newOwnerId;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(_familyId);
        stream.Write(_oldOwnerId);
        stream.Write(_newOwnerId);
        return stream;
    }
}
