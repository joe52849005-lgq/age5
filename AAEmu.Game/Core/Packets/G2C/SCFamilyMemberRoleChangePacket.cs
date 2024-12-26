using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCFamilyMemberRoleChangePacket : GamePacket
{
    private readonly uint _familyId;
    private readonly uint _oldOwnerId;
    private readonly uint _newOwnerId;

    public SCFamilyMemberRoleChangePacket(uint familyId, uint oldOwnerId, uint newOwnerId) : base(SCOffsets.SCFamilyMemberRoleChangePacket, 5)
    {
        // TODO уточнить что за пакет
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
