﻿using System;
using System.Collections.Generic;

using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Chat;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class ItemCapScale : SpecialEffectAction
{
    protected override SpecialType SpecialEffectActionType => SpecialType.ItemCapScale;

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
        if (caster is Character) { Logger.Debug("Special effects: ItemCapScale value1 {0}, value2 {1}, value3 {2}, value4 {3}", value1, value2, value3, value4); }

        var owner = (Character)caster;
        var temperSkillItem = (SkillItem)casterObj;
        var skillTargetItem = (SkillCastItemTarget)targetObj;

        if (owner == null)
        {
            return;
        }

        if (temperSkillItem == null)
        {
            return;
        }

        if (skillTargetItem == null)
        {
            return;
        }

        var targetItem = owner.Inventory.GetItemById(skillTargetItem.Id);

        if (targetItem == null)
        {
            return;
        }

        var equipItem = (EquipItem)targetItem;

        var itemCapScale = ItemManager.Instance.GetItemCapScale(skill.Id);

        var physicalScale = (ushort)Rand.Next(itemCapScale.ScaleMin, itemCapScale.ScaleMax);
        var magicalScale = (ushort)Rand.Next(itemCapScale.ScaleMin, itemCapScale.ScaleMax);

        equipItem.TemperPhysical = physicalScale;
        equipItem.TemperMagical = magicalScale;

        // The item appears to be consumed as a skill reagent
        // temperItem._holdingContainer.ConsumeItem(ItemTaskType.EnchantPhysical, temperItem.TemplateId, 1, temperItem);
        owner.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.EnchantPhysical, new List<ItemTask>() { new ItemUpdate(equipItem) }, new List<ulong>()));
        // Note: According to various videos I have found, there is no information on the % reached by a temper ingame. This is sent to help indicate what was achieved.
        owner.SendMessage(ChatType.System, $"Temper:\n |cFFFFFFFF{physicalScale}%|r Physical\n|cFFFFFFFF{magicalScale}%|r Magical");
    }
}
