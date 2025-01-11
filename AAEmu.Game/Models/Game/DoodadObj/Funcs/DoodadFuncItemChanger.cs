using AAEmu.Game.Models.Game.Char;
using System.Linq;

using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs;

public class DoodadFuncItemChanger : DoodadPhaseFuncTemplate
{
    // doodad_phase_funcs
    public int ItemCount { get; set; }
    public int ItemId { get; set; }
    public int NextPhase { get; set; }
    public int SkillId { get; set; }

    public override bool Use(BaseUnit caster, Doodad owner)
    {
        if (caster is not Character)
        {
            Logger.Trace($"DoodadFuncItemChanger: Id={Id}, ItemCount={ItemCount}, ItemId={ItemId}, NextPhase={NextPhase}, SkillId={SkillId}");
            return false;
        }

        Logger.Debug($"DoodadFuncItemChanger: Id={Id}, ItemCount={ItemCount}, ItemId={ItemId}, NextPhase={NextPhase}, SkillId={SkillId}");

        owner.OverridePhase = NextPhase;
        return true;
    }
}
