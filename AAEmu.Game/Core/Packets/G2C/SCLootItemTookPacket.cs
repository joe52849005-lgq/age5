﻿using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Items.Loots;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCLootItemTookPacket(uint itemTemplateId, ushort itemIndex, LootOwnerType lootOwnerType, uint lootOwnerId, int count)
    : GamePacket(SCOffsets.SCLootItemTookPacket, 5)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(itemTemplateId);
        #region iid
        stream.Write(itemIndex);
        stream.Write((ushort)lootOwnerType);
        stream.WriteBc(lootOwnerId);
        stream.Write((byte)0);
        #endregion
        stream.Write(count);
        return stream;
    }
}
