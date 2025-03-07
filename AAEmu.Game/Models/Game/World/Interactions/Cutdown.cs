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
            if (doodad.TemplateId is not (7420 or 8312)) // ID=7420 Cornucopia Tree, ID=8312 Majestic Tree
            {
                caster.BroadcastPacket(new SCVegetationCutdowningPacket(caster.ObjId, doodad.ObjId), true);
            }
        }
    }
}
