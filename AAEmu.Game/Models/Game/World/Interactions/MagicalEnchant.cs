using System.Collections.Generic;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Units;

using NLog;

namespace AAEmu.Game.Models.Game.World.Interactions;

public class MagicalEnchant : IWorldInteraction
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    public void Execute(BaseUnit caster, SkillCaster casterType, BaseUnit target, SkillCastTarget targetType,
        uint skillId, uint itemId, DoodadFuncTemplate objectFunc = null)
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

        // Set the RuneId and GemIds
        equipItem.RuneId = skillItem.ItemTemplateId; // Set the RuneId to the skill item template ID
        equipItem.GemIds[0] = skillItem.ItemTemplateId; // Set the first gem slot to the skill item template ID

        // Send packets to the client
        character.SendPacket(new SCEnchantMagicalResultPacket(true, targetItem.Id, skillItem.ItemTemplateId));
        character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.EnchantMagical,
            [new ItemUpdate(equipItem)],
            []));

        // Log the action
        Logger.Debug("MagicalEnchant executed by {0} on item {1} with skill item {2}", character.Name, itemTarget.Id, skillItem.ItemTemplateId);
    }
}
