using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.GameData.Framework;
using AAEmu.Game.Models.Game.Items.ItemRndAttr;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Utils.DB;

using Microsoft.Data.Sqlite;

using NLog;

namespace AAEmu.Game.GameData
{
    [GameData]
    public class ItemGameData : Singleton<ItemGameData>, IGameDataLoader
    {
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();
        private Random _random = new();

        private ConcurrentDictionary<uint, ConcurrentDictionary<byte, uint>> _itemGradeBuffs;
        private ConcurrentDictionary<int, ItemRndAttrCategory> _itemRndAttrCategories;
        private ConcurrentDictionary<int, ConcurrentDictionary<int, ItemRndAttrCategoryMaterial>> _itemRndAttrCategoryMaterials;
        private ConcurrentDictionary<int, ConcurrentDictionary<int, ItemRndAttrCategoryProperty>> _itemRndAttrCategoryProperties;
        private ConcurrentDictionary<int, ConcurrentDictionary<int, ItemRndAttrUnitModifierGroupSet>> _itemRndAttrUnitModifierGroupSets;
        private ConcurrentDictionary<int, ConcurrentDictionary<int, ItemRndAttrUnitModifierGroup>> _itemRndAttrUnitModifierGroups;
        private ConcurrentDictionary<int, ItemRndAttrUnitModifierGroup> _itemRndAttributUnitModifierGroups;
        private ConcurrentDictionary<int, ConcurrentDictionary<int, ItemRndAttrUnitModifier>> _itemRndAttrUnitModifiers;

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

            var random = new Random();
            var availableAttributes = unitModifierGroupSets.Values.ToList();
            foreach (var groupSet in availableAttributes)
            {
                var pickNum = groupSet.PickNum; // how much can you take from the list

                if (!_itemRndAttrUnitModifierGroups.TryGetValue(groupSet.Id, out var unitModifierGroups))
                    continue;

                var allAttributes = unitModifierGroups.Values.ToList();

                // remove existing attributes from the list 
                var deletes = new List<ItemRndAttrUnitModifierGroup>();
                foreach (var group in allAttributes)
                {
                    foreach (var existingAttribute in existingAttributes)
                    {
                        if (group.Id == existingAttribute)
                        {
                            deletes.Add(group);
                        }
                    }
                }

                foreach (var delete in deletes)
                {
                    allAttributes.Remove(delete);
                    pickNum--;
                }

                if (allAttributes.Count <= 0 || pickNum <= 0)
                    continue;

                for (var i = 0; i < pickNum; i++)
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
        /// <returns></returns>
        public (int id, int attribute, int value) ReplaceSelectAttribute(int categoryId, byte grade, List<int> existingAttributes)
        {
            if (!_itemRndAttrUnitModifierGroupSets.TryGetValue(categoryId, out var unitModifierGroupSets))
                return (0, 0, 0);

            var random = new Random();
            var availableAttributes = unitModifierGroupSets.Values.ToList();

            foreach (var groupSet in availableAttributes)
            {
                if (!_itemRndAttrUnitModifierGroups.TryGetValue(groupSet.Id, out var unitModifierGroups))
                    continue;

                var allAttributes = unitModifierGroups.Values.ToList();

                // remove existing attributes from the list 
                var deletes = new List<ItemRndAttrUnitModifierGroup>();
                foreach (var group in allAttributes)
                {
                    foreach (var existingAttribute in existingAttributes)
                    {
                        if (group.Id == existingAttribute)
                        {
                            deletes.Add(group);
                        }
                    }
                }

                foreach (var delete in deletes)
                {
                    allAttributes.Remove(delete);
                }


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
            if (_itemRndAttrUnitModifiers.TryGetValue(groupId, out var modifiers))
            {
                modifiers.TryGetValue(grade, out var modifier);

                if (modifier != null)
                {
                    var random = new Random();
                    var min = modifier.Min; // can be negative
                    var max = modifier.Max; // can also be negative

                    // Check and swap if necessary
                    if (min > max)
                    {
                        // Swap places
                        var temp = min;
                        min = max;
                        max = temp;
                    }

                    // Generate a random number 
                    var value = random.Next(min, max + 1); // +1 if you need to enable max
                    return value;
                }
            }

            return 0;
        }

        public ItemRndAttrUnitModifier GetUnitModifier(int groupId, int modifierId)
        {
            ItemRndAttrUnitModifier modifier = null;
            if (_itemRndAttrUnitModifiers.TryGetValue(groupId, out var modifiers))
                modifiers.TryGetValue(modifierId, out modifier);
            return modifier;
        }

        public void Load(SqliteConnection connection, SqliteConnection connection2)
        {
            InitializeDictionaries();
            LoadItemGradeBuffs(connection);
            LoadItemRndAttrCategories(connection);
            LoadItemRndAttrCategoryMaterials(connection);
            LoadItemRndAttrCategoryProperties(connection);
            LoadItemRndAttrUnitModifierGroupSets(connection);
            LoadItemRndAttrUnitModifierGroups(connection);
            LoadItemRndAttrUnitModifiers(connection);
        }

        private void InitializeDictionaries()
        {
            _itemGradeBuffs = new ConcurrentDictionary<uint, ConcurrentDictionary<byte, uint>>();
            _itemRndAttrCategories = new ConcurrentDictionary<int, ItemRndAttrCategory>();
            _itemRndAttrCategoryMaterials = new ConcurrentDictionary<int, ConcurrentDictionary<int, ItemRndAttrCategoryMaterial>>();
            _itemRndAttrCategoryProperties = new ConcurrentDictionary<int, ConcurrentDictionary<int, ItemRndAttrCategoryProperty>>();
            _itemRndAttrUnitModifierGroupSets = new ConcurrentDictionary<int, ConcurrentDictionary<int, ItemRndAttrUnitModifierGroupSet>>();
            _itemRndAttrUnitModifierGroups = new ConcurrentDictionary<int, ConcurrentDictionary<int, ItemRndAttrUnitModifierGroup>>();
            _itemRndAttributUnitModifierGroups = new ConcurrentDictionary<int, ItemRndAttrUnitModifierGroup>();
            _itemRndAttrUnitModifiers = new ConcurrentDictionary<int, ConcurrentDictionary<int, ItemRndAttrUnitModifier>>();
        }

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

        public void PostLoad()
        {
            // Handle any post-loading logic here
        }
    }
}
