﻿using System.Collections.Generic;
using System.Drawing;

using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Utils;
using AAEmu.Game.Utils.Scripts;
using AAEmu.Game.Utils.Scripts.SubCommands;

namespace AAEmu.Game.Scripts.SubCommands.Doodads;

public class DoodadSpawnSubCommand : SubCommandBase
{
    public DoodadSpawnSubCommand()
    {
        Title = "[Doodad Spawn]";
        Description = "Add a new doodad of a specific template 3 meters in front of the player. Default yaw will use characters facing angle.";
        CallPrefix = $"{CommandManager.CommandPrefix}doodad spawn";
        AddParameter(new NumericSubCommandParameter<uint>("templateId", "template id", true));
        AddParameter(new NumericSubCommandParameter<float>("yaw", "yaw=<yaw facing degrees>", false, "yaw"));
    }

    public override void Execute(ICharacter character, string triggerArgument, IDictionary<string, ParameterValue> parameters, IMessageOutput messageOutput)
    {
        uint unitTemplateId = parameters["templateId"];
        if (!DoodadManager.Instance.Exist(unitTemplateId))
        {
            SendColorMessage(messageOutput, Color.Red, $"Doodad templateId:{unitTemplateId} don't exist");
            return;
        }

        using var charPos = character.Transform.CloneDetached();
        charPos.Local.AddDistanceToFront(3f);
        var defaultYaw = (float)MathUtil.CalculateAngleFrom(charPos, character.Transform);
        var newYaw = GetOptionalParameterValue(parameters, "yaw", defaultYaw).DegToRad();
        var doodadSpawner = new DoodadSpawner { Id = 0, UnitId = unitTemplateId };

        doodadSpawner.Position = charPos.CloneAsSpawnPosition();
        doodadSpawner.Position.Yaw = newYaw;
        doodadSpawner.Position.Pitch = 0;
        doodadSpawner.Position.Roll = 0;
        var createdDoodad = doodadSpawner.Spawn(0, 0, character.ObjId);

        if (parameters.ContainsKey("yaw"))
        {
            character.SendDebugMessage($"Doodad ObjId:{createdDoodad.ObjId}, Template:{unitTemplateId} spawned using yaw {newYaw.RadToDeg():0.#}° = {newYaw} rad");
        }
        else
        {
            character.SendDebugMessage($"Doodad ObjId:{createdDoodad.ObjId}, Template {unitTemplateId} spawned facing you, characters yaw {newYaw.RadToDeg():0.#}°");
        }
    }
}
