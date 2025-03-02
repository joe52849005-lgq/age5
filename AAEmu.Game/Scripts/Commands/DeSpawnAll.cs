﻿using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Utils.Scripts;

namespace AAEmu.Game.Scripts.Commands;

public class DeSpawnAll : ICommand
{
    public string[] CommandNames { get; set; } = new string[] { "despawnall", "despawn_all" };

    public void OnLoad()
    {
        CommandManager.Instance.Register(CommandNames, this);
    }

    public string GetCommandLineHelp()
    {
        return "";
    }

    public string GetCommandHelpText()
    {
        return "De-spawns all world objects";
    }

    public void Execute(Character character, string[] args, IMessageOutput messageOutput)
    {
        var removedCount = SpawnManager.Instance.DeSpawnAll((byte)character.Transform.WorldId);
        CommandManager.SendNormalText(this, messageOutput, $"Removed {removedCount} objects");
    }
}
