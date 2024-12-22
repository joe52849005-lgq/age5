using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Items;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCLinkTextForEnchantBroadcastPacket : GamePacket
{
    private readonly string _charName;
    private readonly byte _result;
    private readonly Item _item;
    private readonly byte _initialGrade;
    private readonly byte _itemGrade;

    public SCLinkTextForEnchantBroadcastPacket(string charName, byte result, Item item, byte initialGrade, byte itemGrade)
        : base(SCOffsets.SCLinkTextForEnchantBroadcastPacket, 5)
    {
        _charName = charName;
        _result = result;
        _item = item;
        _initialGrade = initialGrade;
        _itemGrade = itemGrade;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(_charName);
        stream.Write(_result);
        stream.Write(_item);
        stream.Write(_initialGrade);
        stream.Write(_itemGrade);

        return stream;
    }
}
