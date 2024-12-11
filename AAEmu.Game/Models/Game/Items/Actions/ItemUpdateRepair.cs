using AAEmu.Commons.Network;

namespace AAEmu.Game.Models.Game.Items.Actions;

public class ItemUpdateRepair : ItemTask
{
    private readonly Item _item;

    public ItemUpdateRepair(Item item)
    {
        _type = ItemAction.UpdateDetail; // 9
        _item = item;
        _tLogt = SetTlogT(_type, SlotType.Bag); // set tLogt by ItemAction value
    }

    public override PacketStream Write(PacketStream stream)
    {
        base.Write(stream);

        stream.Write((byte)_item.SlotType);
        stream.Write((byte)_item.Slot);
        stream.Write(_item.Id);

        var details = new PacketStream();
        details.Write((byte)_item.DetailType);

        //details.Write(_appearanceTemplateId); // added for normal operation of repairing objects and for transformation
        details.Write(_item.GemIds[0]);  // added for normal operation of repairing objects and for transformation
        
        details.Write(_item.Durability); // durability
        details.Write((short)0);         // unk
        
        details.Write(_item.GemIds[1]);  // Luna Gem, TemplateId EnchantingGem - Позволяет зачаровать предмет снаряжения.
        details.Write(_item.GemIds[2]);  // Tempering

        details.Write(0);  //
        details.Write(0);  //

        details.Write(_item.GemIds[4]);  // 1 crescent stone, TemplateId Socket - Позволяет придать предмету снаряжения дополнительные свойства.
        details.Write(_item.GemIds[5]);  // 2 crescent stone
        details.Write(_item.GemIds[6]);  // 3 crescent stone
        details.Write(_item.GemIds[7]);  // 4 crescent stone
        details.Write(_item.GemIds[8]);  // 5 crescent stone
        details.Write(_item.GemIds[9]);  // 6 crescent stone
        details.Write(_item.GemIds[10]); // 7 crescent stone
        details.Write(_item.GemIds[11]); // 8 crescent stone
        details.Write(_item.GemIds[12]); // 9 crescent stone

        details.Write(0);  //

        details.Write(_item.GemIds[3]);  // RemainingExperience

        details.Write(_item.GemIds[13]); // 5 Additional Effects
        details.Write(_item.GemIds[14]); //
        details.Write(_item.GemIds[15]); //
        details.Write(_item.GemIds[16]); //
        details.Write(_item.GemIds[17]); //

        stream.Write((short)128);
        stream.Write(details, false);
        stream.Write(new byte[128 - details.Count]);

        return stream;
    }
}
