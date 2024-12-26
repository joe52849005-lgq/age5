using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.TodayAssignments;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCTodayAssignmentChangedPacket : GamePacket
{
    private readonly int _todayQuestStepId;
    private readonly int _todayQuestGroupId;
    private readonly int _todayQuestGroupQuestContextId;
    private readonly byte _status;
    private readonly bool _init;

    public SCTodayAssignmentChangedPacket(int todayQuestStepId, int todayQuestGroupId, int todayQuestGroupQuestContextId, TodayAssignmentData status, bool init)
        : base(SCOffsets.SCTodayAssignmentChangedPacket, 5)
    {
            _todayQuestStepId = todayQuestStepId;
            _todayQuestGroupId = todayQuestGroupId;
            _todayQuestGroupQuestContextId = todayQuestGroupQuestContextId;
            _status = (byte)status;
            _init = init;
        }

    public override PacketStream Write(PacketStream stream)
    {
            stream.Write(_todayQuestStepId);
            stream.Write(_todayQuestGroupId);
            stream.Write(_todayQuestGroupQuestContextId);
            stream.Write(_status);
            stream.Write(_init);
            return stream;
        }
}
