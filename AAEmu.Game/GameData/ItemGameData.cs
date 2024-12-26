using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.GameData.Framework;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Formulas;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Items.ItemEnchants;
using AAEmu.Game.Models.Game.Items.ItemRndAttr;
using AAEmu.Game.Models.Game.Items.ItemRndAttrs;
using AAEmu.Game.Models.Game.Items.ItemSockets;
using AAEmu.Game.Models.Game.Items.Mappings;
using AAEmu.Game.Models.Game.Items.Slave;
using AAEmu.Game.Models.Game.Items.Templates;
using AAEmu.Game.Models.Game.Skills.Effects.Enums;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.StaticValues;
using AAEmu.Game.Utils.DB;

using Microsoft.Data.Sqlite;

using NLog;

using ItemGradeEnchantingSupport = AAEmu.Game.Models.Game.Items.ItemEnchants.ItemGradeEnchantingSupport;

namespace AAEmu.Game.GameData
{
    [GameData]
    public class ItemGameData : Singleton<ItemGameData>, IGameDataLoader
    {
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();
        private Random _random = new();

        private ConcurrentDictionary<uint, ConcurrentDictionary<byte, uint>> _itemGradeBuffs;
        // Synthesis
        private ConcurrentDictionary<int, ItemRndAttrCategory> _itemRndAttrCategories;
        private ConcurrentDictionary<int, ConcurrentDictionary<int, ItemRndAttrCategoryMaterial>> _itemRndAttrCategoryMaterials;
        private ConcurrentDictionary<int, ConcurrentDictionary<int, ItemRndAttrCategoryProperty>> _itemRndAttrCategoryProperties;
        private ConcurrentDictionary<int, ConcurrentDictionary<int, ItemRndAttrUnitModifierGroupSet>> _itemRndAttrUnitModifierGroupSets;
        private ConcurrentDictionary<int, ConcurrentDictionary<int, ItemRndAttrUnitModifierGroup>> _itemRndAttrUnitModifierGroups;
        private ConcurrentDictionary<int, ItemRndAttrUnitModifierGroup> _itemRndAttributUnitModifierGroups;
        private ConcurrentDictionary<int, ConcurrentDictionary<int, ItemRndAttrUnitModifier>> _itemRndAttrUnitModifiers;

        // Socketing
        private ConcurrentDictionary<int, ItemSocket> _itemSockets;
        private ConcurrentDictionary<int, ConcurrentDictionary<int, ItemSocketNumLimit>> _itemSocketNumLimits;
        private ConcurrentDictionary<int, int> _itemSocketLevelLimits;
        private ConcurrentDictionary<int, (bool, int)> _itemSocketChances;

        // Mapping
        private ConcurrentDictionary<int, ItemChangeMappingGroup> _itemChangeMappingGroups;
        private ConcurrentDictionary<int, ItemChangeMapping> _itemChangeMappings;

        // GradeEnchant
        private ConcurrentDictionary<int, EnchantScaleRatio> _enchantScaleRatios;
        private ConcurrentDictionary<int, ConcurrentDictionary<int, ItemEnchantRatio>> _itemEnchantRatios;
        private ConcurrentDictionary<int, ItemEnchantRatioGroup> _itemEnchantRatioGroups;
        private ConcurrentDictionary<int, int> _itemEnchantRatioItems;
        private ConcurrentDictionary<int, ItemEnchantingGem> _itemEnchantingGems;
        private ConcurrentDictionary<int, ItemGradeEnchantingSupport> _itemGradeEnchantingSupports;

        private ConcurrentDictionary<uint, ItemSlaveEquipment> _itemSlaveEquipments;
        private ConcurrentDictionary<int, SlaveEquipmentEquipSlotPack> _slaveEquipmentEquipSlotPacks;

        #region Synthesis
        public BuffTemplate GetItemBuff(uint itemId, byte gradeId)
        {
            if (_itemGradeBuffs.TryGetValue(itemId, out var itemGradeBuffs))
                if (itemGradeBuffs.TryGetValue(gradeId, out var buffId))
                    return SkillManager.Instance.GetBuffTemplate(buffId);
            return null;
        }

        public int GetGainExp(int categoryId, int gradeId)
        {
            return GetCategoryProperty(categoryId, gradeId)?.GainExp ?? 0;
        }

        public int GetGoldMul(int categoryId, int gradeId)
        {
            return GetCategoryProperty(categoryId, gradeId)?.GoldMul ?? 0;
        }

        public int GetMaxUnitModifierNum(int categoryId, int gradeId)
        {
            return GetCategoryProperty(categoryId, gradeId)?.MaxUnitModifierNum ?? 0;
        }

        public int GetBonus(int categoryId, int gradeId)
        {
            var property = GetCategoryProperty(categoryId, gradeId);
            if (property?.BonusExpChance > 0)
            {
                var probability = Math.Max(0, Math.Min(100, property.BonusExpChance - 100));
                var chance = _random.Next(0, 101);
                if (chance <= probability)
                {
                    var min = Math.Min(property.BonusExpMin, property.BonusExpMax);
                    var max = Math.Max(property.BonusExpMin, property.BonusExpMax);
                    return _random.Next(min, max + 1);
                }
            }
            return 0;
        }

        public int GetGradeExp(int categoryId, int gradeId)
        {
            return GetCategoryProperty(categoryId, gradeId)?.GradeExp ?? 0;
        }

        public ItemRndAttrCategory GetItemRndAttrCategory(int categoryId)
        {
            _itemRndAttrCategories.TryGetValue(categoryId, out var category);
            return category;
        }

        public ItemRndAttrCategoryMaterial GetCategoryMaterial(int categoryId, int materialId)
        {
            ItemRndAttrCategoryMaterial material = null;
            if (_itemRndAttrCategoryMaterials.TryGetValue(categoryId, out var materials))
                materials.TryGetValue(materialId, out material);
            return material;
        }

        public ItemRndAttrCategoryProperty GetCategoryProperty(int categoryId, int propertyId)
        {
            ItemRndAttrCategoryProperty property = null;
            if (_itemRndAttrCategoryProperties.TryGetValue(categoryId, out var properties))
                properties.TryGetValue(propertyId, out property);
            return property;
        }

