using System;
using System.Collections.Generic;
using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.TodayAssignments;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSTodayAssignmentPacket : GamePacket
{
    public CSTodayAssignmentPacket() : base(CSOffsets.CSTodayAssignmentPacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        var realStep = stream.ReadInt32(); // realStep
        var request = (TodayAssignmentData)stream.ReadByte();  // request
        Logger.Debug($"CSTodayAssignmentPacket, realStep: {realStep}, request: {request}");

        var character = Connection.ActiveChar;
        var todayQuestStepId = TodayAssignmentGameData.Instance.GetTodayQuestStepId(realStep);
        var todayQuestGroupId = TodayAssignmentGameData.Instance.GetTodayQuestGroupId(todayQuestStepId);
        var todayQuestGroupQuestContextId = TodayAssignmentGameData.Instance.GetTodayQuestGroupQuestContextId(todayQuestGroupId);

        switch (request)
        {
            case TodayAssignmentData.Locked:
                break;
            case TodayAssignmentData.Ready:
                {
                    character.SendPacket(new SCTodayAssignmentChangedPacket(todayQuestStepId, todayQuestGroupId, 0, TodayAssignmentData.Ready, false));
                    break;
                }
            case TodayAssignmentData.Progress:
                {
                    character.Quests.AddQuest((uint)todayQuestGroupQuestContextId);
                    character.SendPacket(new SCTodayAssignmentChangedPacket(todayQuestStepId, todayQuestGroupId, todayQuestGroupQuestContextId, TodayAssignmentData.Progress, false));
                    break;
                }
            case TodayAssignmentData.Done:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        //Connection.ActiveChar.Skills.Reset((AbilityType) abilityId);
        // по realStep из таблицы today_quest_steps берем today_quest_step_id
        // например, открываем семейный квест: realStep = 201, request = 1
        // 201 -> 26
        // затем
        // данные из таблицы today_quest_group_quest
        //                                                                    V--today_quest_step_id
        //                                                                        V--today_quest_group_id
        //                                                                             V--quest_context_id
        //                                                                                    V--status
        //character.SendPacket(new SCTodayAssignmentChangedPacket(1u, 13u, 6907u, 2, true));
        //character.SendPacket(new SCTodayAssignmentChangedPacket(9u, 33u, 7205u, 2, true));
        //character.SendPacket(new SCTodayAssignmentChangedPacket(8u, 22u, 7240u, 2, true));
        //character.SendPacket(new SCTodayAssignmentItemSentPacket((uint)realStep, request));

    }
}
