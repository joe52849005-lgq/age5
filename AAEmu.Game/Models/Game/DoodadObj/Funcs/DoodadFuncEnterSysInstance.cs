﻿using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.StaticValues;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs;

public class DoodadFuncEnterSysInstance : DoodadFuncTemplate
{
    // doodad_funcs
    public uint ZoneId { get; set; }
    public FactionsEnum FactionId { get; set; }

    public override void Use(BaseUnit caster, Doodad owner, uint skillId, int nextPhase = 0)
    {
            Logger.Info($"DoodadFuncEnterSysInstance, ZoneId: {ZoneId}, FactionId: {FactionId}");
            if (caster is Character character)
            {
                if (character.MainWorldPosition == null)
                {
                    character.MainWorldPosition = character.Transform.CloneDetached(character); // сохраним координаты для возврата в основной мир
                }
                else if (character.Transform.WorldId == 0)
                {
                    character.MainWorldPosition = character.Transform.CloneDetached(character); // сохраним координаты для возврата в основной мир
                }

                IndunManager.Instance.RequestSysInstance(character, ZoneId);
            }
        }
}
