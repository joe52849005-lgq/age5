using System.Collections.Generic;

using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Housing;

namespace AAEmu.Game.Core.Packets.G2C
{
    public class SCAddHousePacket(List<House> houses) : GamePacket(SCOffsets.SCAddHousePacket, 5)
    {
        public override PacketStream Write(PacketStream stream)
        {
            stream.Write((byte)houses.Count);
            foreach (var house in houses) // TODO не более 20
            {
                house.WriteInfo(stream);
            }

            return stream;
        }
    }
}