        public ItemRndAttrUnitModifierGroupSet GetUnitModifierGroupSet(int categoryId, int groupSetId)
        {
            ItemRndAttrUnitModifierGroupSet groupSet = null;
            if (_itemRndAttrUnitModifierGroupSets.TryGetValue(categoryId, out var groupSets))
                groupSets.TryGetValue(groupSetId, out groupSet);
            return groupSet;
        }

        /// <summary>
        /// GetRandomAttributes - we get random attributes
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="grade"></param>
        /// <param name="existingAttributes"></param>
        /// <returns></returns>
        public List<(int id, int attribute, int value)> GetRandomAttributes(int categoryId, byte grade, List<int> existingAttributes)
        {
            var selectedAttributes = new List<(int id, int attribute, int value)>();
            if (!_itemRndAttrUnitModifierGroupSets.TryGetValue(categoryId, out var unitModifierGroupSets))
                return selectedAttributes;

            var random = Random.Shared; // Use Random.Shared for better randomness and thread safety
            var existingAttributesSet = new HashSet<int>(existingAttributes);
            foreach (var groupSet in unitModifierGroupSets.Values)
            {
                var pickNum = groupSet.PickNum; // how much can you take from the list

                if (!_itemRndAttrUnitModifierGroups.TryGetValue(groupSet.Id, out var unitModifierGroups))
                    continue;

                var allAttributes = unitModifierGroups.Values
                    .Where(group => !existingAttributesSet.Contains(group.Id))
                    .ToList();

                if (allAttributes.Count <= 0 || pickNum <= 0)
                    continue;

                var numToPick = Math.Min(pickNum, allAttributes.Count);
                for (var i = 0; i < numToPick; i++)
                {
                    var randomIndex = random.Next(allAttributes.Count);
                    var selectedAttribute = allAttributes[randomIndex];
                    var value = GetUnitModifierRandomValue(selectedAttribute.Id, grade);
                    selectedAttributes.Add((selectedAttribute.Id, selectedAttribute.UnitAttributeId, value));
                    allAttributes.RemoveAt(randomIndex);
                }
            }

            return selectedAttributes;
        }

        /// <summary>
        /// ReplaceSelectAttribute - replacing one selected attribute with a random one
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="grade"></param>
        /// <param name="existingAttributes"></param>
        /// <param name="selectAttribute"></param>
        /// <returns></returns>
        public (int id, int attribute, int value) ReplaceSelectAttribute(int categoryId, byte grade, List<int> existingAttributes, int selectAttribute)
        {
            if (!_itemRndAttrUnitModifierGroupSets.TryGetValue(categoryId, out var unitModifierGroupSets))
                return (0, 0, 0);

            var random = Random.Shared; // Use Random.Shared for better randomness and thread safety
            var existingAttributesSet = new HashSet<int>(existingAttributes);
            var availableAttributes = unitModifierGroupSets.Values.ToList();

            foreach (var groupSet in availableAttributes)
            {
                if (!_itemRndAttrUnitModifierGroups.TryGetValue(groupSet.Id, out var unitModifierGroups))
                    continue;

                // Check if the specified attribute is present in any group's attributes
                var isAttributePresent = unitModifierGroups.Values.Any(attr => attr.UnitAttributeId == selectAttribute);
                if (!isAttributePresent)
                    continue; // if the group does not contain the required attribute, skip it

                var allAttributes = unitModifierGroups.Values
                    .Where(group => !existingAttributesSet.Contains(group.Id))
                    .ToList();

                if (allAttributes.Count == 0)
                    continue;

                var randomIndex = random.Next(allAttributes.Count);
                var selectedAttribute = allAttributes[randomIndex];
                var value = GetUnitModifierRandomValue(selectedAttribute.Id, grade);
                return (selectedAttribute.Id, selectedAttribute.UnitAttributeId, value);
            }

            return (0, 0, 0);
        }

        public int GetUnitAttributeFromModifierGroup(int id)
        {
            if (!_itemRndAttributUnitModifierGroups.TryGetValue(id, out var groups))
                return 0;

            return groups.UnitAttributeId;
        }

        public ItemRndAttrUnitModifierGroup GetUnitModifierGroup(int groupSetId, int groupId)
        {
            ItemRndAttrUnitModifierGroup group = null;
            if (_itemRndAttrUnitModifierGroups.TryGetValue(groupSetId, out var groups))
                groups.TryGetValue(groupId, out group);
            return group;
        }

        public int GetUnitModifierRandomValue(int groupId, byte grade)
        {
            if (!_itemRndAttrUnitModifiers.TryGetValue(groupId, out var modifiers))
                return 0;

            if (!modifiers.TryGetValue(grade, out var modifier))
                return 0;

            if (modifier == null)
                return 0;

            var min = modifier.Min;
            var max = modifier.Max;

            // Ensure min <= max
            if (min > max)
                (min, max) = (max, min);

            // Generate a random number between min and max (inclusive)
            var value = Random.Shared.Next(min, max + 1);
            return value;

        }

        public ItemRndAttrUnitModifier GetUnitModifier(int groupId, int modifierId)
        {
            ItemRndAttrUnitModifier modifier = null;
            if (_itemRndAttrUnitModifiers.TryGetValue(groupId, out var modifiers))
                modifiers.TryGetValue(modifierId, out modifier);
            return modifier;
        }
        #endregion Synthesis

        #region ItemSocketing
        public int GetSocketChance(Item item)
        {
            _itemSockets.TryGetValue((int)item.TemplateId, out var itemSocket);
            if (itemSocket == null)
            {
                return 0;
            }

            _itemSocketChances.TryGetValue(itemSocket.ItemSocketChanceId, out var chance);

            return chance.Item2;

        }

        public int GetSocketChance(int numSockets)
        {
            return _itemSocketChances.TryGetValue(numSockets, out var chance) ? chance.Item2 : 0;
        }

        public int GetSocketNumLimit(int slotTypeId, byte grade)
        {
            if (_itemSocketNumLimits.TryGetValue(slotTypeId, out var itemSocketNumLimits))
            {
                if (itemSocketNumLimits.TryGetValue(grade, out var value))
                {
                    return value.NumSocket;
                }
            }

            return 0;
        }

        private const int ItemSocketingMaximumSlots = 9;
        private const int ItemSocketingOffset = 4;

        public static void GetGem(EquipItem equipItem, int i, Character owner)
        {
            var extractedGemId = equipItem.GemIds[i + ItemSocketingOffset];
            equipItem.GemIds[i + ItemSocketingOffset] = 0;
            owner.Inventory.Bag.AcquireDefaultItem(ItemTaskType.SkillEffectGainItem, extractedGemId, 1, equipItem.Grade);
        }

