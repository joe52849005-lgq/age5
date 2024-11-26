using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCBlessUthstinApplyStatsPacket : GamePacket
{
    private readonly uint _objId;
    private readonly bool _bResult;
    private readonly int[] _stats;
    private readonly uint _targetPageIndex;
    private readonly int _normalApplyCount;
    private readonly int _specialApplyCount;
    private readonly bool _bLogin;

    public SCBlessUthstinApplyStatsPacket(
        uint objId,
        bool bResult,
        int[] stats,
        uint targetPageIndex,
        int normalApplyCount,
        int specialApplyCount,
        bool bLogin)
        : base(SCOffsets.SCBlessUthstinApplyStatsPacket, 5)
    {
        _objId = objId;
        _bResult = bResult;
        _stats = stats;
        _targetPageIndex = targetPageIndex;
        _normalApplyCount = normalApplyCount;
        _specialApplyCount = specialApplyCount;
        _bLogin = bLogin;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.WriteBc(_objId); // character
        stream.Write(_bResult);
        foreach (var stat in _stats)
        {
            stream.Write(stat);
        }
        stream.Write(_targetPageIndex);
        stream.Write(_normalApplyCount);
        stream.Write(_specialApplyCount);
        stream.Write(_bLogin);
        return stream;
    }
}
