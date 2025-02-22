using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Items;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSSplitCofferItemPacket : GamePacket
{
    public CSSplitCofferItemPacket() : base(CSOffsets.CSSplitCofferItemPacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        var count = stream.ReadInt32();
        var fromItemId = stream.ReadUInt64(); // srcId
        var toItemId = stream.ReadUInt64();   // dstId

        var fromSlotType = (SlotType)stream.ReadByte(); // type
        var fromSlot = stream.ReadByte();          // index

        var toSlotType = (SlotType)stream.ReadByte(); // type
        var toSlot = stream.ReadByte();          // index

        var ownerType = stream.ReadByte();     // ownerType
        var dbId = stream.ReadUInt64();      // ownerId

        Logger.Debug($"SplitCofferItem, Item: {count} x {fromItemId} -> {toItemId}, SlotType: {fromSlotType} -> {toSlotType}, Slot: {fromSlot} -> {toSlot}, ownerType:ItemContainerDbId: {ownerType}:{dbId}");

        if (!Connection.ActiveChar.Inventory.SplitCofferItems(count, fromItemId, toItemId, fromSlotType, fromSlot, toSlotType, toSlot, ownerType, dbId))
        {
            Connection.ActiveChar.SendErrorMessage(ErrorMessageType.CannotMoveSoulboundItemToCoffer); // Not sure what error to send here
        }
    }
}