        public static void PutGem(Item gemItem, EquipItem equipItem)
        {
            var gemRoll = Rand.Next(0, 101);
            var gemChance = ItemGameData.Instance.GetSocketChance(gemItem);
            if (gemRoll < gemChance)
            {
                for (var i = 0; i < ItemSocketingMaximumSlots; i++)
                {
                    if (equipItem.GemIds[i + ItemSocketingOffset] != 0)
                        continue;

                    equipItem.GemIds[i + ItemSocketingOffset] = gemItem.TemplateId;
                    break;
                }
            }
        }

        public static void UpdateCells(EquipItem equipItem, int writeIndex)
        {
            // Move filled cells to the beginning, starting with the first empty cell
            for (var readIndex = writeIndex + 1; readIndex < ItemSocketingMaximumSlots; readIndex++)
            {
                if (equipItem.GemIds[readIndex + ItemSocketingOffset] == 0)
                    continue;

                // If the current cell is not empty, move its value to the cell with index writeIndex
                equipItem.GemIds[writeIndex + ItemSocketingOffset] = equipItem.GemIds[readIndex + ItemSocketingOffset];
                equipItem.GemIds[readIndex + ItemSocketingOffset] = 0;
                writeIndex++;
            }
        }

        public static int GemCount(EquipItem equipItem)
        {
            var gemCount = 0;
            for (var index = 0; index < ItemSocketingMaximumSlots; index++)
            {
                var gem = equipItem.GemIds[index + ItemSocketingOffset];
                if (gem != 0)
                {
                    gemCount++;
                }
            }

            return gemCount;
        }
        #endregion ItemSocketing

        #region ItemEvolving
        private const int ItemEvolvingOffset = 13;
        private const int ItemEvolvingOffsetMax = ItemEvolvingOffset + 5;

        public static int CalculateAddExperience(EquipItemTemplate equipItem1, Item item1, EquipItemTemplate equipItem2, Item item2)
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

        public static (byte grade, int exp) CalculateGradeAndExp(int exp, int categoryId, byte beforeItemGrade = 0)
        {
            Logger.Debug($"ItemEvolving: CalculateGradeAndExp: exp={exp}, categoryId={categoryId}");
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
                    Logger.Debug($"ItemEvolving: CalculateGradeAndExp: exp={exp}, grade={grade}, gradeExp={gradeExp}");
                    return (grade, exp);
                }

                res = grade;
                exp -= gradeExp;
                Logger.Debug($"ItemEvolving: CalculateGradeAndExp: exp={exp}, grade={res}, gradeExp={gradeExp}");
            }

