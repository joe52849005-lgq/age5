﻿using System;

using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class EnterBeautyshop : SpecialEffectAction
{
    protected override SpecialType SpecialEffectActionType => SpecialType.EnterBeautyshop;

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
        if (caster is Character player)
        {
            Logger.Debug("Special effects: EnterBeautyshop value1 {0}, value2 {1}, value3 {2}, value4 {3}", value1, value2, value3, value4);
            player.SendPacket(new SCToggleBeautyshopResponsePacket(1));
        }
    }
}
