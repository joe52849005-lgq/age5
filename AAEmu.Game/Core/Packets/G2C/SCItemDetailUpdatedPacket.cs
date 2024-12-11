using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Items;
namespace AAEmu.Game.Core.Packets.G2C;
public class SCItemDetailUpdatedPacket : GamePacket
{
    private readonly Item _item;
 
    public SCItemDetailUpdatedPacket(Item item)
        : base(SCOffsets.SCItemDetailUpdatedPacket, 5)
    {
        _item = item;
    }
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(_item.Id);
        stream.Write((byte)_item.SlotType);
        stream.Write((byte)_item.Slot);

        var details = new PacketStream();
        details.Write((byte)_item.DetailType);
        _item.WriteDetails(details);
        stream.Write(details, false);

        return stream;
    }
}
