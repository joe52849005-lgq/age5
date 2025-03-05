using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G
{
    public class CSRefreshResidentMembersPacket : GamePacket
    {
        public CSRefreshResidentMembersPacket() : base(CSOffsets.CSRefreshResidentMembersPacket, 5)
        {
        }

        public override void Read(PacketStream stream)
        {
            Logger.Debug("Entering in CSRefreshResidentMembers...");

            var zoneGroupId = stream.ReadUInt16();

            ResidentManager.Instance.UpdateResidenMemberInfo2(zoneGroupId, Connection.ActiveChar);

            Logger.Debug("CSRefreshResidentMembersPacket");
        }
    }
}
