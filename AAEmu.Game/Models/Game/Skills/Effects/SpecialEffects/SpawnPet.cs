﻿using System;

using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class SpawnPet : SpecialEffectAction
{
    protected override SpecialType SpecialEffectActionType => SpecialType.SpawnPet;

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
        // TODO ...
        if (caster is Character) { Logger.Debug("Special effects: SpawnPet value1 {0}, value2 {1}, value3 {2}, value4 {3}", value1, value2, value3, value4); }

        var owner = (Character)caster;
        var skillData = (SkillItem)casterObj;

        switch (value1)
        {
            // TODO - maybe not hardcoded
            case 4944: // land
            case 3466: // sea
                //owner.Mates.SpawnMount(skillData);
                break;
        }
        owner.Mates.SpawnMount(skillData);
    }
}
