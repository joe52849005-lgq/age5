using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G
{
    public class CSBagHandleSelectiveItemsPacket : GamePacket
    {
        public CSBagHandleSelectiveItemsPacket() : base(CSOffsets.CSBagHandleSelectiveItemsPacket, 5)
        {
        }

        public override void Read(PacketStream stream)
        {
            Logger.Debug("CSBagHandleSelectiveItemsPacket");

            var slotType = stream.ReadByte();
            var slotIndex = stream.ReadByte();
            var data = stream.ReadBytes();

            ItemManager.Instance.HandleSelectiveItems(Connection.ActiveChar, slotType, slotIndex, data);
        }
    }
}
