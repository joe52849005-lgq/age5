﻿using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.StaticValues;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSExpeditionReplyInvitationPacket : GamePacket
{
    public CSExpeditionReplyInvitationPacket() : base(CSOffsets.CSExpeditionReplyInvitationPacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        var expeditionId = (FactionsEnum)stream.ReadUInt32(); // type(id)
        var id2 = (FactionsEnum)stream.ReadUInt32(); // type(id)
        var join = stream.ReadBoolean();

        Logger.Debug($"ExpeditionReplyInvitation, Id: {expeditionId}, Id2: {id2}, Join: {join}");
        ExpeditionManager.Instance.ReplyInvite(Connection, expeditionId, id2, join);
    }
}
