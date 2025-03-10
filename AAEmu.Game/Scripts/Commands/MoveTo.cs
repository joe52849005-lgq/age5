﻿using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Units.Route;
using AAEmu.Game.Utils.Scripts;

namespace AAEmu.Game.Scripts.Commands;

public class MoveTo : ICommand
{
    public string[] CommandNames { get; set; } = new[] { "moveto" };

    public void OnLoad()
    {
        CommandManager.Instance.Register("moveto", this);
    }

    public string GetCommandLineHelp()
    {
        return "<rec||save filename||go filename||back filename||stop||run||walk>";
    }

    public string GetCommandHelpText()
    {
        return
            "what is he doing:\n" +
            "- automatically writes the route to the file;\n" +
            "- you can load path data from a file;\n- moves along the route.\n\n" +
            "To start, you need to create the route (s), recording occurs as follows:\n" +
            "1. Start recording;\n" +
            "2. Take a route;\n" +
            "3. Stop recording.\n" +
            "=== here is an example file structure (x, y, z) ===\n" +
            "|15629.0|14989.02|141.2055|\n" +
            "|15628.0|14987.24|141.3826|\n" +
            "|15626.0|14983.88|141.3446|\n" +
            "===================================================;\n";
    }

    public void Execute(Character character, string[] args, IMessageOutput messageOutput)
    {
        var nameFile = "movefile";
        var cmd = "";
        Simulation moveTo;
        //bool walk = false;

        if (args.Length < 1)
        {
            character.SendDebugMessage("[MoveTo] /moveto <rec||save filename||go filename||back filename||stop||run||walk>");
            return;
        }

        if (args[0] == "rec" || args[0] == "stop" || args[0] == "run" || args[0] == "walk")
        {
            cmd = args[0];
        }
        else if (args.Length == 2)
        {
            cmd = args[0];
            nameFile = args[1];
        }
        else
        {
            CommandManager.SendErrorText(this, messageOutput, "there should be two parameters, a command and a file_name...");
            return;
        }

        CommandManager.SendNormalText(this, messageOutput, $"cmd: {cmd}, nameFile: {nameFile}");
        moveTo = character.Simulation; // take the AI movement
        moveTo.Npc = (Npc)character.CurrentTarget;
        if (moveTo.Npc == null)
        {
            CommandManager.SendNormalText(this, messageOutput, $"You need a target NPC to manage it!");
        }
        else
        {
            switch (cmd)
            {
                case "rec":
                    CommandManager.SendNormalText(this, messageOutput, $"start recording...");
                    moveTo.StartRecord(moveTo, character);
                    break;
                case "save":
                    CommandManager.SendNormalText(this, messageOutput, $"have finished recording...");
                    moveTo.MoveFileName = nameFile;
                    moveTo.StopRecord(moveTo);
                    break;
                case "go":
                    CommandManager.SendNormalText(this, messageOutput, $"walk go...");
                    moveTo.RunningMode = false;
                    moveTo.MoveFileName = nameFile;
                    moveTo.GoToPath((Npc)character.CurrentTarget, true);
                    break;
                case "back":
                    CommandManager.SendNormalText(this, messageOutput, $"walk back...");
                    moveTo.RunningMode = false;
                    moveTo.MoveFileName = nameFile;
                    moveTo.GoToPath((Npc)character.CurrentTarget, false);
                    break;
                case "run":
                    CommandManager.SendNormalText(this, messageOutput, $"turned on running mode...");
                    moveTo.RunningMode = true;
                    break;
                //case "walk":
                //    character.SendDebugMessage("[MoveTo] turned off running mode...");
                //    moveTo.RunningMode = walk;
                //    break;
                case "stop":
                    CommandManager.SendNormalText(this, messageOutput, $"standing still...");
                    moveTo.StopMove((Npc)character.CurrentTarget);
                    break;
            }
        }
    }
}
