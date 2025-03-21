﻿using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.StaticValues;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs;

public class DoodadFuncSpawnGimmick : DoodadPhaseFuncTemplate
{
    public uint GimmickId { get; set; }
    public FactionsEnum FactionId { get; set; }
    public float Scale { get; set; }
    public float OffsetX { get; set; }
    public float OffsetY { get; set; }
    public float OffsetZ { get; set; }
    public float VelocityX { get; set; }
    public float VelocityY { get; set; }
    public float VelocityZ { get; set; }
    public float AngleX { get; set; }
    public float AngleY { get; set; }
    public float AngleZ { get; set; }
    public int NextPhase { get; set; }

    public override bool Use(BaseUnit caster, Doodad owner)
    {
        Logger.Info($"DoodadFuncSpawnGimmick GimmickId={GimmickId}, scale={Scale}");
        if (caster is Character)
        {
            //I think this is used to reschedule anything that needs triggered at a specific gametime
            owner.OverridePhase = NextPhase;
            return true;
        }

        return false;
    }
}
