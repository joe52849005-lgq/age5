using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Core.Packets.G2C;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSCopyBlessUthstinPagePacket : GamePacket
{
    public CSCopyBlessUthstinPagePacket() : base(CSOffsets.CSCopyBlessUthstinPagePacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        Logger.Debug("CSCopyBlessUthstinPagePacket");

        var srcPageIndex = stream.ReadUInt32();
        var dstPageIndex = stream.ReadUInt32();

        var character = Connection.ActiveChar;
        var stats = character.Stats.GetStatsByPageIndex(srcPageIndex);
        var normalApplyCount = character.Stats.GetApplyNormalCountByPageIndex(srcPageIndex);
        var specialApplyCount = character.Stats.GetApplySpecialCountByPageIndex(srcPageIndex);
        character.Stats.UpdateStatsByPageIndex(dstPageIndex, stats, normalApplyCount, specialApplyCount);

        character.SendPacket(new SCBlessUthstinCopyPagePacket(
            character.ObjId,
            true,
            dstPageIndex,
            stats,
            normalApplyCount,
            specialApplyCount
        ));
    }
}
