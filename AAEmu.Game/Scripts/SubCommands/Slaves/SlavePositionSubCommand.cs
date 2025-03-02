﻿using System.Collections.Generic;
using System.Drawing;

using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Utils;
using AAEmu.Game.Utils.Scripts;
using AAEmu.Game.Utils.Scripts.SubCommands;

namespace AAEmu.Game.Scripts.SubCommands.Slaves;

public class SlavePositionSubCommand : SubCommandBase
{
    public SlavePositionSubCommand()
    {
        Title = "[Slave Position]";
        Description = "Change slave position and angle - All positions are optional use all or only the ones you want to change (Use yaw to rotate Slave)";
        CallPrefix = $"{CommandManager.CommandPrefix}slave position||pos";
        AddParameter(new StringSubCommandParameter("target", "target", true, "target", "id"));
        AddParameter(new NumericSubCommandParameter<uint>("ObjId", "object id", false));
        AddParameter(new NumericSubCommandParameter<float>("x", "x=<new x>", false, "x"));
        AddParameter(new NumericSubCommandParameter<float>("y", "y=<new y>", false, "y"));
        AddParameter(new NumericSubCommandParameter<float>("z", "z=<new z>", false, "z"));
        AddParameter(new NumericSubCommandParameter<float>("roll", "roll=<new roll degrees>", false, "roll", 0, 360));
        AddParameter(new NumericSubCommandParameter<float>("pitch", "pitch=<new pitch degrees>", false, "pitch", 0, 360));
        AddParameter(new NumericSubCommandParameter<float>("yaw", "yaw=<new yaw degrees>", false, "yaw", 0, 360));
    }

    public override void Execute(ICharacter character, string triggerArgument,
        IDictionary<string, ParameterValue> parameters, IMessageOutput messageOutput)
    {
        Models.Game.Units.Slave slave;
        if (parameters.TryGetValue("ObjId", out var objId))
        {
            slave = (Models.Game.Units.Slave)WorldManager.Instance.GetGameObject(objId);
            if (slave is null)
            {
                SendColorMessage(messageOutput, Color.Red, $"Slave with objId {objId} does not exist");
                return;
            }
        }
        else
        {
            var currentTarget = ((Character)character).CurrentTarget;
            if (currentTarget is null || !(currentTarget is Models.Game.Units.Slave))
            {
                SendColorMessage(messageOutput, Color.Red, "You need to target a Slave first");
                return;
            }

            slave = (Models.Game.Units.Slave)currentTarget;
        }

        var x = GetOptionalParameterValue(parameters, "x", slave.Transform.Local.Position.X);
        var y = GetOptionalParameterValue(parameters, "y", slave.Transform.Local.Position.Y);
        var z = GetOptionalParameterValue(parameters, "z", slave.Transform.Local.Position.Z);
        var yaw = GetOptionalParameterValue(parameters, "yaw", slave.Transform.Local.Rotation.Z.RadToDeg()).DegToRad();
        var pitch = GetOptionalParameterValue(parameters, "pitch", slave.Transform.Local.Rotation.Y.RadToDeg()).DegToRad();
        var roll = GetOptionalParameterValue(parameters, "roll", slave.Transform.Local.Rotation.X.RadToDeg()).DegToRad();

        SendMessage(messageOutput, $"Slave ObjId:{slave.ObjId} TemplateId:{slave.TemplateId}, x:{x}, y:{y}, z:{z}, roll:{roll.RadToDeg():0.#}°, pitch:{pitch.RadToDeg():0.#}°, yaw:{yaw.RadToDeg():0.#}°");

        slave.Transform.Local.SetPosition(x, y, z, roll, pitch, yaw);
        slave.Hide();
        slave.Show();
    }
}
