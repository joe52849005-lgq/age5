using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Items;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCItemChangeMappingResultPacket : GamePacket
{
    private readonly Item _item1;
    private readonly Item _item2;
    private readonly uint _grade;
    private readonly bool _result;

    public SCItemChangeMappingResultPacket(Item item1, Item item2, uint grade, bool result)
        : base(SCOffsets.SCItemChangeMappingResultPacket, 5)
    {
        _item1 = item1;
        _item2 = item2;
        _grade = grade;
        _result = result;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(_item1);
        stream.Write(_item2);
        stream.Write(_grade);
        stream.Write(_result);
        return stream;
    }
}
