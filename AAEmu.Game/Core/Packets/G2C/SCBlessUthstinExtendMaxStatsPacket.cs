using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCBlessUthstinExtendMaxStatsPacket : GamePacket
{
    private readonly uint _objId;
    private readonly bool _bResult;
    private readonly int _extendMaxStats;
    private readonly int _applyExtendCount;

    public SCBlessUthstinExtendMaxStatsPacket(uint objId, bool bResult, int extendMaxStats, int applyExtendCount)
        : base(SCOffsets.SCBlessUthstinExtendMaxStatsPacket, 5)
    {
        _objId = objId;
        _bResult = bResult;
        _extendMaxStats = extendMaxStats;
        _applyExtendCount = applyExtendCount;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.WriteBc(_objId); // character
        stream.Write(_bResult);
        stream.Write(_extendMaxStats);
        stream.Write(_applyExtendCount);
        return stream;
    }
}
