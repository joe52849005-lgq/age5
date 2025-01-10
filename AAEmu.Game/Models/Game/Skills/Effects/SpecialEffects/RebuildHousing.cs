using System;
using System.Linq;

using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Housing;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class RebuildHousing : SpecialEffectAction
{
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

        Logger.Debug($"Special effects: RebuildHousing value1 {value1}, value2 {value2}, value3 {value3}, value4 {value4}, value5 {value5}, value6 {value6}, value7 {value7}");

        if (target is not House house)
        {
            return;
        }

        // Get skillId
        if (castObj is not CastSkill castSkill)
        {
            Logger.Debug("Invalid cast type. Expected CastSkill.");
            return;
        }

        // Get housingId
        if (skillObject is not SkillObjectRebuildHousingSupport skillObjectRebuildHousing)
        {
            Logger.Debug("Invalid skillObject type. Expected SkillObjectRebuildHousingSupport.");
            return;
        }

        // Get & Check materials
        var housingRebuildingId = HousingManager.Instance.GetHousingRebuildingId((int)castSkill.SkillId, (int)skillObjectRebuildHousing.HousingId);
        var materials = HousingManager.Instance.GetMaterialsByHousingRebuildingId(housingRebuildingId);
        if (materials.Any(material => !character.Inventory.CheckItems(SlotType.Bag, (uint)material.ItemId, material.Count)))
        {
            character.SendErrorMessage(ErrorMessageType.NotEnoughRequiredItem);
            return;
        }

        // All seems to be in order, roll item, consume items and send the results
        if (materials.Any(material => character.Inventory.Bag.ConsumeItem(ItemTaskType.HouseRebuild, (uint)material.ItemId, material.Count, null) <= 0))
        {
            character.SendErrorMessage(ErrorMessageType.BagInvalidItem);
            return;
        }

        HousingManager.Instance.DemolishBeforeRebuilding(character.Connection, house, false, false);
        HousingManager.Instance.Rebuild(character.Connection, skillObjectRebuildHousing.HousingId, skillObjectRebuildHousing.X, skillObjectRebuildHousing.Y, skillObjectRebuildHousing.Z, skillObjectRebuildHousing.Rot, house.Name);
    }
}
