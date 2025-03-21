﻿using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSReadMailPacket : GamePacket
{
    public CSReadMailPacket() : base(CSOffsets.CSReadMailPacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        Logger.Debug("Entering in CSReadMailPacket...");

        var isSent = stream.ReadBoolean();
        var mailId = stream.ReadInt64();

        Logger.Debug("ReadMail, Id: {0}, isSent: {1}", mailId, isSent);
        Connection.ActiveChar.Mails.ReadMail(isSent, mailId);
    }
}
