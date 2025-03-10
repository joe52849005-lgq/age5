﻿using System;

using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class ApplyReagents : SpecialEffectAction
{
    protected override SpecialType SpecialEffectActionType => SpecialType.ApplyReagents;

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
        if (caster is Character) { Logger.Debug("Special effects: ApplyReagents value1 {0}, value2 {1}, value3 {2}, value4 {3}", value1, value2, value3, value4); }

        var skillReagents = SkillManager.Instance.GetSkillReagentsBySkillId(skill.Id);

        if (skillReagents.Count > 0)
        {
            var player = (Character)caster;
            foreach (var reagent in skillReagents)
            {
                player.Inventory.Bag.ConsumeItem(Items.Actions.ItemTaskType.SkillReagents, reagent.ItemId, reagent.Amount, null);
            }
        }
        else if (casterObj is SkillItem item)
        {
            var player = (Character)caster;
            player.Inventory.Bag.ConsumeItem(Items.Actions.ItemTaskType.SkillReagents, item.ItemTemplateId, 1, null);
        }
    }
}
