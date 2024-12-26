using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCFamilyNameChangedPacket : GamePacket
{
    private readonly uint _familyId;
    private readonly uint _ownerId;
    private readonly string _newName;

    public SCFamilyNameChangedPacket(uint familyId, uint ownerId, string newName) : base(SCOffsets.SCFamilyNameChangedPacket, 5)
    {
        _familyId = familyId;
        _ownerId = ownerId;
        _newName = newName;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.WriteBc(_familyId);
        stream.Write(_ownerId);
        stream.Write(_newName);
        return stream;
    }
}
