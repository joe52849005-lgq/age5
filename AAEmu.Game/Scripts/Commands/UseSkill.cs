﻿using System;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Tasks.Skills;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Utils.Scripts;

namespace AAEmu.Game.Scripts.Commands;

public class UseSkill : ICommand
{
    public string[] CommandNames { get; set; } = new[] { "useskill", "use_skill", "testskill", "test_skill" };

    public void OnLoad()
    {
        CommandManager.Instance.Register(CommandNames, this);
    }

    public string GetCommandLineHelp()
    {
        return "[target||area] <skillId>";
    }

    public string GetCommandHelpText()
    {
        return "Forces unit(target optional) to use a skill";
    }

    public void Execute(Character character, string[] args, IMessageOutput messageOutput)
    {
        var argsIdx = 0;
        Unit source = character;
        var target = character.CurrentTarget == null ? character : (Unit)character.CurrentTarget;

        if (target == null)
        {
            return;
        }

        if (args.Length == 0)
        {
            CommandManager.SendDefaultHelpText(this, messageOutput);
            return;
        }

        if (args[0] == "target")
        {
            (source, target) = (target, source);
            argsIdx++;
        }

        if (args[0] == "area")
        {
            if (uint.TryParse(args[1], out var skillIdAoe))
            {
                var skillTemplate2 = SkillManager.Instance.GetSkillTemplate(skillIdAoe);
                if (skillTemplate2 != null)
                {
                    DoAoe(character, skillTemplate2);
                }
            }

            return;
        }

        var casterObj = new SkillCasterUnit(source.ObjId);
        var targetObj = SkillCastTarget.GetByType(SkillCastTargetType.Unit);
        targetObj.ObjId = target.ObjId;
        var skillObject = new SkillObject();

        if (!uint.TryParse(args[argsIdx], out var skillId))
        {
            return;
        }

        var skillTemplate = SkillManager.Instance.GetSkillTemplate(skillId);
        if (skillTemplate == null)
        {
            return;
        }

        var useSkill = new Skill(skillTemplate);
        TaskManager.Instance.Schedule(new UseSkillTask(useSkill, source, casterObj, target, targetObj, skillObject), TimeSpan.FromMilliseconds(0));
    }

    private static void DoAoe(Character character, SkillTemplate skill)
    {
        foreach (var target in WorldManager.GetAround<Unit>(character, 20f))
        {
            var casterObj = new SkillCasterUnit(target.ObjId);
            var targetObj = SkillCastTarget.GetByType(SkillCastTargetType.Unit);
            targetObj.ObjId = character.ObjId;
            var skillObject = new SkillObject();

            var useSkill = new Skill(skill);
            TaskManager.Instance.Schedule(new UseSkillTask(useSkill, target, casterObj, character, targetObj, skillObject), TimeSpan.FromMilliseconds(0));
        }
    }
}
