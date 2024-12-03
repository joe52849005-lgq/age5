using AAEmu.Commons.Network;

namespace AAEmu.Game.Models.Game.Items.Actions;

public class ItemUpdateRepair : ItemTask
{
    private readonly Item _item;
    private readonly uint _appearanceTemplateId;
    private readonly uint[] _additionalDetail;

    public ItemUpdateRepair(Item item)
    {
        _type = ItemAction.UpdateDetail; // 9
        _item = item;
        _tLogt = SetTlogT(_type, SlotType.Bag); // set tLogt by ItemAction value

        _appearanceTemplateId = item.AppearanceTemplateId;
        _additionalDetail = item.AdditionalDetails;
    }

    public override PacketStream Write(PacketStream stream)
    {
        base.Write(stream);

        stream.Write((byte)_item.SlotType);
        stream.Write((byte)_item.Slot);
        stream.Write(_item.Id);

        var details = new PacketStream();
        details.Write((byte)_item.DetailType);

        details.Write(_appearanceTemplateId); // added for normal operation of repairing objects and for transformation

        _item.WriteDetails(details, false);
        details.Write(new byte[23]); // added for normal synthesis operation

        // TODO ~19 Additional bytes
        details.Write(_additionalDetail[0]);
        details.Write(_additionalDetail[1]);
        details.Write(_additionalDetail[2]); // Tempering effect of Ephen cubes
        details.Write(_additionalDetail[3]); // RemainingExperience
        for (var i = 4; i < 9; i++)
        {
            details.Write(_additionalDetail[i]); // 15 Additional Effects
        }

        stream.Write((short)128);
        stream.Write(details, false);
        stream.Write(new byte[128 - details.Count]);

        return stream;
    }
}
