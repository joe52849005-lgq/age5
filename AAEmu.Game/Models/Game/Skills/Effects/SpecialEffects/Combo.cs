﻿using System;

using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class Combo : SpecialEffectAction
{
    public override void Execute(BaseUnit caster,
        SkillCaster casterObj,
        BaseUnit target,
        SkillCastTarget targetObj,
        CastAction castObj,
        Skill skill,
        SkillObject skillObject,
        DateTime time,
        int comboSkillId,
        int timeFromNow,
        int value3,
        int value4, int value5, int value6, int value7)
    {
        if (caster is Character) { Logger.Debug("Special effects: Combo comboSkillId {0}, timeFromNow {1}, value3 {2}, value4 {3}", comboSkillId, timeFromNow, value3, value4); }
    }
}
