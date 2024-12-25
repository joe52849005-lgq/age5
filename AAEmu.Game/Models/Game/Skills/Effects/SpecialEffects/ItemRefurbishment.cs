using System;

using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Formulas;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Items.Templates;
using AAEmu.Game.Models.Game.Skills.Effects.Enums;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class ItemRefurbishment : SpecialEffectAction
{
    protected override SpecialType SpecialEffectActionType => SpecialType.ItemRefurbishment;
    private int currentLevel = 0; // Assuming this is defined elsewhere in your class
    private int nextLevel = 0;
    private string name = "";

    public override void Execute(BaseUnit caster,
        SkillCaster casterObj,
        BaseUnit target,
        SkillCastTarget targetObj,
        CastAction castObj,
        Skill skill,
        SkillObject skillObject,
        DateTime time,
        int itemType,
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

        Logger.Debug($"Special effects: ItemRefurbishment itemType {itemType}, value2 {value2}, value3 {value3}, value4 {value4}, value5 {value5}, value6 {value6}, value7 {value7}");

        // Validate target type
        if (targetObj is not SkillCastItemTarget itemTarget)
        {
            Logger.Debug("Invalid target type. Expected SkillCastItemTarget.");
            return;
        }

        // Validate caster type
        if (casterObj is not SkillItem skillItem)
        {
            Logger.Debug("Invalid caster type. Expected SkillItem.");
            return;
        }

        // Get the target item
        var targetItem = character.Inventory.Bag.GetItemByItemId(itemTarget.Id);
        if (targetItem is null)
        {
            Logger.Debug($"Target item with ID {itemTarget.Id} not found in inventory.");
            return;
        }

        // Validate target item type
        if (targetItem is not EquipItem equipItem)
        {
            Logger.Debug("Target item is not an EquipItem.");
            return;
        }

        name = character.Name;
        currentLevel = equipItem.TemperPhysical;
        nextLevel = equipItem.TemperPhysical;
        var result = Upgrade();
        equipItem.GemIds[21] = (uint)nextLevel; // equipItem.TemperPhysical

        // let's spend the money
        var money = ItemGameData.GoldCost(equipItem, (ItemImpl)itemType, currentLevel + 1, FormulaKind.GradeTemperingCost);
        if (money == -1)
        {
            // No gold on template, invalid ?
            return;
        }

        if (character.Money < money)
        {
            character.SendErrorMessage(ErrorMessageType.NotEnoughMoney);
            return;
        }
        character.ChangeMoney(SlotType.Bag, -money);

        // Send packets to the client
        character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.ScaleCap, [new ItemUpdate(equipItem)], []));
        character.SendPacket(new SCItemTemperingResultPacket((byte)result, equipItem, 12, (ushort)currentLevel, (ushort)nextLevel));

        // Log the action
        Logger.Debug($"ItemRefurbishment executed by {name} on item {itemTarget.Id} with skill item {skillItem.ItemTemplateId}");
    }

    private GradeEnchantResult Upgrade()
    {
        if (currentLevel >= 30)
        {
            Logger.Debug("Cannot upgrade beyond level +30.");
            return GradeEnchantResult.Fail2;
        }

        var esr = ItemGameData.Instance.GetEnchantScaleRatio(currentLevel);

        var successChance = esr.SuccessRatio / 10000.0;
        var worseningChance = esr.DownRatio / 10000.0;

        var random = new Random();
        var roll = random.NextDouble();

        if (currentLevel < 10)
        {
            // Success is guaranteed
            nextLevel = currentLevel + 1;
            Logger.Debug($"Upgrade successful! Equipment is now {name} +{nextLevel}, successChance={successChance}, worseningChance={worseningChance}.");
            return GradeEnchantResult.Success;
        }

        if (currentLevel is >= 10 and < 18)
        {
            if (roll <= successChance)
            {
                nextLevel = currentLevel + 1;
                Logger.Debug($"Upgrade successful! Equipment is now {name} +{nextLevel}, successChance={successChance}, worseningChance={worseningChance}.");
                return GradeEnchantResult.Success;
            }

            Logger.Debug($"Upgrade failed. No changes to the equipment, successChance={successChance}, worseningChance={worseningChance}.");
            return GradeEnchantResult.Fail2;
        }

        if (currentLevel >= 18)
        {
            if (roll <= successChance)
            {
                nextLevel = currentLevel + 1;
                Logger.Debug($"Upgrade successful! Equipment is now {name} +{nextLevel}, successChance={successChance}, worseningChance={worseningChance}.");
                return GradeEnchantResult.Success;
            }

            if (roll <= successChance + worseningChance)
            {
                nextLevel = currentLevel - esr.DownMax;
                Logger.Debug($"Upgrade worsened! Equipment is now {name} +{nextLevel}, successChance={successChance}, worseningChance={worseningChance}.");
                return GradeEnchantResult.Downgrade;
            }

            Logger.Debug($"Upgrade failed. No changes to the equipment, successChance={successChance}, worseningChance={worseningChance}.");
            return GradeEnchantResult.Fail2;
        }

        return GradeEnchantResult.Fail2;
    }
}
