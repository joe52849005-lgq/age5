using AAEmu.Commons.Network;

namespace AAEmu.Game.Models.Game.Items.Actions;

public class ItemUpdateRepair : ItemTask
{
    private readonly Item _item;
    private readonly uint _templateId;

    public ItemUpdateRepair(Item item, uint templateId = 0)
    {
        _type = ItemAction.UpdateDetail; // 9
        _item = item;
        _templateId = templateId;
        _tLogt = SetTlogT(_type, SlotType.Bag); // установим tLogt по значению ItemAction
    }

    public override PacketStream Write(PacketStream stream)
    {
        base.Write(stream);

        stream.Write((byte)_item.SlotType);
        stream.Write((byte)_item.Slot);
        stream.Write(_item.Id);

        var details = new PacketStream();
        details.Write((byte)_item.DetailType);

        details.Write(_templateId); // добавил для нормальной работы починки предметов и для трансформации

        _item.WriteDetails(details);

        stream.Write((short)128);
        stream.Write(details, false);
        stream.Write(new byte[128 - details.Count]);

        return stream;
    }
}
