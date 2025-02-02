using System;

using AAEmu.Game.Core.Packets;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.AI.Enums;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.StaticValues;

namespace AAEmu.Game.Models.Game.Skills.Effects;

public class NpcControlEffect : EffectTemplate
{
    public NpcControlCategory CategoryId { get; set; }
    public string ParamString { get; set; }
    public uint ParamInt { get; set; }

    // ---
    private string FileName { get; set; }
    private string FileName2 { get; set; }
    private uint SkillId { get; set; }
    private uint Timeout { get; set; }
    // ---

    public override bool OnActionTime => false;

    public override void Apply(BaseUnit caster, SkillCaster casterObj, BaseUnit target, SkillCastTarget targetObj,
        CastAction castObj, EffectSource source, SkillObject skillObject, DateTime time,
        CompressedGamePackets packetBuilder = null)
    {
        Logger.Info($"NpcControllEffect: CategoryId={CategoryId}, ParamString={ParamString}, ParamInt={ParamInt}, caster={caster.TemplateId}, target={target.TemplateId}");

        FileName = string.Empty;
        FileName2 = string.Empty;
        
        if (target is Npc targetNpc)
        {
            switch (CategoryId)
            {
                case NpcControlCategory.Signal:
                    break;
                case NpcControlCategory.FollowUnit:
                    break;
                case NpcControlCategory.FollowPath:
                    {
                        if (targetNpc.IsInPatrol) { break; }
                        targetNpc.IsInPatrol = true;
                        if (targetNpc.Simulation != null)
                        {
                            targetNpc.Simulation.RunningMode = false;
                            targetNpc.Simulation.MoveToPathEnabled = false;
                            targetNpc.Simulation.MoveFileName = ParamString;
                            targetNpc.Simulation.GoToPath(targetNpc, true);
                        }

                        break;
                    }
                case NpcControlCategory.AttackUnit:
                    targetNpc.SetFaction(FactionsEnum.Monstrosity);
                    break;
                case NpcControlCategory.GoAway:
                    break;
                case NpcControlCategory.RunCommandSet:
                    {
                        var cmds = AiGameData.Instance.GetAiCommands(ParamInt);
                        if (cmds is { Count: > 0 })
                        {
                            targetNpc.Ai?.EnqueueAiCommands(cmds);
                            
                            foreach (var aiCommands in cmds)
                            {
                                switch (aiCommands.CmdId)
                                {
                                    case AiCommandCategory.FollowUnit:
                                        break;
                                    case AiCommandCategory.FollowPath:
                                        if (string.IsNullOrEmpty(FileName))
                                        {
                                            FileName = aiCommands.Param2;
                                        }
                                        else
                                        {
                                            FileName2 = aiCommands.Param2;
                                        }
                                        break;
                                    case AiCommandCategory.UseSkill:
                                        SkillId = aiCommands.Param1;
                                        break;
                                    case AiCommandCategory.Timeout:
                                        Timeout = aiCommands.Param1;
                                        break;
                                    default:
                                        throw new NotSupportedException(nameof(aiCommands.CmdId));
                                }
                            }
                            if (!string.IsNullOrEmpty(FileName))
                            {
                                if (targetNpc.IsInPatrol) { return; }
                                targetNpc.IsInPatrol = true;
                                if (targetNpc.Simulation != null)
                                {
                                    targetNpc.Simulation.RunningMode = false;
                                    targetNpc.Simulation.Cycle = false;
                                    targetNpc.Simulation.MoveToPathEnabled = false;
                                    targetNpc.Simulation.MoveFileName = FileName;
                                    targetNpc.Simulation.MoveFileName2 = FileName2;
                                    targetNpc.Simulation.GoToPath(targetNpc, true, SkillId, Timeout);
                                }
                            }
                        }
                        break;
                    }
                default:
                    throw new NotSupportedException(nameof(CategoryId));
            }
        }
    }
}
