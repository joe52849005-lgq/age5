using System.Xml.Linq;
using System;

using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game;

using Mysqlx.Expr;

using static Mysqlx.Notice.Warning.Types;
using AAEmu.Game.Core.Packets.G2C;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSFamilyOpenIncreaseMemberPacket : GamePacket
{
    public CSFamilyOpenIncreaseMemberPacket() : base(CSOffsets.CSFamilyOpenIncreaseMemberPacket, 5)
    {
    }
    public override void Read(PacketStream stream)
    {
        // empty body

        Logger.Debug($"CSFamilyOpenIncreaseMember");

        FamilyManager.Instance.IncreaseFamilyMembers(Connection.ActiveChar);
    }
}
