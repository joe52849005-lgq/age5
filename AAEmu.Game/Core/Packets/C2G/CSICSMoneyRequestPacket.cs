﻿using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Core.Packets.G2C;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSICSMoneyRequestPacket : GamePacket
{
    public CSICSMoneyRequestPacket() : base(CSOffsets.CSICSMoneyRequestPacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        // Empty struct
        Logger.Warn("ICSMoneyRequest");

        var points = AccountManager.Instance.GetAccountDetails(Connection.AccountId);
        Connection.SendPacket(new SCICSCashPointPacket(points.Credits));
    }
}