            Logger.Debug($"ItemEvolving: CalculateGradeAndExp: exp={exp}, grade={res}");
            return (res, exp);
        }

        public static List<int> GetCurrentAttributes(Item item)
        {
            var res = new List<int>();

            for (var index = ItemEvolvingOffset; index < ItemEvolvingOffsetMax; index++)
            {
                var attribute = item.GemIds[index];
                if (attribute <= 0)
                    break;

                res.Add((int)attribute);
            }

            return res;
        }

        public static void CopyAllAttributes(Item sourceItem, Item targetItem)
        {
            for (var index = 0; index < 22; index++)
            {
                targetItem.GemIds[index] = sourceItem.GemIds[index];
            }
        }
        #endregion ItemEvolving

        #region Mapping
        public int GetMappingItem(int mappingGroupId, byte grade, int sourceItemId)
        {
            return (from itemChangeMapping in _itemChangeMappings.Values
                    where itemChangeMapping.MappingGroupId == mappingGroupId &&
                          itemChangeMapping.SourceGradeId == grade &&
                          itemChangeMapping.SourceItemId == sourceItemId
                    select itemChangeMapping.TargetItemId).FirstOrDefault();
        }
        #endregion Mapping

        #region GradeEnchant

        public int GetItemEnchantRatioGroupByItemId(int itemId)
        {
            _itemEnchantRatioItems.TryGetValue(itemId, out var value);
            if (value == 0)
            {
                value = 9000001;
            }
            return value;
        }

        // kindid (itemType) 1 - WeaponTemplate, 2 - armorTemplate, 3 - accessoryTemplate
        public int GetItemEnchantRatioGroup(Item item)
        {
            var kindId = 0;
            var value = 0;
            if (item.Template.ImplId > ItemImplEnum.Armor)
            {
                kindId = 3;
            }
            else if (item.Template.ImplId == ItemImplEnum.Armor)
            {
                kindId = 2;
            }
            else if (item.Template.ImplId == ItemImplEnum.Weapon)
            {
                kindId = 1;
            }

            foreach (var itemEnchantRatioGroup in _itemEnchantRatioGroups.Values)
            {
                if (itemEnchantRatioGroup.ItemEnchantRatioKindId == kindId)
                {
                    value = itemEnchantRatioGroup.Id;
                }
            }
            return value;
        }

        public ItemGradeEnchantingSupport GetItemGradEnchantingSupportByItemId(int itemId)
        {
            _itemGradeEnchantingSupports.TryGetValue(itemId, out var itemGradeEnchantingSupport);
            return itemGradeEnchantingSupport;
        }

        public int GetGradeEnchantCost(int ratioGroupId, int grade)
        {
            if (_itemEnchantRatios.TryGetValue(ratioGroupId, out var itemEnchantRatios))
            {
                if (itemEnchantRatios.TryGetValue(grade, out var value))
                {
                    return value.GradeEnchantCost;
                }
            }

            return 0;
        }
        public ItemEnchantRatio GetItemEnchantRatio(int ratioGroupId, int grade)
        {
            if (_itemEnchantRatios.TryGetValue(ratioGroupId, out var itemEnchantRatios))
            {
                if (itemEnchantRatios.TryGetValue(grade, out var value))
                {
                    return value;
                }
            }

            return new ItemEnchantRatio();
        }

        //public ItemEnchantRatio GetItemEnchantRatio(int ItemTemplateId, int grade)
        //{
        //    if (_itemEnchantRatioItems.TryGetValue(ItemTemplateId, out var ItemEnchantRatioGroupId))
        //    {
        //        if (_itemEnchantRatios.TryGetValue(ItemEnchantRatioGroupId, out var enchantScaleRatioTemplates))
        //        {
        //            if (enchantScaleRatioTemplates.TryGetValue(grade, out var value))
        //            {
        //                return value;
        //            }
        //        }
        //    }

        //    return new ItemEnchantRatio();
        //}
        public EnchantScaleRatio GetEnchantScaleRatio(int temperingLevel)
        {
            if (_enchantScaleRatios.TryGetValue(temperingLevel, out var enchantScaleRatio))
            {
                return enchantScaleRatio;
            }

            return new EnchantScaleRatio();
        }
        public ItemChangeMappingGroup GetItemChangeMappingGroup(int mappingGroupId)
        {
            if (_itemChangeMappingGroups.TryGetValue(mappingGroupId, out var itemChangeMappingGroup))
            {
                return itemChangeMappingGroup;
            }

            return new ItemChangeMappingGroup();
        }

        public static GradeEnchantResult RollRegrade(ItemEnchantRatio itemEnchantRatio, Item item, bool isLucky, bool useCharm, ItemGradeEnchantingSupport charmInfo)
        {
            var successRoll = Rand.Next(0, 10000);
            var breakRoll = Rand.Next(0, 10000);
            var downgradeRoll = Rand.Next(0, 10000);
            var greatSuccessRoll = Rand.Next(0, 10000);

            // TODO : Refactor
            var successChance = useCharm
                ? GetCharmChance(itemEnchantRatio.GradeEnchantSuccessRatio, charmInfo.AddSuccessRatio, charmInfo.AddSuccessMul)
                : itemEnchantRatio.GradeEnchantSuccessRatio;
            var greatSuccessChance = useCharm
                ? GetCharmChance(itemEnchantRatio.GradeEnchantGreatSuccessRatio, charmInfo.AddGreatSuccessRatio,
                    charmInfo.AddGreatSuccessMul)
                : itemEnchantRatio.GradeEnchantGreatSuccessRatio;
            var breakChance = useCharm
                ? GetCharmChance(itemEnchantRatio.GradeEnchantBreakRatio, charmInfo.AddBreakRatio, charmInfo.AddBreakMul)
                : itemEnchantRatio.GradeEnchantBreakRatio;
            var downgradeChance = useCharm
                ? GetCharmChance(itemEnchantRatio.GradeEnchantDowngradeRatio, charmInfo.AddDowngradeRatio,
                    charmInfo.AddDowngradeMul)
                : itemEnchantRatio.GradeEnchantDowngradeRatio;

            if (successRoll < successChance)
            {
                if (isLucky && greatSuccessRoll < greatSuccessChance)
                {
                    // TODO : Refactor
                    var increase = useCharm ? 2 + charmInfo.AddGreatSuccessGrade : 2;
                    item.Grade = (byte)GetNextGrade(item.Grade, increase);
                    return GradeEnchantResult.GreatSuccess;
                }

                item.Grade = (byte)GetNextGrade(item.Grade, 1);
                return GradeEnchantResult.Success;
            }

            if (breakRoll < breakChance)
            {
                return GradeEnchantResult.Break;
            }

            if (downgradeRoll < downgradeChance)
            {
                var newGrade = (byte)Rand.Next(itemEnchantRatio.GradeEnchantDowngradeMin, itemEnchantRatio.GradeEnchantDowngradeMax);
                if (newGrade < 0)
                {
                    return GradeEnchantResult.Fail;
                }

                item.Grade = newGrade;
                return GradeEnchantResult.Downgrade;
            }

            return GradeEnchantResult.Fail2;
        }

        public static int GoldCost(Item item, ItemImpl itemType, int scaleCost = 0, FormulaKind formulaKind = FormulaKind.GradeEnchantCost)
        {
            uint slotTypeId = 0;
            switch (itemType)
            {
                case ItemImpl.Weapon:
                    var weaponTemplate = (WeaponTemplate)item.Template;
                    slotTypeId = weaponTemplate.HoldableTemplate.SlotTypeId;
                    break;
                case ItemImpl.Armor:
                    var armorTemplate = (ArmorTemplate)item.Template;
                    slotTypeId = armorTemplate.SlotTemplate.SlotTypeId;
                    break;
                case ItemImpl.Accessory:
                    var accessoryTemplate = (AccessoryTemplate)item.Template;
                    slotTypeId = accessoryTemplate.SlotTemplate.SlotTypeId;
                    break;
                    //case ItemImpl.SlaveEquipment:
                    //    var slaveEquip = (ItemSlaveEquip)item;
                    //    if (slaveEquip is not null)
                    //    {
                    //        slotTypeId = GetSlaveEquipSlotTypeId(slaveEquip.SlotPackId);
                    //    }
                    //    break;
            }

            if (slotTypeId == 0)
            {
                return -1;
            }

            var enchantingCost = ItemManager.Instance.GetEquipSlotEnchantingCost(slotTypeId);
            var equipSlotEnchantCost = enchantingCost.Cost;

            var ratioGroupId = Instance.GetItemEnchantRatioGroupByItemId((int)item.TemplateId);
            var itemGrade = Instance.GetGradeEnchantCost(ratioGroupId, item.Grade);
            var itemLevel = item.Template.Level;

            var parameters = new Dictionary<string, double>();
            parameters.Add("scale_cost", scaleCost);
            parameters.Add("item_grade", itemGrade);
            parameters.Add("item_level", itemLevel);
            parameters.Add("equip_slot_enchant_cost", equipSlotEnchantCost);
            var formula = FormulaManager.Instance.GetFormula((uint)formulaKind);

            var cost = (int)formula.Evaluate(parameters);

            return cost;
        }

        private static int GetNextGrade(int currentGrade, int gradeChange)
        {
            currentGrade = currentGrade switch
            {
                0 => 1,
                1 => 0,
                _ => currentGrade
            };

            // Calculate the next grade
            var nextGrade = currentGrade + gradeChange;

            nextGrade = nextGrade switch
            {
                // Ensure nextGrade is within the valid range
                0 => 1,
                1 => 0,
                > 12 => 12,
                _ => nextGrade
            };

            // Return the clamped nextGrade
            return nextGrade;
        }

        private static int GetCharmChance(int baseChance, int charmRatio, int charmMul)
        {
            return baseChance + charmRatio + (int)(baseChance * (charmMul / 100.0));
        }
        #endregion GradeEnchant

        #region Sqlite
        public void Load(SqliteConnection connection, SqliteConnection connection2)
        {
            InitializeDictionaries();

            #region Synthesis
            LoadItemGradeBuffs(connection);
            LoadItemRndAttrCategories(connection);
            LoadItemRndAttrCategoryMaterials(connection);
            LoadItemRndAttrCategoryProperties(connection);
            LoadItemRndAttrUnitModifierGroupSets(connection);
            LoadItemRndAttrUnitModifierGroups(connection);
            LoadItemRndAttrUnitModifiers(connection);
            #endregion Synthesis

            #region Socketing
            LoadItemSockets(connection);
            LoadItemSocketNumLimits(connection);
            LoadItemSocketLevelLimits(connection);
            LoadItemSocketChances(connection);
            #endregion Socketing

            #region Mapping
            LoadItemChangeMappingGroups(connection);
            LoadItemChangeMappings(connection);
            #endregion Mapping

            #region GradeEnchant
            LoadEnchantScaleRatios(connection);
            LoadItemEnchantRatios(connection);
            LoadItemEnchantRatioGroups(connection);
            LoadItemEnchantRatioItems(connection);
            LoadItemEnchantingGems(connection);
            LoadItemGradeEnchantingSupports(connection);
            #endregion GradeEnchant

            LoadItemSlaveEquipments(connection);
            LoadSlaveEquipmentEquipSlotPacks(connection);

        }

        private void InitializeDictionaries()
        {
            #region Synthesis
            _itemGradeBuffs = new ConcurrentDictionary<uint, ConcurrentDictionary<byte, uint>>();
            _itemRndAttrCategories = new ConcurrentDictionary<int, ItemRndAttrCategory>();
            _itemRndAttrCategoryMaterials = new ConcurrentDictionary<int, ConcurrentDictionary<int, ItemRndAttrCategoryMaterial>>();
            _itemRndAttrCategoryProperties = new ConcurrentDictionary<int, ConcurrentDictionary<int, ItemRndAttrCategoryProperty>>();
            _itemRndAttrUnitModifierGroupSets = new ConcurrentDictionary<int, ConcurrentDictionary<int, ItemRndAttrUnitModifierGroupSet>>();
            _itemRndAttrUnitModifierGroups = new ConcurrentDictionary<int, ConcurrentDictionary<int, ItemRndAttrUnitModifierGroup>>();
            _itemRndAttributUnitModifierGroups = new ConcurrentDictionary<int, ItemRndAttrUnitModifierGroup>();
            _itemRndAttrUnitModifiers = new ConcurrentDictionary<int, ConcurrentDictionary<int, ItemRndAttrUnitModifier>>();
            #endregion Synthesis

            #region Socketing
            _itemSockets = new ConcurrentDictionary<int, ItemSocket>();
            _itemSocketNumLimits = new ConcurrentDictionary<int, ConcurrentDictionary<int, ItemSocketNumLimit>>();
            _itemSocketLevelLimits = new ConcurrentDictionary<int, int>();
            _itemSocketChances = new ConcurrentDictionary<int, (bool, int)>();
            #endregion Socketing

            #region Mapping
            _itemChangeMappingGroups = new ConcurrentDictionary<int, ItemChangeMappingGroup>();
            _itemChangeMappings = new ConcurrentDictionary<int, ItemChangeMapping>();
            #endregion Mapping

            #region GradeEnchant
            _enchantScaleRatios = new ConcurrentDictionary<int, EnchantScaleRatio>();
            _itemEnchantRatios = new ConcurrentDictionary<int, ConcurrentDictionary<int, ItemEnchantRatio>>();
            _itemEnchantRatioGroups = new ConcurrentDictionary<int, ItemEnchantRatioGroup>();
            _itemEnchantRatioItems = new ConcurrentDictionary<int, int>();
            _itemEnchantingGems = new ConcurrentDictionary<int, ItemEnchantingGem>();
            _itemGradeEnchantingSupports = new ConcurrentDictionary<int, ItemGradeEnchantingSupport>();
            #endregion GradeEnchant

            _itemSlaveEquipments = new ConcurrentDictionary<uint, ItemSlaveEquipment>();
            _slaveEquipmentEquipSlotPacks = new ConcurrentDictionary<int, SlaveEquipmentEquipSlotPack>();
        }

        #region Synthesis
        private void LoadItemGradeBuffs(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM item_grade_buffs";
            command.Prepare();
            using var sqliteReader = command.ExecuteReader();
            using var reader = new SQLiteWrapperReader(sqliteReader);
            while (reader.Read())
            {
                var itemId = reader.GetUInt32("item_id");
                var itemGrade = reader.GetByte("item_grade_id");
                var buffId = reader.GetUInt32("buff_id");

                var itemGradeBuffs = _itemGradeBuffs.GetOrAdd(itemId, new ConcurrentDictionary<byte, uint>());

                if (!itemGradeBuffs.TryAdd(itemGrade, buffId))
                    Logger.Warn($"Duplicate detected: itemId={itemId}, itemGrade={itemGrade}, buffId={buffId}");
            }
        }

        private void LoadItemRndAttrCategories(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM item_rnd_attr_categories";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var category = new ItemRndAttrCategory();
                category.Id = reader.GetInt32("id");
                category.CurrencyId = reader.GetInt32("currency_id");
                category.Desc = reader.GetString("desc");
                category.MaterialGradeLimit = reader.GetInt32("material_grade_limit");
                category.MaxEvolvingGrade = reader.GetInt32("max_evolving_grade");
                category.MessageGrade = reader.GetInt32("message_grade");
                category.ReRollItemId = reader.GetInt32("re_roll_item_id");

                _itemRndAttrCategories[category.Id] = category;
            }
        }

        private void LoadItemRndAttrCategoryMaterials(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM item_rnd_attr_category_materials";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var material = new ItemRndAttrCategoryMaterial();
                material.Id = reader.GetInt32("id");
                material.ItemRndAttrCategoryId = reader.GetInt32("item_rnd_attr_category_id");
                material.MaterialId = reader.GetInt32("material_id");

                if (!_itemRndAttrCategoryMaterials.ContainsKey(material.ItemRndAttrCategoryId))
                    _itemRndAttrCategoryMaterials[material.ItemRndAttrCategoryId] = new ConcurrentDictionary<int, ItemRndAttrCategoryMaterial>();
                _itemRndAttrCategoryMaterials[material.ItemRndAttrCategoryId][material.Id] = material;
            }
        }

        private void LoadItemRndAttrCategoryProperties(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM item_rnd_attr_category_properties";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var property = new ItemRndAttrCategoryProperty();
                property.Id = reader.GetInt32("id");
                property.BonusExpChance = reader.GetInt32("bonus_exp_chance");
                property.BonusExpMax = reader.GetInt32("bonus_exp_max");
                property.BonusExpMin = reader.GetInt32("bonus_exp_min");
                property.GainExp = reader.GetInt32("gain_exp");
                property.GoldMul = reader.GetInt32("gold_mul");
                property.GradeId = reader.GetInt32("grade_id");
                property.GradeExp = reader.GetInt32("grade_exp");
                property.ItemRndAttrCategoryId = reader.GetInt32("item_rnd_attr_category_id");
                property.MaxUnitModifierNum = reader.GetInt32("max_unit_modifier_num");

                if (!_itemRndAttrCategoryProperties.ContainsKey(property.ItemRndAttrCategoryId))
                    _itemRndAttrCategoryProperties[property.ItemRndAttrCategoryId] = new ConcurrentDictionary<int, ItemRndAttrCategoryProperty>();

                if (_itemRndAttrCategoryProperties[property.ItemRndAttrCategoryId].ContainsKey(property.GradeId))
                    Logger.Warn($"Duplicate gradeId {property.GradeId} found for categoryId {property.ItemRndAttrCategoryId}.");
                else
                    _itemRndAttrCategoryProperties[property.ItemRndAttrCategoryId][property.GradeId] = property;
            }
        }

        private void LoadItemRndAttrUnitModifierGroupSets(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM item_rnd_attr_unit_modifier_group_sets";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var groupSet = new ItemRndAttrUnitModifierGroupSet();
                groupSet.Id = reader.GetInt32("id");
                groupSet.ItemRndAttrCategoryId = reader.GetInt32("item_rnd_attr_category_id");
                groupSet.Name = reader.GetString("name");
                groupSet.PickNum = reader.GetInt32("pick_num");
                groupSet.Weight = reader.GetInt32("weight");

                if (!_itemRndAttrUnitModifierGroupSets.ContainsKey(groupSet.ItemRndAttrCategoryId))
                    _itemRndAttrUnitModifierGroupSets[groupSet.ItemRndAttrCategoryId] = new ConcurrentDictionary<int, ItemRndAttrUnitModifierGroupSet>();
                _itemRndAttrUnitModifierGroupSets[groupSet.ItemRndAttrCategoryId][groupSet.Id] = groupSet;
            }
        }

        private void LoadItemRndAttrUnitModifierGroups(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM item_rnd_attr_unit_modifier_groups";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var group = new ItemRndAttrUnitModifierGroup();
                group.Id = reader.GetInt32("id");
                group.FixedAttr = reader.GetBoolean("fixed_attr");
                group.ItemRndAttrUnitModifierGroupSetId = reader.GetInt32("item_rnd_attr_unit_modifier_group_set_id");
                group.UnitAttributeId = reader.GetInt32("unit_attribute_id");
                group.UnitModifierTypeId = reader.GetInt32("unit_modifier_type_id");
                group.Weight = reader.GetInt32("weight");

                if (!_itemRndAttrUnitModifierGroups.ContainsKey(group.ItemRndAttrUnitModifierGroupSetId))
                    _itemRndAttrUnitModifierGroups[group.ItemRndAttrUnitModifierGroupSetId] = new ConcurrentDictionary<int, ItemRndAttrUnitModifierGroup>();
                _itemRndAttrUnitModifierGroups[group.ItemRndAttrUnitModifierGroupSetId][group.Id] = group;

                if (!_itemRndAttributUnitModifierGroups.ContainsKey(group.Id))
                    _itemRndAttributUnitModifierGroups[group.Id] = new ItemRndAttrUnitModifierGroup();
                _itemRndAttributUnitModifierGroups[group.Id] = group;
            }
        }

        private void LoadItemRndAttrUnitModifiers(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM item_rnd_attr_unit_modifiers";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var modifier = new ItemRndAttrUnitModifier();
                modifier.Id = reader.GetInt32("id");
                modifier.GradeId = reader.GetInt32("grade_id");
                modifier.GroupId = reader.GetInt32("group_id");
                modifier.Max = reader.GetInt32("max");
                modifier.Min = reader.GetInt32("min");

                if (!_itemRndAttrUnitModifiers.ContainsKey(modifier.GroupId))
                    _itemRndAttrUnitModifiers[modifier.GroupId] = new ConcurrentDictionary<int, ItemRndAttrUnitModifier>();
                _itemRndAttrUnitModifiers[modifier.GroupId][modifier.GradeId] = modifier;
            }
        }
        #endregion Synthesis

        #region Socketing
        private void LoadItemSockets(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM item_sockets";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var itemSocket = new ItemSocket();
                itemSocket.Id = reader.GetInt32("id");
                itemSocket.ItemId = reader.GetInt32("item_id");
                itemSocket.BuffModifierTooltip = reader.GetString("buff_modifier_tooltip");
                itemSocket.EisetId = reader.GetInt32("eiset_id");
                itemSocket.EquipItemTagId = reader.GetInt32("equip_item_tag_id");
                itemSocket.EquipItemId = reader.GetInt32("equip_item_id");
                itemSocket.EquipSlotGroupId = reader.GetInt32("equip_slot_group_id");
                itemSocket.Extractable = reader.GetBoolean("extractable");
                itemSocket.IgnoreEquipItemTag = reader.GetBoolean("ignore_equip_item_tag");
                itemSocket.ItemSocketChanceId = reader.GetInt32("item_socket_chance_id");
                itemSocket.SkillModifierTooltip = reader.GetString("skill_modifier_tooltip");

                if (!_itemSockets.ContainsKey(itemSocket.ItemId))
                    _itemSockets[itemSocket.ItemId] = new ItemSocket();
                _itemSockets[itemSocket.ItemId] = itemSocket;
            }
        }

        private void LoadItemSocketLevelLimits(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM item_socket_level_limits";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var itemSocketLevelLimit = new ItemSocketLevelLimit();
                itemSocketLevelLimit.ItemId = reader.GetInt32("item_id");
                itemSocketLevelLimit.Level = reader.GetInt32("level");

                if (!_itemSocketLevelLimits.ContainsKey(itemSocketLevelLimit.ItemId))
                {
                    _itemSocketLevelLimits[itemSocketLevelLimit.ItemId] = itemSocketLevelLimit.Level;
                }
                else
                {
                    Logger.Warn($"Duplicate entry for item_socket_level_limits {itemSocketLevelLimit.ItemId}");
                    _itemSocketLevelLimits[itemSocketLevelLimit.ItemId] = itemSocketLevelLimit.Level;
                }
            }
        }

        private void LoadItemSocketNumLimits(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM item_socket_num_limits";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var itemSocketNumLimit = new ItemSocketNumLimit();
                itemSocketNumLimit.SlotId = reader.GetInt32("slot_id");
                itemSocketNumLimit.GradeId = reader.GetInt32("grade_id");
                itemSocketNumLimit.NumSocket = reader.GetInt32("num_socket");

                if (!_itemSocketNumLimits.ContainsKey(itemSocketNumLimit.SlotId))
                    _itemSocketNumLimits[itemSocketNumLimit.SlotId] = new ConcurrentDictionary<int, ItemSocketNumLimit>();
                _itemSocketNumLimits[itemSocketNumLimit.SlotId][itemSocketNumLimit.GradeId] = itemSocketNumLimit;
            }
        }

        private void LoadItemSocketChances(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM item_socket_chances";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var itemSocketChance = new ItemSocketChance();
                itemSocketChance.Id = reader.GetInt32("id");
                itemSocketChance.FailBreak = reader.GetBoolean("fail_break");
                itemSocketChance.CostRatio = reader.GetInt32("cost_ratio");

                if (!_itemSocketChances.ContainsKey(itemSocketChance.Id))
                {
                    _itemSocketChances[itemSocketChance.Id] = (itemSocketChance.FailBreak, itemSocketChance.CostRatio);
                }
                else
                {
                    Logger.Warn($"Duplicate entry for item_socket_chances {itemSocketChance.Id}");
                    _itemSocketChances[itemSocketChance.Id] = (itemSocketChance.FailBreak, itemSocketChance.CostRatio);
                }
            }
        }

        #endregion Socketing

        #region Mapping
        private void LoadItemChangeMappingGroups(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM item_change_mapping_groups";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var itemChangeMappingGroup = new ItemChangeMappingGroup();
                itemChangeMappingGroup.Id = reader.GetInt32("id");
                itemChangeMappingGroup.Disable = reader.GetInt32("disable");
                itemChangeMappingGroup.EvolvingExpInherit = reader.GetBoolean("evolving_exp_inherit");
                itemChangeMappingGroup.FailBonus = reader.GetInt32("fail_bonus");
                itemChangeMappingGroup.Name = reader.GetString("name");
                itemChangeMappingGroup.Selectable = reader.GetBoolean("selectable");
                itemChangeMappingGroup.Success = reader.GetInt32("success");


                if (!_itemChangeMappingGroups.ContainsKey(itemChangeMappingGroup.Id))
                    _itemChangeMappingGroups[itemChangeMappingGroup.Id] = new ItemChangeMappingGroup();
                _itemChangeMappingGroups[itemChangeMappingGroup.Id] = itemChangeMappingGroup;
            }
        }

        private void LoadItemChangeMappings(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM item_change_mappings";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var itemChangeMapping = new ItemChangeMapping();
                itemChangeMapping.Id = reader.GetInt32("id");
                itemChangeMapping.MappingGroupId = reader.GetInt32("mapping_group_id");
                itemChangeMapping.SourceGradeId = reader.GetInt32("source_grade_id");
                itemChangeMapping.SourceItemId = reader.GetInt32("source_item_id");
                itemChangeMapping.TargetItemId = reader.GetInt32("target_item_id");

                if (!_itemChangeMappings.ContainsKey(itemChangeMapping.Id))
                    _itemChangeMappings[itemChangeMapping.Id] = new ItemChangeMapping();
                _itemChangeMappings[itemChangeMapping.Id] = itemChangeMapping;
            }
        }
        #endregion Mapping

        #region GradeEnchant
        private void LoadEnchantScaleRatios(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM enchant_scale_ratios";
            command.Prepare();
            using var sqliteReader = command.ExecuteReader();
            using var reader = new SQLiteWrapperReader(sqliteReader);
            while (reader.Read())
            {
                var template = new EnchantScaleRatio();
                template.Id = reader.GetInt32("id");
                template.BreakRatio = reader.GetInt32("break_ratio");
                template.Cost = reader.GetInt32("cost");
                template.CurrencyId = reader.GetInt32("currency_id");
                template.DisableRatio = reader.GetInt32("disable_ratio");
                template.DownMax = reader.GetInt32("down_max");
                template.DownRatio = reader.GetInt32("down_ratio");
                template.GrateSuccessRatio = reader.GetInt32("grate_success_ratio");
                template.Name = reader.GetString("name");
                template.Scale = reader.GetInt32("scale");
                template.SuccessRatio = reader.GetInt32("success_ratio");

                _enchantScaleRatios.TryAdd(template.Id, template);
            }
        }

        private void LoadItemEnchantRatios(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM item_enchant_ratios";
            command.Prepare();
            using var sqliteReader = command.ExecuteReader();
            using var reader = new SQLiteWrapperReader(sqliteReader);
            while (reader.Read())
            {
                var template = new ItemEnchantRatio();
                template.ItemEnchantRatioGroupId = reader.GetInt32("item_enchant_ratio_group_id");
                template.Grade = reader.GetInt32("grade");
                template.GradeEnchantSuccessRatio = reader.GetInt32("grade_enchant_success_ratio");
                template.GradeEnchantGreatSuccessRatio = reader.GetInt32("grade_enchant_great_success_ratio");
                template.GradeEnchantBreakRatio = reader.GetInt32("grade_enchant_break_ratio");
                template.GradeEnchantDowngradeRatio = reader.GetInt32("grade_enchant_downgrade_ratio");
                template.GradeEnchantCost = reader.GetInt32("grade_enchant_cost");
                template.GradeEnchantDowngradeMin = reader.GetInt32("grade_enchant_downgrade_min");
                template.GradeEnchantDowngradeMax = reader.GetInt32("grade_enchant_downgrade_max");
                template.CurrencyId = reader.GetInt32("currency_id");
                template.GradeEnchantDisableRatio = reader.GetInt32("grade_enchant_disable_ratio");

                if (!_itemEnchantRatios.ContainsKey(template.ItemEnchantRatioGroupId))
                    _itemEnchantRatios[template.ItemEnchantRatioGroupId] = new ConcurrentDictionary<int, ItemEnchantRatio>();
                _itemEnchantRatios[template.ItemEnchantRatioGroupId][template.Grade] = template;
            }
        }

        private void LoadItemEnchantRatioGroups(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM item_enchant_ratio_groups";
            command.Prepare();
            using var sqliteReader = command.ExecuteReader();
            using var reader = new SQLiteWrapperReader(sqliteReader);
            while (reader.Read())
            {
                var group = new ItemEnchantRatioGroup();
                group.Id = reader.GetInt32("id");
                group.ItemImplId = reader.GetInt32("item_impl_id");
                group.ItemEnchantRatioKindId = reader.GetInt32("item_enchant_ratio_kind_id");

                _itemEnchantRatioGroups.TryAdd(group.Id, group);
            }
        }

        private void LoadItemEnchantRatioItems(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM item_enchant_ratio_items";
            command.Prepare();
            using var sqliteReader = command.ExecuteReader();
            using var reader = new SQLiteWrapperReader(sqliteReader);
            while (reader.Read())
            {
                var item = new ItemEnchantRatioItem();
                item.ItemEnchantRatioGroupId = reader.GetInt32("item_enchant_ratio_group_id");
                item.ItemId = reader.GetInt32("item_id");

                _itemEnchantRatioItems.TryAdd(item.ItemId, item.ItemEnchantRatioGroupId);
            }
        }

        private void LoadItemEnchantingGems(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM item_enchanting_gems";
            command.Prepare();
            using var sqliteReader = command.ExecuteReader();
            using var reader = new SQLiteWrapperReader(sqliteReader);
            while (reader.Read())
            {
                var gem = new ItemEnchantingGem();
                gem.Id = reader.GetInt32("id");
                gem.ItemId = reader.GetInt32("item_id");
                gem.BuffModifierTooltip = reader.IsDBNull("buff_modifier_tooltip") ? null : reader.GetString("buff_modifier_tooltip");
                gem.EisetId = reader.GetInt32("eiset_id");
                gem.EquipItemTagId = reader.GetInt32("equip_item_tag_id");
                gem.EquipItemId = reader.GetInt32("equip_item_id");
                gem.EquipLevel = reader.GetInt32("equip_level");
                gem.EquipSlotGroupId = reader.GetInt32("equip_slot_group_id");
                gem.GemVisualEffectId = reader.GetInt32("gem_visual_effect_id");
                gem.IgnoreEquipItemTag = reader.GetBoolean("ignore_equip_item_tag");
                gem.ItemGradeId = reader.GetInt32("item_grade_id");
                gem.SkillModifierTooltip = reader.IsDBNull("skill_modifier_tooltip") ? null : reader.GetString("skill_modifier_tooltip");

                _itemEnchantingGems.TryAdd(gem.Id, gem);
            }
        }

        private void LoadItemGradeEnchantingSupports(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM item_grade_enchanting_supports";
            command.Prepare();
            using var sqliteReader = command.ExecuteReader();
            using var reader = new SQLiteWrapperReader(sqliteReader);
            while (reader.Read())
            {
                var support = new ItemGradeEnchantingSupport();
                support.ItemId = reader.GetInt32("item_id");
                support.AddBreakMul = reader.GetInt32("add_break_mul");
                support.AddBreakRatio = reader.GetInt32("add_break_ratio");
                support.AddDisableMul = reader.GetInt32("add_disable_mul");
                support.AddDisableRatio = reader.GetInt32("add_disable_ratio");
                support.AddDowngradeMul = reader.GetInt32("add_downgrade_mul");
                support.AddDowngradeRatio = reader.GetInt32("add_downgrade_ratio");
                support.AddGreatSuccessGrade = reader.GetInt32("add_great_success_grade");
                support.AddGreatSuccessMul = reader.GetInt32("add_great_success_mul");
                support.AddGreatSuccessRatio = reader.GetInt32("add_great_success_ratio");
                support.AddSuccessMul = reader.GetInt32("add_success_mul");
                support.AddSuccessRatio = reader.GetInt32("add_success_ratio");
                support.Icons = reader.GetInt32("icons");
                support.ImplFlags = reader.GetInt32("impl_flags");
                support.ReqScaleMaxId = reader.GetInt32("req_scale_max_id");
                support.ReqScaleMinId = reader.GetInt32("req_scale_min_id");
                support.RequireGradeMax = reader.GetInt32("require_grade_max");
                support.RequireGradeMin = reader.GetInt32("require_grade_min");

                _itemGradeEnchantingSupports.TryAdd(support.ItemId, support);
            }
        }
        #endregion GradeEnchant

        private void LoadItemSlaveEquipments(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM item_slave_equipments";
            command.Prepare();
            using var sqliteReader = command.ExecuteReader();
            using var reader = new SQLiteWrapperReader(sqliteReader);
            while (reader.Read())
            {
                var itemSlaveEquipment = new ItemSlaveEquipment
                {
                    Id = reader.GetUInt32("id"),
                    ItemId = reader.GetUInt32("item_id"),
                    DoodadScale = reader.GetFloat("doodad_scale"),
                    DoodadId = reader.GetUInt32("doodad_id"),
                    RequireItemId = reader.GetUInt32("require_item_id"),
                    SlaveEquipPackId = reader.GetUInt32("slave_equip_pack_id"),
                    SlaveId = reader.GetUInt32("slave_id"),
                    SlotPackId = reader.GetUInt32("slot_pack_id")
                };

                _itemSlaveEquipments[itemSlaveEquipment.Id] = itemSlaveEquipment;
            }
        }

        private void LoadSlaveEquipmentEquipSlotPacks(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM slave_equipment_equip_slot_packs";
            command.Prepare();
            using var sqliteReader = command.ExecuteReader();
            using var reader = new SQLiteWrapperReader(sqliteReader);
            while (reader.Read())
            {
                var slotPack = new SlaveEquipmentEquipSlotPack
                {
                    Id = reader.GetInt32("id"),
                    GradeEnchantCost = reader.GetInt32("grade_enchant_cost"),
                    Name = reader.GetString("name")
                };

                _slaveEquipmentEquipSlotPacks[slotPack.Id] = slotPack;
            }
        }

        public void PostLoad()
        {
            // Handle any post-loading logic here
        }
        #endregion Sqlite
    }
}
