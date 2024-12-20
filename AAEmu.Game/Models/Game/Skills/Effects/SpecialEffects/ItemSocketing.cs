using System;
using System.Collections.Generic;

using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Items.ItemSockets;
using AAEmu.Game.Models.Game.Items.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class ItemSocketing : SpecialEffectAction
{
    protected override SpecialType SpecialEffectActionType => SpecialType.ItemSocketing;
    private const int MaximumSlots = 9;
    private const int Offset = 4;

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
        // Get Player
        if (caster is not Character character)
        {
            Logger.Debug("Invalid caster type. Expected Character.");
            return;
        }

        Logger.Debug($"Special effects: ItemSocketing value1 {value1}, value2 {value2}, value3 {value3}, value4 {value4}, value5 {value5}, value6 {value6}, value7 {value7}");

        // value1 = 0 - Destroy, 1 - Add, 2 - Extract

        if (casterObj is not SkillItem gemSkillItem)
        {
            Logger.Debug("Invalid caster object type. Expected SkillItem.");
            return;
        }

        if (targetObj is not SkillCastItemTarget skillTargetItem)
        {
            Logger.Debug("Invalid target object type. Expected SkillCastItemTarget.");
            return;
        }

        var targetItem = character.Inventory.GetItemById(skillTargetItem.Id);
        if (targetItem is null)
        {
            Logger.Debug($"Target item with ID {skillTargetItem.Id} not found in inventory.");
            return;
        }

        var gemItem = character.Inventory.GetItemById(gemSkillItem.ItemId);
        if (gemItem is null)
        {
            Logger.Debug($"Gem item with ID {gemSkillItem.ItemId} not found in inventory.");
            return;
        }

        if (targetItem is not EquipItem equipItem)
        {
            Logger.Debug("Target item is not an EquipItem.");
            return;
        }


        if (targetItem.Template is not EquipItemTemplate equipItemTemplate)
        {
            Logger.Debug("Target item template is not an EquipItemTemplate.");
            return;
        }

        var tasksSocketing = new List<ItemTask>();
        var gemCount = ItemGameData.GemCount(equipItem);
        var socketNumLimit = ItemGameData.Instance.GetSocketNumLimit((int)equipItemTemplate.SlotTypeId, equipItem.Grade);

        switch (skillObject)
        {
            case SkillObjectAddSocketingSupport support:
                // Handle adding a gem
                if (support.Continuous)
                {
                    if (support.Count + gemCount < socketNumLimit)
                        socketNumLimit = support.Count + gemCount;

                    var preCount = gemCount + 1;
                    while (gemItem is not null && gemCount < socketNumLimit)
                    {
                        // Inserting a stone
                        ItemGameData.PutGem(gemItem, equipItem);

                        // Update the number of stones and check for the availability of the next stone
                        gemCount = ItemGameData.GemCount(equipItem);
                        gemItem = character.Inventory.GetItemById(gemSkillItem.ItemId);
                    }
                    tasksSocketing.Add(new ItemUpdate(equipItem));
                    character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.Socketing, tasksSocketing, []));
                    character.SendPacket(new SCSocketingResultPacket((int)SocketingKind.Add, skillTargetItem.Id, gemItem.TemplateId, (byte)SocketingKind.Add, true));
                    character.Inventory.Bag.ConsumeItem(ItemTaskType.SkillEffectGainItem, gemItem.TemplateId, gemCount - preCount, gemItem);
                }
                else
                {
                    // Inserting a stone
                    ItemGameData.PutGem(gemItem, equipItem);

                    tasksSocketing.Add(new ItemUpdate(equipItem));
                    character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.Socketing, tasksSocketing, []));
                    character.SendPacket(new SCSocketingResultPacket((int)SocketingKind.Add, skillTargetItem.Id, gemItem.TemplateId, (byte)SocketingKind.Add, true));
                    // тратится во время работы скилла
                    // is spent during the skill's operation
                    //character.Inventory.Bag.ConsumeItem(ItemTaskType.SkillEffectGainItem, gemItem.TemplateId, 1, gemItem);
                }

                break;
            case SkillObjectExtractSocketingSupport support:
                // Handle extracting a gem
                if (support.IsAll)
                {
                    for (var i = 0; i < socketNumLimit; i++)
                    {
                        if (equipItem.GemIds[i + Offset] == 0)
                            continue;

                        ItemGameData.GetGem(equipItem, i, character);
                    }

                    tasksSocketing.Add(new ItemUpdate(equipItem));
                    character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.Socketing, tasksSocketing, []));
                    character.SendPacket(new SCSocketingResultPacket((int)SocketingKind.Extract, skillTargetItem.Id, gemItem.TemplateId, (byte)SocketingKind.Extract, true));
                    character.Inventory.Bag.ConsumeItem(ItemTaskType.SkillEffectGainItem, gemItem.TemplateId, gemCount, gemItem);
                }
                else if (equipItem.GemIds[support.Index + Offset] != 0)
                {
                    ItemGameData.GetGem(equipItem, support.Index, character);

                    ItemGameData.UpdateCells(equipItem, support.Index);

                    tasksSocketing.Add(new ItemUpdate(equipItem));
                    character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.Socketing, tasksSocketing, []));
                    character.SendPacket(new SCSocketingResultPacket((int)SocketingKind.Extract, skillTargetItem.Id, gemItem.TemplateId, (byte)SocketingKind.Extract, true));
                    character.Inventory.Bag.ConsumeItem(ItemTaskType.SkillEffectGainItem, gemItem.TemplateId, 1, gemItem);
                }

                break;
            case SkillObject:
                // Handle destroying a gem
                for (var i = 0; i < socketNumLimit; i++)
                {
                    equipItem.GemIds[i + Offset] = 0;
                }
                tasksSocketing.Add(new ItemUpdate(equipItem));
                character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.Socketing, tasksSocketing, []));
                character.SendPacket(new SCSocketingResultPacket((int)SocketingKind.Destroy, skillTargetItem.Id, gemItem.TemplateId, (byte)SocketingKind.Destroy, true));
                character.Inventory.Bag.ConsumeItem(ItemTaskType.SkillEffectGainItem, gemItem.TemplateId, gemCount - 1, gemItem);
                break;
        }
    }
}
