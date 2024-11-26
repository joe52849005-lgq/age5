using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCBlessUthstinConsumeApplyStatsPacket : GamePacket
{
    private readonly uint _objId;
    private readonly bool _bResult;
    private readonly uint _skillType;
    private readonly int _incStatsKind;
    private readonly int _decStatsKind;
    private readonly int _incStatsPoint;
    private readonly int _decStatsPoint;

    public SCBlessUthstinConsumeApplyStatsPacket(
        uint objId,
        bool bResult,
        uint skillType,
        int incStatsKind,
        int decStatsKind,
        int incStatsPoint,
        int decStatsPoint)
        : base(SCOffsets.SCBlessUthstinConsumeApplyStatsPacket, 5)
    {
        _objId = objId;
        _bResult = bResult;
        _skillType = skillType;
        _incStatsKind = incStatsKind;
        _decStatsKind = decStatsKind;
        _incStatsPoint = incStatsPoint;
        _decStatsPoint = decStatsPoint;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.WriteBc(_objId); // character
        stream.Write(_bResult);
        stream.Write(_skillType); // skillType 35178u see table skill_dynamic_effects
        stream.Write(_incStatsKind);
        stream.Write(_decStatsKind);
        stream.Write(_incStatsPoint);
        stream.Write(_decStatsPoint);
        return stream;
    }
}
