﻿using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G
{
    public class CSSetHouseAllowRecoverPacket : GamePacket
    {
        public CSSetHouseAllowRecoverPacket() : base(CSOffsets.CSSetHouseAllowRecoverPacket, 5)
        {
        }

        public override void Read(PacketStream stream)
        {
            var houseId = stream.ReadUInt16();  // tl
            Logger.Debug("CSSetHouseAllowRecoverPacket, houseId: {0}", houseId);
            HousingManager.Instance.HousingToggleAllowRecover(Connection.ActiveChar, houseId);
        }
    }
}
