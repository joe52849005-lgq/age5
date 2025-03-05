using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Core.Packets.G2C;

namespace AAEmu.Game.Core.Packets.C2G
{
    public class CSRequestHousingTradeListPacket : GamePacket
    {
        public CSRequestHousingTradeListPacket() : base(CSOffsets.CSRequestHousingTradeListPacket, 5)
        {
        }

        public override void Read(PacketStream stream)
        {
            Logger.Debug("Entering in CSRequestHousingTradeList...");

            var zoneGroupId = stream.ReadUInt16();
            
            //ResidentManager.Instance.UpdateResidenMemberInfo2(zoneGroupId, Connection.ActiveChar);

            Connection.ActiveChar.BroadcastPacket(new SCHousingTradeListPacket(0, true, true), true);

            Logger.Debug("CSRequestHousingTradeListPacket");
        }
    }
}
