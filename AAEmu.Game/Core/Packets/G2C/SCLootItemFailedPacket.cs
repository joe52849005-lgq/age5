using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Items.Loots;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCLootItemFailedPacket(ErrorMessageType errorMessage, LootOwnerType lootOwnerType, uint lootOwnerObjId, ushort itemIndex, uint itemTemplateId, uint objId)
    : GamePacket(SCOffsets.SCLootItemFailedPacket, 5)
{
    private readonly int _errorMessage = (int)errorMessage;

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(_errorMessage);
        #region iid
        stream.Write(itemIndex);
        stream.Write((ushort)lootOwnerType);
        stream.WriteBc(lootOwnerObjId);
        stream.Write((byte)0);
        #endregion
        stream.Write(itemTemplateId);
        stream.WriteBc(objId);
        return stream;
    }
}
