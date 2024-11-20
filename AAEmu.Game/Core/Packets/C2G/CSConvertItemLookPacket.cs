using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSConvertItemLookPacket : GamePacket
{
    public CSConvertItemLookPacket() : base(CSOffsets.CSConvertItemLookPacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        var baseId = stream.ReadUInt64();
        var lookId = stream.ReadUInt64();
        var npcId = stream.ReadBc();

        ItemManager.Instance.HandleConvertItemLook(Connection.ActiveChar, baseId, lookId, npcId);
    }
}
