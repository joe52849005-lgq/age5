using System;
using System.Collections.Generic;

using AAEmu.Commons.Utils;
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
        int value4)
    {
        if (caster is not Character character)
        {
            Logger.Debug("Invalid caster type. Expected Character.");
            return;
        }

        Logger.Debug($"Special effects: ItemSocketing value1 {value1}, value2 {value2}, value3 {value3}, value4 {value4}");

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
        var gemCount = GemCount(equipItem);
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
                        PutGem(gemItem, equipItem);

                        // Update the number of stones and check for the availability of the next stone
                        gemCount = GemCount(equipItem);
                        gemItem = character.Inventory.GetItemById(gemSkillItem.ItemId);
                    }
                    tasksSocketing.Add(new ItemUpdate(equipItem));
                    character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.Socketing, tasksSocketing, []));
                    character.SendPacket(new SCSocketingResultPacket((int)SocketingKind.Add, skillTargetItem.Id, gemItem.TemplateId, (byte)SocketingKind.Add, true));
                    character.Inventory.Bag.ConsumeItem(ItemTaskType.SkillEffectGainItem, gemItem.TemplateId, gemCount - preCount, gemItem);

                    //character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.Socketing,
                    //[
                    //    new ItemUpdate(equipItem),
                    //    new MoneyChange(-(83579 * gemCount)),
                    //    gemItem.Count > gemCount
                    //        ? new ItemCountUpdate(gemItem, -(gemCount - preCount))
                    //        : new ItemRemove(gemItem)
                    //], [], 0));
                    //character.SendPacket(new SCSocketingResultPacket((int)SocketingKind.Add, skillTargetItem.Id, gemItem.TemplateId, (byte)SocketingKind.Add, true));
                }
                else
                {
                    // Inserting a stone
                    PutGem(gemItem, equipItem);

                    tasksSocketing.Add(new ItemUpdate(equipItem));
                    character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.Socketing, tasksSocketing, []));
                    character.SendPacket(new SCSocketingResultPacket((int)SocketingKind.Add, skillTargetItem.Id, gemItem.TemplateId, (byte)SocketingKind.Add, true));
                    // тратится во время работы скилла
                    //character.Inventory.Bag.ConsumeItem(ItemTaskType.SkillEffectGainItem, gemItem.TemplateId, 1, gemItem);

                    //character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.Socketing,
                    //[
                    //    new ItemUpdate(equipItem),
                    //    new MoneyChange(-(83579 * gemCount)),
                    //    gemItem.Count > 1
                    //        ? new ItemCountUpdate(gemItem, -1)
                    //        : new ItemRemove(gemItem)
                    //], [], 0));
                    //character.SendPacket(new SCSocketingResultPacket((int)SocketingKind.Add, skillTargetItem.Id, gemItem.TemplateId, (byte)SocketingKind.Add, true));
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

                        GetGem(equipItem, i, character);
                    }

                    tasksSocketing.Add(new ItemUpdate(equipItem));
                    character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.Socketing, tasksSocketing, []));
                    character.SendPacket(new SCSocketingResultPacket((int)SocketingKind.Extract, skillTargetItem.Id, gemItem.TemplateId, (byte)SocketingKind.Extract, true));
                    character.Inventory.Bag.ConsumeItem(ItemTaskType.SkillEffectGainItem, gemItem.TemplateId, gemCount, gemItem);

                    //character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.Socketing,
                    //[
                    //    new ItemUpdate(equipItem),
                    //    gemItem.Count > gemCount
                    //        ? new ItemCountUpdate(gemItem, -gemCount)
                    //        : new ItemRemove(gemItem)
                    //], [], 0));
                    //character.SendPacket(new SCSocketingResultPacket((int)SocketingKind.Extract, skillTargetItem.Id, gemItem.TemplateId, (byte)SocketingKind.Extract, true));
                }
                else if (equipItem.GemIds[support.Index + Offset] != 0)
                {
                    GetGem(equipItem, support.Index, character);

                    UpdateCells(equipItem, support.Index);

                    tasksSocketing.Add(new ItemUpdate(equipItem));
                    character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.Socketing, tasksSocketing, []));
                    character.SendPacket(new SCSocketingResultPacket((int)SocketingKind.Extract, skillTargetItem.Id, gemItem.TemplateId, (byte)SocketingKind.Extract, true));
                    character.Inventory.Bag.ConsumeItem(ItemTaskType.SkillEffectGainItem, gemItem.TemplateId, 1, gemItem);

                    //character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.Socketing,
                    //[
                    //    new ItemUpdate(equipItem),
                    //    gemItem.Count > 1
                    //        ? new ItemCountUpdate(gemItem, -1)
                    //        : new ItemRemove(gemItem)
                    //], [], 0));
                    //character.SendPacket(new SCSocketingResultPacket((int)SocketingKind.Extract, skillTargetItem.Id, gemItem.TemplateId, (byte)SocketingKind.Extract, true));
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

                //character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.Socketing,
                //[
                //    new ItemUpdate(equipItem),
                //    gemItem.Count > gemCount - 1
                //        ? new ItemCountUpdate(gemItem, -(gemCount - 1))
                //        : new ItemRemove(gemItem)
                //], [], 0));
                //character.SendPacket(new SCSocketingResultPacket((int)SocketingKind.Destroy, skillTargetItem.Id, gemItem.TemplateId, (byte)SocketingKind.Destroy, true));

                break;
        }
    }

    private static void GetGem(EquipItem equipItem, int i, Character owner)
    {
        var extractedGemId = equipItem.GemIds[i + Offset];
        equipItem.GemIds[i + Offset] = 0;
        owner.Inventory.Bag.AcquireDefaultItem(ItemTaskType.SkillEffectGainItem, extractedGemId, 1, equipItem.Grade);
    }

    private static void PutGem(Item gemItem, EquipItem equipItem)
    {
        var gemRoll = Rand.Next(0, 101);
        var gemChance = ItemGameData.Instance.GetSocketChance(gemItem);
        if (gemRoll < gemChance)
        {
            for (var i = 0; i < MaximumSlots; i++)
            {
                if (equipItem.GemIds[i + Offset] != 0)
                    continue;

                equipItem.GemIds[i + Offset] = gemItem.TemplateId;
                break;
            }
        }
    }

    private static void UpdateCells(EquipItem equipItem, int writeIndex)
    {
        // Move filled cells to the beginning, starting with the first empty cell
        for (var readIndex = writeIndex + 1; readIndex < MaximumSlots; readIndex++)
        {
            if (equipItem.GemIds[readIndex + Offset] == 0)
                continue;

            // If the current cell is not empty, move its value to the cell with index writeIndex
            equipItem.GemIds[writeIndex + Offset] = equipItem.GemIds[readIndex + Offset];
            equipItem.GemIds[readIndex + Offset] = 0;
            writeIndex++;
        }
    }

    private static int GemCount(EquipItem equipItem)
    {
        var gemCount = 0;
        for (var index = 0; index < MaximumSlots; index++)
        {
            var gem = equipItem.GemIds[index + Offset];
            if (gem != 0)
            {
                gemCount++;
            }
        }

        return gemCount;
    }
}
