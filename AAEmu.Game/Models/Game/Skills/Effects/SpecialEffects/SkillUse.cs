﻿using System;

using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Tasks.Skills;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class SkillUse : SpecialEffectAction
{
    protected override SpecialType SpecialEffectActionType => SpecialType.SkillUse;

    public override void Execute(BaseUnit caster,
        SkillCaster casterObj,
        BaseUnit target,
        SkillCastTarget targetObj,
        CastAction castObj,
        Skill skill,
        SkillObject skillObject,
        DateTime time,
        int skillId,
        int delay,
        int chance,
        int value4, int value5, int value6, int value7)
    {
        // TODO ...
        if (caster is Character) { Logger.Debug("Special effects: SkillUse skillId {0}, delay {1}, value3 {2}, value4 {3}", skillId, delay, chance, value4); }

        if (Rand.Next(0, 100) > chance && chance != 0)
        {
            ((Unit)caster).ConditionChance = false;
            return;
        }
        else
        {
            ((Unit)caster).ConditionChance = true;
        }

        //target = ((Unit)caster).CurrentTarget;
        var useSkill = new Skill(SkillManager.Instance.GetSkillTemplate((uint)skillId));
        targetObj = new SkillCastUnitTarget(target?.ObjId ?? 0);
        caster.Buffs.TriggerRemoveOn(Buffs.BuffRemoveOn.UseSkill);//Not sure if it belongs here.
        TaskManager.Instance.Schedule(new UseSkillTask(useSkill, caster, casterObj, target, targetObj, skillObject), TimeSpan.FromMilliseconds(delay));
        //useSkill.ApplyEffects(caster, casterObj, target, targetObj, skillObject);
    }
}
