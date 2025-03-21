﻿using System;

using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class AutoAttack : SpecialEffectAction
{
    public override void Execute(BaseUnit caster,
        SkillCaster casterObj,
        BaseUnit target,
        SkillCastTarget targetObj,
        CastAction castObj,
        Skill skill,
        SkillObject skillObject,
        DateTime time,
        int value1,
        int value2,
        int value3,
        int value4, int value5, int value6, int value7)
    {
        // TODO start auto attack...
        if (caster is Character) { Logger.Debug("Special effects: AutoAttack value1 {0}, value2 {1}, value3 {2}, value4 {3}", value1, value2, value3, value4); }

        // skill_effect_Id->effect_id->skill_Id автоатаки 1->1->2, 2->2->3, 51->36->4
    }
}
