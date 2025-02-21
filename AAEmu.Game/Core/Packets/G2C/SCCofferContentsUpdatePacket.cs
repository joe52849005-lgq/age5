using System;

using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.DoodadObj;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCCofferContentsUpdatePacket : GamePacket
{
    public const byte MaxSlotsToSend = 30;
    private readonly DoodadCoffer _cofferDoodad;
    private readonly byte _firstSlot;
    private readonly byte _slotCount;

    public SCCofferContentsUpdatePacket(DoodadCoffer cofferDoodad, byte firstSlot)
        : base(SCOffsets.SCCofferContentsUpdatePacket, 5)
    {
        _cofferDoodad = cofferDoodad ?? throw new ArgumentNullException(nameof(cofferDoodad), "CofferDoodad cannot be null.");
        _firstSlot = firstSlot;

        // Calculate the last slot and the count of slots to send
        var lastSlot = (byte)(Math.Min(_firstSlot + MaxSlotsToSend, _cofferDoodad.Capacity));
        var slotCount = (byte)Math.Min(lastSlot - _firstSlot, MaxSlotsToSend);
        _slotCount = slotCount;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.WriteBc(_cofferDoodad.ObjId);              // cofferDoodadId
        stream.Write((byte)1);                            // ownerType
        stream.Write(_cofferDoodad.GetItemContainerId()); // ownerId
        stream.Write(_slotCount);                         // count, max 30

        for (byte i = 0; i < _slotCount; i++)
        {
            var slot = (byte)(_firstSlot + i);
            stream.Write(slot);                           // slotIndex

            var item = _cofferDoodad.ItemContainer.GetItemBySlot(slot);
            if (item == null)
            {
                stream.Write(0u); // Write default value when no item is found
            }
            else
            {
                stream.Write(item); // Write the item data
            }
        }

        return stream;
    }
}
