using System;

using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Units;

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
        // TODO ...
        if (caster is Character)
        {
            Logger.Debug("Special effects: Skinize value1 {0}, value2 {1}, value3 {2}, value4 {3}", value1, value2, value3, value4);
        }

        if (caster is not Character character)
        {
            return;
        }

        if (targetObj is not SkillCastItemTarget itemTarget)
        {
            return;
        }

        var itemToImage = character.Inventory.GetItemById(itemTarget.Id);
        if (itemToImage == null)
        {
            return;
        }

        if (itemToImage.HasFlag(ItemFlag.Skinized))
        {
            // Already an image item
            return;
        }

        if (casterObj is not SkillItem powderSkillItem)
        {
            return;
        }

        var powderItem = character.Inventory.GetItemById(powderSkillItem.ItemId);
        if (powderItem == null)
        {
            return;
        }

        if (powderItem.Count < 1)
        {
            return;
        }

        itemToImage.SetFlag(ItemFlag.Skinized);
        character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.Sknize,
        [
            new ItemUpdateSecurity(itemToImage, 9, 1, false, false, false)
        ], [], 1));

        //if (character.Inventory.Bag.ConsumeItem(ItemTaskType.SkillEffectConsumption, powderItem.TemplateId, 1, null) <= 0)
        if (powderItem.HoldingContainer.ConsumeItem(ItemTaskType.SkillEffectConsumption, powderItem.TemplateId, 1, null) <= 0)
        {
            character.SendErrorMessage(ErrorMessageType.FailedToUseItem);
            Logger.Error($"Couldn't for Sknize item {powderItem.TemplateId}");
        }
    }
}
