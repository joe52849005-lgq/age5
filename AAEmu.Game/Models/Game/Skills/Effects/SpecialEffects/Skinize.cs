using System;
using System.Collections.Generic;

using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Units;

using NLog;
namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class Skinize : SpecialEffectAction
{
    protected override SpecialType SpecialEffectActionType => SpecialType.Skinize;

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
        // Validate caster
        if (caster is not Character character)
        {
            return;
        }

        // Log values for debugging
        Logger.Debug("Special effects: Skinize value1 {0}, value2 {1}, value3 {2}, value4 {3}", value1, value2, value3, value4);

        // Validate target
        if (targetObj is not SkillCastItemTarget itemTarget)
        {
            Logger.Debug($"Invalid target type: {targetObj?.GetType().Name ?? "null"} for skinizing.");
            return;
        }

        // Get the item to be skinized
        var itemToImage = character.Inventory.GetItemById(itemTarget.Id);
        if (itemToImage == null)
        {
            Logger.Debug($"Item with ID {itemTarget.Id} not found in inventory.");
            return;
        }

        // Check if the item is already skinized
        if (itemToImage.HasFlag(ItemFlag.Skinized))
        {
            Logger.Debug($"Item {itemToImage.Id} is already skinized.");
            return;
        }

        // Get the powder item
        if (casterObj is not SkillItem powderSkillItem)
        {
            Logger.Debug($"Invalid caster object type: {casterObj?.GetType().Name ?? "null"} for skinizing.");
            return;
        }

        var powderItem = character.Inventory.GetItemById(powderSkillItem.ItemId);
        if (powderItem == null || powderItem.Count < 1)
        {
            Logger.Debug($"Powder item with ID {powderSkillItem.ItemId} not found or has insufficient count.");
            return;
        }

        // Mark the item as skinized
        itemToImage.SetFlag(ItemFlag.Skinized);

        // Send success packet to the client
        character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.Sknize,
            new List<ItemTask> { new ItemUpdateSecurity(itemToImage, 9, 1, false, false, false) },
            new List<ulong>(),
            1));

        // Consume the powder item
        if (powderItem.HoldingContainer.ConsumeItem(ItemTaskType.SkillEffectConsumption, powderItem.TemplateId, 1, null) <= 0)
        {
            character.SendErrorMessage(ErrorMessageType.FailedToUseItem);
            Logger.Debug($"Failed to consume powder item {powderItem.TemplateId} for skinizing. Item count: {powderItem.Count}.");
        }
    }
}
