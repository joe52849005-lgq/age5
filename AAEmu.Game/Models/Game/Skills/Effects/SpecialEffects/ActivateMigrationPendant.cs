using System;

using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;

public class ActivateMigrationPendant : SpecialEffectAction
{
    protected override SpecialType SpecialEffectActionType => SpecialType.ActivateMigrationPendant;

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
        if (caster is not Character character)
        {
            return;
        }
        Logger.Debug("Special effects: ActivateMigrationPendant value1 {0}, value2 {1}, value3 {2}, value4 {3}", value1, value2, value3, value4);

        if (character.Stats.PageCount == 2)
        {
            switch (character.Stats.PageIndex)
            {
                case 0:
                    character.Stats.PageIndex = 1;
                    break;
                case 1:
                    character.Stats.PageIndex = 0;
                    break;
                default:
                    character.Stats.PageIndex = character.Stats.PageIndex;
                    break;
            }
        }
        else if (character.Stats.PageCount == 3)
        {
            switch (character.Stats.PageIndex)
            {
                case 0:
                    character.Stats.PageIndex = 1; // делаем активной 2 страницу
                    break;
                case 1:
                    character.Stats.PageIndex = 2; // делаем активной 3 страницу
                    break;
                case 2:
                    character.Stats.PageIndex = 0; // делаем активной первую страницу
                    break;
                default:
                    character.Stats.PageIndex = character.Stats.PageIndex;
                    break;
            }
        }
        // TODO определить, где берется сумма
        character.SubtractMoney(SlotType.Bag, 33000);

        character.SendPacket(new SCBlessUthstinSelectPagePacket(
            character.ObjId,
            true,
            character.Stats.PageIndex
        ));
    }
}
