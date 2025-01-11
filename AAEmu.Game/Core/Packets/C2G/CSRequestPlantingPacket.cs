using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G
{
    public class CSRequestPlantingPacket : GamePacket
    {
        public CSRequestPlantingPacket() : base(CSOffsets.CSRequestPlantingPacket, 5)
        {
        }

        public override void Read(PacketStream stream)
        {
            Logger.Debug("Entering in CSRequestPlantingPacket...");

            var objId = stream.ReadBc();
            var idx = stream.ReadInt32();

            var doodad = WorldManager.Instance.GetDoodad(objId);

            Logger.Debug($"CSRequestPlantingPacket, objId: {objId}, idx: {idx}");
        }
    }
}
