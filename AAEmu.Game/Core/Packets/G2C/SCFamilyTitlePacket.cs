using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCFamilyTitlePacket : GamePacket
{
    private readonly uint _unitId;
    private readonly byte _role;
    private readonly string _owner;
    private readonly string _title;
    private readonly string _name;

    public SCFamilyTitlePacket(uint unitId, byte role, string owner, string title, string name) : base(SCOffsets.SCFamilyTitlePacket, 5)
    {
        _unitId = unitId;
        _role = role;
        _owner = owner;
        _title = title;
        _name = name;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.WriteBc(_unitId);
        stream.Write(_role);
        stream.Write(_owner); // TODO max length 128
        stream.Write(_title); // TODO max length 104
        stream.Write(_name); // TODO max length 256
        return stream;
    }
}
