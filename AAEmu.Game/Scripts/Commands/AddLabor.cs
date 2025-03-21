﻿using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Utils.Scripts;

namespace AAEmu.Game.Scripts.Commands;

public class AddLabor : ICommand
{
    public string[] CommandNames { get; set; } = new string[] { "labor", "addlabor", "add_labor" };

    public void OnLoad()
    {
        CommandManager.Instance.Register(CommandNames, this);
    }

    public string GetCommandLineHelp()
    {
        return "(target) <amount> [vocationSkillId]";
    }

    public string GetCommandHelpText()
    {
        // Optional TODO: Add the values by extracting them from actability_groups ?
        return
            "Add or remove <amount> of labor. If [vocationSkillId] is provided, then target vocation skill also gains a amount of points.\n" +
            "(1)Alchemy, (2)Construction, (3)Cooking, (4)Handicrafts, (5)Husbandry, (6)Farming, (7)Fishing, (8)Logging, (9)Gathering, (10)Machining, " +
            "(11)Metalwork, (12)Printing, (13)Mining, (14)Masonry, (15)Tailoring, (16)Leatherwork, (17)Weaponry, (18)Carpentry, (20)Larceny, " +
            "(21)Nuian, (22)Elven, (23)Dwarven, (25)Harani, (26)Firran, (27)Warborn, (29)Nuia Dialect, (30)Haranya Dialect, " +
            "(31)Commerce, (33)Artistry, (34)Exploration";
    }

    public void Execute(Character character, string[] args, IMessageOutput messageOutput)
    {
        if (args.Length == 0)
        {
            CommandManager.SendDefaultHelpText(this, messageOutput);
            return;
        }

        var targetPlayer = WorldManager.GetTargetOrSelf(character, args[0], out var firstArg);

        short amount = 0;

        if (args.Length > firstArg + 0 && short.TryParse(args[firstArg + 0], out var argAmount))
        {
            amount = argAmount;
        }

        var vocationSkillId = 0;

        if (args.Length > firstArg + 1 && int.TryParse(args[firstArg + 1], out var argVocationSkillId))
        {
            vocationSkillId = argVocationSkillId;
        }

        targetPlayer.ChangeLabor(amount, vocationSkillId);
        if (character.Id != targetPlayer.Id)
        {
            CommandManager.SendNormalText(this, messageOutput, $"added {amount} labor to {targetPlayer.Name}");
            targetPlayer.SendDebugMessage($"[GM] {character.Name} has changed your labor by {amount}");
        }
    }
}
