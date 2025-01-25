using System;

using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class FinishChanneling : SpecialEffectAction
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
        Logger.Debug($"Special effects: FinishChanneling value1 {value1}, value2 {value2}, value3 {value3}, value4 {value4}, value5 {value5}, value6 {value6}, value7 {value7}");

        if (target is Unit unit)
        {
            unit.ActivePlotState?.FinishChanneling();
        }
    }
}
