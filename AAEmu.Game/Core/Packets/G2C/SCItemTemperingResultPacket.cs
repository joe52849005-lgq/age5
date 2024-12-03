using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Items;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCItemTemperingResultPacket : GamePacket
{
    private readonly bool _result;
    private readonly Item _item;
    private readonly uint _type1;
    private readonly ushort _type2;
    private readonly ushort _type3;

    public SCItemTemperingResultPacket(bool result, Item item, uint type1, ushort type2, ushort type3)
        : base(SCOffsets.SCItemTemperingResultPacket, 5)
    {
        _result = result;
        _item = item;
        _type1 = type1;
        _type2 = type2;
        _type3 = type3;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(_result);
        stream.Write(_item);
        stream.Write(_type1);
        stream.Write(_type2);
        stream.Write(_type3);
        return stream;
    }
}
