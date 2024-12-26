using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSFamilyChangeMemberRolePacket : GamePacket
{
    public CSFamilyChangeMemberRolePacket() : base(CSOffsets.CSFamilyChangeMemberRolePacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        var type1 = stream.ReadUInt32();
        var type2 = stream.ReadUInt32();

        Logger.Debug($"CSFamilyChangeMemberRole, type1: {type1}, type2: {type1}");

        //FamilyManager.Instance.ReplyToInvite(invitorId, Connection.ActiveChar, join, role);
    }
}
