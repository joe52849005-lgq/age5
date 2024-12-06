using System;
using System.Collections.Generic;

using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Items.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class ItemEvolving : SpecialEffectAction
{
    protected override SpecialType SpecialEffectActionType => SpecialType.ItemEvolving;

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
            return;

        Logger.Debug("Special effects: ItemEvolving value1 {0}, value2 {1}, value3 {2}, value4 {3}", value1, value2, value3, value4);

        var itemId = ((SkillCastItemTarget)targetObj).Id;
        var item = ItemManager.Instance.GetItemByItemId(itemId);
        if (!(item is EquipItem && item.Template is EquipItemTemplate equipItem))
        {
            Logger.Debug($"Attempting to upgrade a non-equipment item. Item={item.Id}");
            return;
        }

        var itemId1 = ((SkillObjectItemEvolvingSupport)skillObject).M1ItemId;
        var item1 = ItemManager.Instance.GetItemByItemId(itemId1);
        if (!(item1 is EquipItem && item1.Template is EquipItemTemplate equipItem1))
        {
            Logger.Debug($"Attempting to use an item that is not equipment. Item={item1.Id}");
            return;
        }

        var itemId2 = ((SkillObjectItemEvolvingSupport)skillObject).M2ItemId;
        var item2 = ItemManager.Instance.GetItemByItemId(itemId2);
        if (!(item2 is EquipItem && item2.Template is EquipItemTemplate equipItem2))
        {
            Logger.Debug($"Attempting to use an item that is not equipment. Item={item2.Id}");
            return;
        }

        // We do not allow the item being upgraded to be destroyed
        if (item.Id == item1.Id || item.Id == item2.Id)
        {
            Logger.Debug($"You cannot use the same item for improvement! Item={item.Id}, Item1={item1.Id}, Item2={item2.Id}");
            character.SendMessage($"You cannot use the same item for improvement! Item @ITEM_NAME({item.TemplateId})'s");
            return;
        }

        var autoUseAAPoint = ((SkillObjectItemEvolvingSupport)skillObject).AutoUseAAPoint;

        var beforeItemGrade = item.Grade;
        Logger.Debug($"ItemEvolving: beforeItemGrade={beforeItemGrade}");

        var addExperience = CalculateAddExperience(equipItem1, item1, equipItem2, item2);
        var currentExperience = (int)item.AdditionalDetails[3]; //RemainingExperience; // взять текущий опыт
        currentExperience += addExperience;

        var bonusExp = ItemGameData.Instance.GetBonus(equipItem.ItemRndAttrCategoryId, item.Grade);
        if (bonusExp > 0)
        {
            currentExperience += bonusExp;
            addExperience += bonusExp;
        }

        // ограничим грейд предмета
        var gradeExp = ItemGameData.Instance.GetGradeExp(equipItem.ItemRndAttrCategoryId, item.Grade);
        var maxEvolvingGrade = ItemGameData.Instance.GetItemRndAttrCategory(equipItem.ItemRndAttrCategoryId).MaxEvolvingGrade;
        if (beforeItemGrade >= maxEvolvingGrade && currentExperience >= gradeExp)
        {
            currentExperience = gradeExp;
            addExperience = gradeExp;
            item.AdditionalDetails[3] = 0;
            Logger.Debug($"ItemEvolving: Reached grade limit! beforeItemGrade:maxEvolvingGrade={beforeItemGrade}:{maxEvolvingGrade}, currentExperience={currentExperience}");
        }

        var (afterItemGrade, remainingExperience) = CalculateGradeAndExp(currentExperience, equipItem.ItemRndAttrCategoryId, beforeItemGrade);
        item.AdditionalDetails[3] = (uint)remainingExperience; // сохраним оставшийся опыт
        item.Grade = afterItemGrade;

        var changeIndex = ((SkillObjectItemEvolvingSupport)skillObject).ChangeIndex; // -1 - не менять аттрибуты, (0..n) - изменить существующий атрибут
        var needChangeAttribute = changeIndex != -1;

        var beforeAttribute = new ItemEvolvingAttribute();
        var afterAttribute = new ItemEvolvingAttribute();
        var addAttributes = new List<ItemEvolvingAttribute>();
        var currentAttributes = GetCurrentAttributes(item);

        if (needChangeAttribute)
        {
            if (afterItemGrade > beforeItemGrade)
            {
                var selectAttribute = ItemGameData.Instance.GetUnitAttributeFromModifierGroup((int)item.AdditionalDetails[changeIndex + 4]);
                beforeAttribute.Attribute = (ushort)selectAttribute;
                beforeAttribute.AttributeType = 0;
                beforeAttribute.AttributeValue = 0;

                var newAttribute = ItemGameData.Instance.ReplaceSelectAttribute(equipItem.ItemRndAttrCategoryId, item.Grade, currentAttributes, selectAttribute);
                item.AdditionalDetails[changeIndex + 4] = (uint)newAttribute.id;
                afterAttribute.Attribute = (ushort)newAttribute.attribute;
                afterAttribute.AttributeType = 0;
                afterAttribute.AttributeValue = newAttribute.value;

                Logger.Debug($"ItemEvolving: beforeAttribute id={item.AdditionalDetails[changeIndex + 4]}, attribute={selectAttribute}, value={beforeAttribute.AttributeValue}, afterAttribute id={newAttribute.id}, attribute={newAttribute.attribute}, value={newAttribute.value}");
            }
        }
        else if (ItemGameData.Instance.GetMaxUnitModifierNum(equipItem.ItemRndAttrCategoryId, item.Grade) > currentAttributes.Count && afterItemGrade > beforeItemGrade)
        {
            var randomAttributes = ItemGameData.Instance.GetRandomAttributes(equipItem.ItemRndAttrCategoryId, item.Grade, currentAttributes);

            for (var index = currentAttributes.Count; index < randomAttributes.Count + currentAttributes.Count; index++)
            {
                var newAttribute = new ItemEvolvingAttribute();
                newAttribute.Attribute = (ushort)randomAttributes[index - currentAttributes.Count].attribute;
                newAttribute.AttributeType = 0;
                newAttribute.AttributeValue = randomAttributes[index - currentAttributes.Count].value;

                addAttributes.Add(newAttribute);
                item.AdditionalDetails[index + 4] = (uint)randomAttributes[index - currentAttributes.Count].id;
                Logger.Debug($"ItemEvolving: addAttribute id={randomAttributes[index - currentAttributes.Count].id}, Attribute={randomAttributes[index - currentAttributes.Count].attribute}, value={randomAttributes[index - currentAttributes.Count].value}");
            }
        }

        // израсходуем материал
        character.Inventory.Bag.ConsumeItem(ItemTaskType.Evolving, item1.TemplateId, item1.Count, item1);
        character.Inventory.Bag.ConsumeItem(ItemTaskType.Evolving, item2.TemplateId, item2.Count, item2);

        // израсходуем деньги
        var money = ItemGameData.Instance.GetGoldMul(equipItem.ItemRndAttrCategoryId, item.Grade);
        character.ChangeMoney(SlotType.Bag, -money);

        character.SendPacket(new SCItemTaskSuccessPacket(
            ItemTaskType.Evolving,
            [
                new ItemGradeChange(item),
                new ItemUpdateRepair(item)
            ],
            [])
        );

        Logger.Debug($"ItemEvolving: isEvolving={true}, itemId={itemId}, changeAttr={needChangeAttribute}, afterItemGrade={afterItemGrade}, addExperience={addExperience}, bonusExp={bonusExp}, beforeItemGrade={beforeItemGrade}");
        character.SendPacket(new SCItemEvolvingPacket(
                true, // bool isEvolving,
                itemId, // ulong itemId,
                needChangeAttribute, //bool changeAttr,
                afterItemGrade, // byte afterItemGrade,
                beforeAttribute, // ItemEvolvingAttribute beforeAttribute,
                afterAttribute, // ItemEvolvingAttribute afterAttribute,
                addAttributes, // List<ItemEvolvingAttribute> addAttributes,
                (byte)addAttributes.Count, // addAttrCount,
                addExperience,
                bonusExp,
                beforeItemGrade
            )
        );
    }

    private int CalculateAddExperience(EquipItemTemplate equipItem1, Item item1, EquipItemTemplate equipItem2, Item item2)
    {
        var addExperience = 0;
        var e1 = ItemGameData.Instance.GetGainExp(equipItem1.ItemRndAttrCategoryId, item1.Grade);
        Logger.Debug($"ItemEvolving: equipItem1 ItemRndAttrCategoryId={equipItem1.ItemRndAttrCategoryId}, Grade={item1.Grade}, addExperience={e1}");
        addExperience += e1;

        var e2 = ItemGameData.Instance.GetGainExp(equipItem2.ItemRndAttrCategoryId, item2.Grade);
        Logger.Debug($"ItemEvolving: equipItem2 ItemRndAttrCategoryId={equipItem2.ItemRndAttrCategoryId}, Grade={item2.Grade}, addExperience={e2}");
        addExperience += e2;

        return addExperience;
    }

    private (byte grade, int exp) CalculateGradeAndExp(int exp, int categoryId, byte beforeItemGrade = 0)
    {
        Logger.Debug($"ItemEvolving:CalculateGradeAndExp: exp={exp}, categoryId={categoryId}");
        var grades = new List<byte>
            {
                0,  // Grade 0 Обычный предмет - Basic
                2,  // Grade 2 Необычный предмет - Grand
                3,  // Grade 3 Редкий предмет - Rare
                4,  // Grade 4 Уникальный предмет - Arcane
                5,  // Grade 5 Эпический предмет - Heroic
                6,  // Grade 6 Легендарный предмет - Unique
                7,  // Grade 7 Реликвия - Celestial
                8,  // Grade 8 предмет эпохи Чудес - Divine
                9,  // Grade 9 предмет эпохи Сказаний - Epic
                10, // Grade 10 предмет эпохи Легенд - Legendary
                11, // Grade 11 предмет эпохи Мифов - Mythic
                12  // Grade 12 предмет эпохи Двенадцати - Ethernal
            };

        byte res = 0;

        foreach (var grade in grades)
        {
            if (grade < beforeItemGrade)
                continue;

            var gradeExp = ItemGameData.Instance.GetGradeExp(categoryId, grade);
            if (gradeExp == 0)
                gradeExp = ItemGameData.Instance.GetGradeExp(4, grade); // как подстраховка

            if (exp <= gradeExp)
            {
                Logger.Debug($"ItemEvolving:CalculateGradeAndExp: exp={exp}, grade={grade}, gradeExp={gradeExp}");
                return (grade, exp);
            }

            res = grade;
            exp -= gradeExp;
            Logger.Debug($"ItemEvolving:CalculateGradeAndExp: exp={exp}, grade={res}, gradeExp={gradeExp}");
        }

        Logger.Debug($"ItemEvolving:CalculateGradeAndExp: exp={exp}, grade={res}");
        return (res, exp);
    }

    private static List<int> GetCurrentAttributes(Item item)
    {
        var res = new List<int>();

        for (var index = 4; index < 9; index++)
        {
            var attribute = item.AdditionalDetails[index];
            if (attribute <= 0)
                break;

            res.Add((int)attribute);
        }

        return res;
    }
}
