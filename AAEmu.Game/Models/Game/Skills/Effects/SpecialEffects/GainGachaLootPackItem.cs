using System;

using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class GainGachaLootPackItem : SpecialEffectAction
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
        int value4)
    {
        // TODO ...
        if (caster is Character character)
        {
            Logger.Debug("Special effects: GainGachaLootPackItem value1 {0}, value2 {1}, value3 {2}, value4 {3}", value1, value2, value3, value4);

            if (casterObj is SkillItem crate)
            {
                var crateItem = ItemManager.Instance.GetItemByItemId(crate.ItemId);
                if (crateItem is null)
                {
                    return;
                }
                if (targetObj is SkillCastItemTarget keyLock)
                {
                    var keyLockItem = ItemManager.Instance.GetItemByItemId(keyLock.Id);
                    if (keyLockItem is null)
                    {
                        return;
                    }

                    // TODO организовать получение предметов из сундука
                    //var item = LootGameData.Instance.GetPack(templateId);
                    var money = 500u;
                    var moneyCount = 110957;
                    var newItem1 = ItemManager.Instance.Create(money, moneyCount, 0);
                    var newItemtemplateId = 46156u; // ID=46156, Superior Glow Lunarite
                    var newItem2 = ItemManager.Instance.Create(newItemtemplateId, 1, 2);

                    character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.SkillEffectGainItemWithPos,
                    [
                        //new ItemRemove(crateItem),
                        crateItem.Count > 1 ? new ItemCountUpdate(crateItem, -1) : new ItemRemove(crateItem),
                        //new ItemRemove(keyLockItem),
                        keyLockItem.Count > 1 ? new ItemCountUpdate(keyLockItem, -1) : new ItemRemove(keyLockItem),
                        new MoneyChange(moneyCount), // TODO золото
                        new ItemAdd(newItem2), // TODO предмет
                    ], [], 0xFFFFFFFF));

                    (uint id, byte type, int stack)[] items =
                    [
                        (money, 0, moneyCount),
                        (keyLockItem.TemplateId, 2, 1)
                    ];

                    character.SendPacket(new SCGachaLootPackItemLogPacket((byte)items.Length, items));

                    var errorMessage = ErrorMessageType.NoErrorMessage;
                    var count = 0;
                    var itemCount = items.Length;
                    var finish = true;
                    Item[] items2 = [newItem1, newItem2];

                    character.SendPacket(new SCGachaLootPackItemResultPacket(errorMessage, count, itemCount, finish, items2));
                }
            }

            //if (character.Inventory.Bag.ConsumeItem(ItemTaskType.BlessUthstinChangeStats, item.TemplateId, 1, item) <= 0)
            //{
            //    character.SendErrorMessage(ErrorMessageType.NotEnoughItem);
            //    Logger.Debug($"Not Enough Item {item.TemplateId}");
            //}
        }
    }
}
