using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.World.Interactions;


public class Cutdown : IWorldInteraction
{
    public void Execute(BaseUnit caster, SkillCaster casterType, BaseUnit target, SkillCastTarget targetType,
        uint skillId, uint doodadId, DoodadFuncTemplate objectFunc = null)
    {
        if (target is Doodad doodad)
        {
            doodad.Use(caster, skillId);
            if (doodad.TemplateId != 8312) // ID=8312, Гигантское дерево торговцев, False, [Deforestation - Trees], (2), 100, 1
            {
                caster.BroadcastPacket(new SCVegetationCutdowningPacket(caster.ObjId, doodad.ObjId), true);
            }
        }
    }
}
