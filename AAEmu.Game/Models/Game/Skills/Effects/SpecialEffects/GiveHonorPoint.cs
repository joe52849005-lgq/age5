﻿using System;

using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.StaticValues;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class GiveHonorPoint : SpecialEffectAction
{
    protected override SpecialType SpecialEffectActionType => SpecialType.GiveHonorPoint;

    public override void Execute(BaseUnit caster, SkillCaster casterObj, BaseUnit target, SkillCastTarget targetObj,
        CastAction castObj,
        Skill skill, SkillObject skillObject, DateTime time, int amount, int value2, int value3, int value4, int value5,
        int value6, int value7)
    {
        if (caster is Character) { Logger.Debug("Special effects: GiveHonorPoint amount {0}, value2 {1}, value3 {2}, value4 {3}", amount, value2, value3, value4); }

        if (!(caster is Character character))
            return;

        var points = (int)Math.Round(AppConfiguration.Instance.World.HonorRate * amount);
        character.ChangeGamePoints(GamePointKind.Honor, points);
    }
}
