﻿using System;

using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class ResetCooldown : SpecialEffectAction
{
    protected override SpecialType SpecialEffectActionType => SpecialType.ResetCooldown;

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
        if (caster is Character) { Logger.Debug("Special effects: ResetCooldown skillId {0}, tagId {1}, gcd {2}, value4 {3}", value1, value2, value3, value4); }

        uint skillId = (uint)value1;
        uint tagId = (uint)value2;
        bool gcd = value3 == 1;
        if (caster is Character character)
        {
            if (value1 != 0)
            {
                character.ResetSkillCooldown(skillId, gcd);
            }
            if (value2 != 0)
            {
                //unsure if this works..Might need to reset each skill individually
                character.SendPacket(new SCSkillCooldownResetPacket(character, 0, tagId, gcd));
            }
        }

        //Maybe do this for NPC's ?
    }
}
