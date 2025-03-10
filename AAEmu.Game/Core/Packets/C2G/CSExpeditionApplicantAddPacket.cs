﻿using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.StaticValues;

namespace AAEmu.Game.Core.Packets.C2G
{
    public class CSExpeditionApplicantAddPacket : GamePacket
    {
        public CSExpeditionApplicantAddPacket() : base(CSOffsets.CSExpeditionApplicantAddPacket, 5)
        {
        }

        public override void Read(PacketStream stream)
        {
            var expeditionId = (FactionsEnum)stream.ReadUInt32(); // type(id)
            var memo = stream.ReadString();

            Logger.Debug($"CSExpeditionApplicantAddPacket: character={Connection.ActiveChar.Name}:{Connection.ActiveChar.Id}, expeditionId={expeditionId}, memo={memo}");

            ExpeditionManager.Instance.AddPretender(Connection.ActiveChar, expeditionId, memo);
        }
    }
}
