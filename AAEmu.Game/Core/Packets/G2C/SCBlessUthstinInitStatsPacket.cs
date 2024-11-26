using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCBlessUthstinInitStatsPacket : GamePacket
{
    private readonly uint _objId;
    private readonly bool _bResult;
    private readonly uint _pageIndex;

    public SCBlessUthstinInitStatsPacket(uint objId, bool bResult, uint pageIndex) : base(SCOffsets.SCBlessUthstinInitStatsPacket, 5)
    {
        _objId = objId;
        _bResult = bResult;
        _pageIndex = pageIndex;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.WriteBc(_objId); // character
        stream.Write(_bResult);
        stream.Write(_pageIndex);
        return stream;
    }
}
