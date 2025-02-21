using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G
{
    public class CSAskHeirLevelUpPacket : GamePacket
    {
        public CSAskHeirLevelUpPacket() : base(CSOffsets.CSAskHeirLevelUpPacket, 5)
        {
        }

        public override void Read(PacketStream stream)
        {
            Logger.Debug("CSAskHeirLevelUpPacket");
            
            Connection.ActiveChar.CheckHeirLevelUp();
        }
    }
}
