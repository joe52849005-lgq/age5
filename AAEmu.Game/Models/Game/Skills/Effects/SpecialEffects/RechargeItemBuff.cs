using System;
using System.Collections.Generic;

using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class RechargeItemBuff : SpecialEffectAction
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
        // TODO ...
        if (caster is Character character)
        {
            Logger.Debug($"Special effects: RechargeItemBuff value1 {value1}, value2 {value2}, value3 {value3}, value4 {value4}, value5 {value5}, value6 {value6}, value7 {value7}");

            var item = ItemManager.Instance.GetItemByItemId(((SkillCastItemTarget)targetObj).Id);
            item.ChargeTime = DateTime.UtcNow;
            var tasks = new List<ItemTask> { new ItemUpdate(item) };
            character.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.RechargeBuff, tasks, []));
        }
    }
}
