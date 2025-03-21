﻿using System;
using System.Collections.Generic;

using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Items.Templates;
using AAEmu.Game.Utils.Scripts;
using AAEmu.Game.Utils.Scripts.SubCommands;

namespace AAEmu.Game.Scripts.SubCommands.Items;

public class ItemUnwrapSubCommand : SubCommandBase
{
    public ItemUnwrapSubCommand()
    {
        Title = "[Item]";
        Description = "Set target item's unwrap time to now, or to expire a specified amount of minutes from now.";
        CallPrefix = $"{CommandManager.CommandPrefix}item unwrap";
        AddParameter(new NumericSubCommandParameter<ulong>("itemId", "item id", true));
        AddParameter(
            new NumericSubCommandParameter<float>("minutes", "minutes=-1", false, -1f, 1000000f)
            {
                DefaultValue = -1f
            });
    }

    public override void Execute(ICharacter character, string triggerArgument, IDictionary<string, ParameterValue> parameters, IMessageOutput messageOutput)
    {
        //Character addTarget;
        var selfCharacter = (Character)character;

        ulong itemId = parameters["itemId"];
        var minutes = GetOptionalParameterValue<float>(parameters, "minutes", -1);

        var item = ItemManager.Instance.GetItemByItemId(itemId);

        if (item == null)
        {
            character.SendDebugMessage($"No item could be found with Id: {itemId}");
            return;
        }

        if (!(item.Template is EquipItemTemplate equipItemTemplate))
        {
            character.SendDebugMessage($"Item @ITEM_NAME({item.TemplateId}) is not a equipment item, Id: {itemId}");
            return;
        }

        var newTime = DateTime.UtcNow;
        character.SendDebugMessage($"Item unwrap using {minutes}");
        if (minutes >= 0 && equipItemTemplate.ChargeLifetime > 0)
        {
            newTime = newTime.AddMinutes(equipItemTemplate.ChargeLifetime * -1).AddMinutes(minutes);
        }

        item.UnpackTime = newTime;

        item.SetFlag(ItemFlag.Unpacked);
        if (item.Template.BindType == ItemBindType.BindOnUnpack)
        {
            item.SetFlag(ItemFlag.SoulBound);
        }

        var updateItemTask = new ItemUpdateSecurity(item, (byte)item.ItemFlags, 0, item.HasFlag(ItemFlag.Secure), item.HasFlag(ItemFlag.Secure), item.ItemFlags.HasFlag(ItemFlag.Unpacked));
        character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.ItemTaskThistimeUnpack, updateItemTask, new List<ulong>()));
        if (equipItemTemplate.ChargeLifetime > 0)
        {
            character.SendPacket(new SCSyncItemLifespanPacket(true, item.Id, item.TemplateId, item.UnpackTime));
        }

        character.SendDebugMessage($"Item @ITEM_NAME({item.TemplateId}) unwrap time set to {newTime}");
    }
}
