using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Items.Templates;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Units;

using NLog;

namespace AAEmu.Game.Models.Game.World.Interactions;

public class MagicalEnchant : IWorldInteraction
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    public void Execute(BaseUnit caster, SkillCaster casterType, BaseUnit target, SkillCastTarget targetType, uint skillId, uint itemId, DoodadFuncTemplate objectFunc = null)
    {
        // Validate caster
        if (caster is not Character character)
        {
            Logger.Debug("Invalid caster type. Expected Character.");
            return;
        }

        // Validate target type
        if (targetType is not SkillCastItemTarget itemTarget)
        {
            Logger.Debug("Invalid target type. Expected SkillCastItemTarget.");
            return;
        }

        // Validate caster type
        if (casterType is not SkillItem skillItem)
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

        if (!(equipItem is { Template: EquipItemTemplate equipItemTemplate }))
        {
            Logger.Debug($"Attempting to upgrade a non-equipment item. Item={equipItem.Id}");
            return;
        }

        // Set the RuneId and GemIds
        //equipItem.RuneId = skillItem.ItemTemplateId; // Set the RuneId to the skill item template ID
        equipItem.GemIds[1] = skillItem.ItemTemplateId; // Set the first gem slot to the skill item template ID

        // let's spend the money
        var money = ItemGameData.Instance.GetGoldMul(equipItemTemplate.ItemRndAttrCategoryId, equipItem.Grade);
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
        character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.EnchantMagical, [new ItemUpdate(equipItem)], [], 4195041312));
        character.SendPacket(new SCEnchantMagicalResultPacket(true, targetItem.Id, skillItem.ItemTemplateId));

        // Log the action
        Logger.Debug($"MagicalEnchant executed by {character.Name} on item {itemTarget.Id} with skill item {skillItem.ItemTemplateId}");
    }
}
