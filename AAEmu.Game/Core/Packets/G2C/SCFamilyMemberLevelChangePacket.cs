using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCFamilyMemberLevelChangePacket : GamePacket
{
    private readonly uint _familyId;
    private readonly uint _memberId;
    private readonly byte _level;
    private readonly byte _heirLevel;

    public SCFamilyMemberLevelChangePacket(uint familyId, uint memberId, byte level, byte heirLevel)
        : base(SCOffsets.SCFamilyMemberLevelChangePacket, 5)
    {
        _familyId = familyId;
        _memberId = memberId;
        _level = level;
        _heirLevel = heirLevel;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(_familyId);
        stream.Write(_memberId);
        stream.Write(_level);
        stream.Write(_heirLevel);
        return stream;
    }
}
