using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C
{
    public class SCAuction_Packet_0x1AE : GamePacket
    {
        private readonly uint _id;
        private readonly byte _type;
        private readonly int _moneyAmount;

        public SCAuction_Packet_0x1AE(uint id, byte type, int moneyAmount) : base(SCOffsets.SCAuction_Packet_0x1AE, 5)
        {
            _id = id;
            _type = type;
            _moneyAmount = moneyAmount;
        }

        public override PacketStream Write(PacketStream stream)
        {
            stream.Write(_id);
            stream.Write(_type);
            stream.Write(_moneyAmount);

            return stream;
        }
    }
}
