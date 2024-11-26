using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCBlessUthstinExpandPagePacket : GamePacket
{
    private readonly uint _objId;
    private readonly bool _bResult;
    private readonly uint _expandPageIndex;

    public SCBlessUthstinExpandPagePacket(
        uint objId,
        bool bResult,
        uint expandPageIndex)
        : base(SCOffsets.SCBlessUthstinExpandPagePacket, 5)
    {
        _objId = objId;
        _bResult = bResult;
        _expandPageIndex = expandPageIndex;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.WriteBc(_objId); // character
        stream.Write(_bResult);
        stream.Write(_expandPageIndex);
        return stream;
    }
}
