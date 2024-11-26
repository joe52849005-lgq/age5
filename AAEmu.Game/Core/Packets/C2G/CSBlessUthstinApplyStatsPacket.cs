using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Core.Packets.G2C;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSBlessUthstinApplyStatsPacket : GamePacket
{
    public CSBlessUthstinApplyStatsPacket() : base(CSOffsets.CSBlessUthstinApplyStatsPacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        Logger.Debug("CSBlessUthstinApplyStatsPacket");

        var bApply = stream.ReadBoolean();
        var skillType = stream.ReadUInt32(); // 35178u see table skill_dynamic_effects
        var incStatsKind = stream.ReadInt32();
        var decStatsKind = stream.ReadInt32();
        var incStatsPoint = stream.ReadInt32();
        var decStatsPoint = stream.ReadInt32();
        var pageIndex = stream.ReadUInt32();

        var character = Connection.ActiveChar;

        if (!bApply)
        {
            Logger.Debug($"BlessUthstin has been canceled");
            return;
        }

        var stats = character.Stats.GetStatsByPageIndex(pageIndex);
        
        stats[incStatsKind] += incStatsPoint;
        stats[decStatsKind] -= decStatsPoint;

        var normalApplyCount = character.Stats.ApplyNormalCount;
        var specialApplyCount = character.Stats.ApplySpecialCount;


        var blessUthstinItem = SkillManager.Instance.GetBlessUthstinItems(skillType);
        switch (blessUthstinItem.ItemFunction)
        {
            case "normal":
                normalApplyCount++;
                break;
            case "special":
                specialApplyCount++;
                break;
        }
        character.Stats.UpdateStatsByPageIndex(pageIndex, stats, normalApplyCount, specialApplyCount);

        var bLogin = false;

        character.SendPacket(new SCBlessUthstinApplyStatsPacket(
            character.ObjId,
            true,
            stats,
            pageIndex,
            normalApplyCount,
            specialApplyCount,
            bLogin
        ));
    }
}
