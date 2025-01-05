﻿using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G
{
    public class CSPrepayHouseTaxPacket : GamePacket
    {
        public CSPrepayHouseTaxPacket() : base(CSOffsets.CSPrepayHouseTaxPacket, 5)
        {
        }

        public override void Read(PacketStream stream)
        {
            Logger.Debug("Entering in CSPrepayHouseTaxPacket...");

            var tl = stream.ReadUInt16();
            var ausp = stream.ReadBoolean();

            Logger.Debug("CSPrepayHouseTaxPacket, Tl: {0}, ausp: {1}", tl, ausp);

            HousingManager.Instance.PayingWeeklyTax(Connection, tl, ausp);
        }
    }
}
