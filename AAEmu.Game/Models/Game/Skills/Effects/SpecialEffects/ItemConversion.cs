﻿using System;

using AAEmu.Commons.Utils;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class ItemConversion : SpecialEffectAction
{
    protected override SpecialType SpecialEffectActionType => SpecialType.ItemConversion;
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
        if (caster is Character) { Logger.Debug("Special effects: ItemConversion value1 {0}, value2 {1}, value3 {2}, value4 {3}", value1, value2, value3, value4); }

        if (caster is not Character character)
        {
            skill.Cancelled = true;
            return;
        }

        if (casterObj is not SkillItem sourceItem)
        {
            skill.Cancelled = true;
            return;
        }
        if (targetObj is not SkillCastItemTarget itemTarget)
        {
            skill.Cancelled = true;
            return;
        }

        var targetItem = character.Inventory.Bag.GetItemByItemId(itemTarget.Id);
        if (targetItem == null)
        {
            skill.Cancelled = true;
            return;
        }

        if (!targetItem.Template.Disenchantable)
        {
            skill.Cancelled = true;
            return;
        }

        var id = targetItem.TemplateId;
        var reagent = ItemConversionGameData.Instance.GetReagentForItem(targetItem.Grade, targetItem.Template.ImplId, id, targetItem.Template.Level);
        if (reagent == null)
        {
            Logger.Error($"Couldn't find Reagent for item {id}");
            skill.Cancelled = true;
            return;
        }

        var product = ItemConversionGameData.Instance.GetProductFromReagent(reagent);
        if (product == null)
        {
            Logger.Error($"Couldn't find Product from Reagent for item {id}");
            skill.Cancelled = true;
            return;
        }

        var productRoll = Rand.Next(0, 10000);
        var productChance = product.ChanceRate;
        if (productRoll < productChance)
        {
            // give product
            // TODO: add in weights
            var value = Rand.Next(product.MinOutput, product.MaxOutput + 1);
            if (!character.Inventory.Bag.AcquireDefaultItem(ItemTaskType.Conversion, product.OuputItemId, value))
            {
                skill.Cancelled = true;
                character.SendErrorMessage(ErrorMessageType.BagFull);
                return;
            }
        }

        // destroy item
        if (character.Inventory.Bag.ConsumeItem(ItemTaskType.SkillEffectGainItem, sourceItem.ItemTemplateId, 1, null) <= 0)
        {
            character.SendErrorMessage(ErrorMessageType.FailedToUseItem);
            Logger.Error($"Couldn't find Product from Reagent for item {sourceItem.ItemTemplateId}");
        }
        if (character.Inventory.Bag.ConsumeItem(ItemTaskType.SkillEffectGainItem, targetItem.TemplateId, 1, null) <= 0)
        {
            character.SendErrorMessage(ErrorMessageType.FailedToUseItem);
            Logger.Error($"Couldn't find Product from Reagent for item {targetItem.TemplateId}");
        }
    }
}
