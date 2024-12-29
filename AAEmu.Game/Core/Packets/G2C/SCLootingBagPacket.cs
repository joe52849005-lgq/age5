using System.Collections.Generic;

using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Items;

namespace AAEmu.Game.Core.Packets.G2C;

class SCLootingBagPacket(List<Item> items, bool lootAll, bool autoLoot) : GamePacket(SCOffsets.SCLootingBagPacket, 5)
{
    public override PacketStream Write(PacketStream stream)
    {
        stream.Write((byte)items.Count);

        foreach (var item in items)
        {
            stream.Write(item);
        }
        stream.Write(lootAll);
        stream.Write(autoLoot);

        return stream;
    }
}
