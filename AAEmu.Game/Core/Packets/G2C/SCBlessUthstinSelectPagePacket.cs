using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCBlessUthstinSelectPagePacket : GamePacket
{
    private readonly uint _objId;
    private readonly bool _bResult;
    private readonly uint _selectPageIndex;

    public SCBlessUthstinSelectPagePacket(
        uint objId,
        bool bResult,
        uint selectPageIndex)
        : base(SCOffsets.SCBlessUthstinSelectPagePacket, 5)
    {
        _objId = objId;
        _bResult = bResult;
        _selectPageIndex = selectPageIndex;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.WriteBc(_objId); // character
        stream.Write(_bResult);
        stream.Write(_selectPageIndex);
        return stream;
    }
}
