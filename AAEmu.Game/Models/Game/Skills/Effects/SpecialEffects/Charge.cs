﻿using System;

using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

/// <summary>
/// SpecialEffect linked with adding charges to a buff.
/// </summary>
public class Charge : SpecialEffectAction
{
    protected override SpecialType SpecialEffectActionType => SpecialType.Charge;

    public override void Execute(BaseUnit caster, SkillCaster casterObj, BaseUnit target, SkillCastTarget targetObj,
        CastAction castObj,
        Skill skill, SkillObject skillObject, DateTime time, int buffId, int minCharge, int maxCharge, int unused,
        int value5, int value6, int value7)
    {
        if (caster is Character) { Logger.Debug("Special effects: Charge buffId {0}, minCharge {1}, maxCharge {2}, unused {3}", buffId, minCharge, maxCharge, unused); }

        lock (caster.ChargeLock)
        {
            var buff = caster.Buffs.GetEffectFromBuffId((uint)buffId);
            var template = SkillManager.Instance.GetBuffTemplate((uint)buffId);

            var chargeDelta = Rand.Next(minCharge, maxCharge);
            var oldCharge = buff?.Charge ?? 0;

            var newEffect =
                new Buff(target, caster, casterObj, template, skill, time)
                {
                    Charge = Math.Min(chargeDelta, template.MaxCharge)
                };

            caster.Buffs.AddBuff(newEffect, buff?.Index ?? 0);

            var newCharge = Math.Min(oldCharge + chargeDelta, template.MaxCharge);
            newEffect.Charge = newCharge;
        }
    }
}
