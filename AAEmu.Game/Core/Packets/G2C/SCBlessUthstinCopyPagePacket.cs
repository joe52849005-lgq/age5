using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCBlessUthstinCopyPagePacket : GamePacket
{
    private readonly uint _objId;
    private readonly bool _bResult;
    private readonly uint _copyPageIndex;
    private readonly int[] _stats;
    private readonly int _normalApplyCount;
    private readonly int _specialApplyCount;

    public SCBlessUthstinCopyPagePacket(
        uint objId,
        bool bResult,
        uint copyPageIndex,
        int[] stats,
        int normalApplyCount,
        int specialApplyCount)
        : base(SCOffsets.SCBlessUthstinCopyPagePacket, 5)
    {
        _objId = objId;
        _bResult = bResult;
        _copyPageIndex = copyPageIndex;
        _stats = stats;
        _normalApplyCount = normalApplyCount;
        _specialApplyCount = specialApplyCount;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.WriteBc(_objId); // character
        stream.Write(_bResult);
        stream.Write(_copyPageIndex);
        foreach (var stat in _stats)
        {
            stream.Write(stat);
        }
        stream.Write(_normalApplyCount);
        stream.Write(_specialApplyCount);
        return stream;
    }
}
