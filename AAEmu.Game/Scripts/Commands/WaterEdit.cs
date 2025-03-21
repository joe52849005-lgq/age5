﻿using System;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Core.Managers.World;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using AAEmu.Commons.IO;
using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers.Id;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Teleport;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.World;
using AAEmu.Game.Models.StaticValues;
using AAEmu.Game.Utils.Scripts;

namespace AAEmu.Game.Scripts.Commands;

public class WaterEdit : ICommand
{
    public string[] CommandNames { get; set; } = new[] { "wateredit", "water_edit", "wedit" };

    public static WaterBodyArea SelectedWater { get; set; }
    public static World SelectedWorld { get; set; }
    public static int NextPoint { get; set; }
    public List<(WaterBodyArea, float)> NearbyList = new();
    public List<BaseUnit> Markers = new();

    public void OnLoad()
    {
        CommandManager.Instance.Register(CommandNames, this);
    }

    public string GetCommandLineHelp()
    {
        return "<subcommand>";
    }

    public string GetCommandHelpText()
    {
        return "Live editing, saving and loading of water bodies";
    }

    public void CreateNearbyList(Character character, World world)
    {
        NearbyList.Clear();
        lock (world.Water._lock)
        {
            foreach (var area in world.Water.Areas)
            {
                var offsetVec = area.Points[0] - character.Transform.World.Position;
                var dist = offsetVec.Length();

                if (NearbyList.Count <= 0)
                {
                    NearbyList.Add((area, dist));
                }
                else
                {
                    var insertAt = NearbyList.Count;
                    for (var i = 0; i < NearbyList.Count; i++)
                    {
                        if (NearbyList[i].Item2 > dist)
                        {
                            insertAt = i;
                            break;
                        }
                    }

                    NearbyList.Insert(insertAt, (area, dist));
                }
            }
        }
    }

    public void ShowSelectedArea(Character character)
    {
        var bottomDoodadId = 4763u; // Crescent Throne Flag 
        var centerSurfaceDoodadId = 5014u; // Combat Flag
        var middleDoodadId = 5622u; // Stone Post
        var topNpcId = 13013u; // Forward Scarecrow
        foreach (var marker in Markers)
        {
            ObjectIdManager.Instance.ReleaseId(marker.ObjId);
            marker.Delete();
        }

        Markers.Clear();

        if (SelectedWorld == null || SelectedWater == null)
        {
            return;
        }

        lock (SelectedWorld.Water._lock)
        {
            SelectedWater.UpdateBounds();

            var centerDoodad = DoodadManager.Instance.Create(0, centerSurfaceDoodadId);
            centerDoodad.Transform.Local.SetPosition(SelectedWater.GetCenter(true));
            centerDoodad.Show();
            Markers.Add(centerDoodad);
            // character.SendDebugMessage("--" + centerDoodad.Transform.ToFullString());

            for (var p = 0; p < SelectedWater.Points.Count - 1; p++)
            {
                var point = SelectedWater.Points[p];
                var bottomDoodad = DoodadManager.Instance.Create(0, bottomDoodadId);
                bottomDoodad.Transform.Local.SetPosition(point);
                bottomDoodad.Show();
                Markers.Add(bottomDoodad);

                // var surfaceDoodad = DoodadManager.Instance.Create(0, topDoodadId);
                // surfaceDoodad.Transform.Local.SetPosition(point);
                // surfaceDoodad.Transform.Local.SetHeight(point.Z + SelectedWater.Height);
                // surfaceDoodad.Show();
                // Markers.Add(surfaceDoodad);

                var surfaceUnit = NpcManager.Instance.Create(0, topNpcId);
                surfaceUnit.Transform.Local.SetPosition(point);
                surfaceUnit.Transform.Local.SetHeight(point.Z + SelectedWater.Height);
                if (p != 0)
                {
                    surfaceUnit.Name = "#" + p;
                }
                else
                {
                    surfaceUnit.Name = "#" + p + " <-> #" + (SelectedWater.Points.Count - 1);
                }

                surfaceUnit.Faction = FactionManager.Instance.GetFaction(FactionsEnum.Friendly);
                surfaceUnit.Show();
                Markers.Add(surfaceUnit);

                var dividers = MathF.Ceiling(SelectedWater.Height / 5f);
                dividers = MathF.Max(dividers, 2f);

                for (var i = 1; i < dividers; i++)
                {
                    var h = SelectedWater.Height / dividers * i;
                    var middleDoodad = DoodadManager.Instance.Create(0, middleDoodadId);
                    middleDoodad.Transform.Local.SetPosition(point);
                    middleDoodad.Transform.Local.SetHeight(point.Z + h);
                    middleDoodad.Show();
                    Markers.Add(middleDoodad);
                }
            }
        }
    }

    public void Execute(Character character, string[] args, IMessageOutput messageOutput)
    {
        var world = WorldManager.Instance.GetWorld(character.Transform.WorldId);

        if (args.Length <= 0)
        {
            CommandManager.SendNormalText(this, messageOutput, $"Currently selected:");
            CommandManager.SendNormalText(this, messageOutput, $"|cFFFFFFFF{SelectedWater?.Name ?? "<no area>"}|r in |cFFFFFFFF{SelectedWorld?.Name ?? "<no world>"}|r with height {SelectedWater?.Height ?? 0f}");
            CommandManager.SendNormalText(this, messageOutput, $"Available commands:");
            CommandManager.SendNormalText(this, messageOutput, $"|cFF00FF00list|r: lists all water bodies in the current world");
            CommandManager.SendNormalText(this, messageOutput, $"|cFF00FF00nearby|r: lists all nearby water bodies");
            CommandManager.SendNormalText(this, messageOutput, $"|cFF00FF00select <name||id>|r: select a water body");
            CommandManager.SendNormalText(this, messageOutput, $"|cFF00FF00next|r: select the next water body in the world list");
            CommandManager.SendNormalText(this, messageOutput, $"|cFF00FF00createwater <name>|r: creates a new cube of water at your location using name");
            CommandManager.SendNormalText(this, messageOutput, $"|cFF00FF00save|r: saves water_bodies.json to disk");
            CommandManager.SendNormalText(this, messageOutput, $"|cFF00FF00load|r: load data from water_bodies.json on disk");
            if (SelectedWater != null)
            {
                CommandManager.SendNormalText(this, messageOutput, $"|cFF00FF00clear|r: unselect everything");
                CommandManager.SendNormalText(this, messageOutput, $"|cFF00FF00goto|r: teleports to selected water body");
                CommandManager.SendNormalText(this, messageOutput, $"|cFF00FF00setheight <value>|r: set a new height for selected water body");
                CommandManager.SendNormalText(this, messageOutput, $"|cFF00FF00setbottom <value>|r: set a new Z position for all points in selected water body");
                CommandManager.SendNormalText(this, messageOutput, $"|cFF00FF00listpoints|r: Shows world position of all points in the selected water body");
                CommandManager.SendNormalText(this, messageOutput, $"|cFF00FF00movepoint <index>|r: changes the point at index's X and Y position of your current location");
                CommandManager.SendNormalText(this, messageOutput, $"|cFF00FF00insertpoint <index>|r: inserts a new point before index using X and Y position of your current location");
                CommandManager.SendNormalText(this, messageOutput, $"|cFF00FF00removepoint <index>|r: removes the point at index");
                CommandManager.SendNormalText(this, messageOutput, $"|cFF00FF00removewater <count>|r: entirely removes a body of water, must provide the current amount of points in the body");
            }

            return;
        }

        if (world == null)
        {
            CommandManager.SendErrorText(this, messageOutput, $"You are somehow not in a valid world!");
            return;
        }

        var subCommand = args[0].ToLower();
        if (subCommand is "list" or "l")
        {
            CommandManager.SendNormalText(this, messageOutput, $"World {world.Name} has {world.Water.Areas.Count} water bodies defined:");
            foreach (var area in world.Water.Areas)
            {
                CommandManager.SendNormalText(this, messageOutput, $"|cFFFFFFFF{area.Name}|r ({area.Id}) => {area.Points.Count} points");
            }
        }
        else if (subCommand == "nearby")
        {
            CreateNearbyList(character, world);
            var c = 0;
            for (var i = 0; i < NearbyList.Count && i < 5; i++)
            {
                var area = NearbyList[i].Item1;
                var distance = NearbyList[i].Item2;
                if (distance > 1000f)
                {
                    break;
                }

                c++;
                CommandManager.SendNormalText(this, messageOutput, $"|cFFFFFFFF{area.Name}|r ({area.Id}) - {distance:F1}m");
            }

            if (c <= 0)
            {
                CommandManager.SendErrorText(this, messageOutput, $"Nothing nearby");
            }
        }
        else if (subCommand is "select" or "s")
        {
            if (args.Length <= 1)
            {
                CommandManager.SendErrorText(this, messageOutput, $"No area name or id provided!");
                return;
            }

            var selectName = args[1].ToLower();
            SelectedWater = null;
            SelectedWorld = world;
            // search exact name or id
            lock (world.Water._lock)
            {
                foreach (var area in world.Water.Areas)
                {
                    if (area.Name.ToLower() == selectName || area.Id.ToString() == selectName)
                    {
                        SelectedWater = area;
                        NextPoint = 0;
                        break;
                    }
                }
            }

            // If nothing found, do partial name search
            if (SelectedWater == null)
            {
                lock (world.Water._lock)
                {
                    foreach (var area in world.Water.Areas)
                    {
                        if (area.Name.ToLower().Contains(selectName))
                        {
                            SelectedWater = area;
                            NextPoint = 0;
                            break;
                        }
                    }
                }
            }

            if (SelectedWater != null)
            {
                CommandManager.SendNormalText(this, messageOutput, $"Selected |cFFFFFFFF{SelectedWater.Name}|r ({SelectedWater.Id}), height: |cFF00FF00{SelectedWater.Height}|r");
            }
            else
            {
                CommandManager.SendErrorText(this, messageOutput, $"Nothing selected");
            }

            ShowSelectedArea(character);
        }
        else if (subCommand is "setheight" or "seth")
        {
            if (SelectedWater == null)
            {
                CommandManager.SendErrorText(this, messageOutput, $"You need to select a water body first!");
                return;
            }

            if (args.Length <= 1)
            {
                CommandManager.SendErrorText(this, messageOutput, $"No height provided!");
                return;
            }

            if (!float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var newHeight))
            {
                CommandManager.SendErrorText(this, messageOutput, $"Error parsing height float!");
                return;
            }

            SelectedWater.Height = newHeight;
            ShowSelectedArea(character);
            CommandManager.SendNormalText(this, messageOutput, $"Height for |cFFFFFFFF{SelectedWater.Name}|r set to |cFF00FF00{newHeight}!|r");
        }
        else if (subCommand is "setbottom" or "setb")
        {
            if (SelectedWater == null)
            {
                CommandManager.SendErrorText(this, messageOutput, $"You need to select a water body first!");
                return;
            }

            if (args.Length <= 1)
            {
                CommandManager.SendErrorText(this, messageOutput, $"No Z position provided!");
                return;
            }

            if (!float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var newBottom))
            {
                CommandManager.SendErrorText(this, messageOutput, $"Error parsing height float!");
                return;
            }

            for (var i = 0; i < SelectedWater.Points.Count; i++)
            {
                SelectedWater.Points[i] = new Vector3(SelectedWater.Points[i].X, SelectedWater.Points[i].Y, newBottom);
            }

            ShowSelectedArea(character);
            CommandManager.SendNormalText(this, messageOutput, $"Z position for all points in |cFFFFFFFF{SelectedWater.Name}|r have been set to |cFF00FF00{newBottom}!|r");
        }
        else if (subCommand is "listpoints" or "listp")
        {
            if (SelectedWater == null)
            {
                CommandManager.SendErrorText(this, messageOutput, $"You need to select a water body first!");
                return;
            }

            for (var i = 0; i < SelectedWater.Points.Count - 1; i++)
            {
                CommandManager.SendNormalText(this, messageOutput, $"#{i} : {SelectedWater.Points[i]}");
            }

            ShowSelectedArea(character);
        }
        else if (subCommand is "goto")
        {
            if (SelectedWater == null)
            {
                CommandManager.SendErrorText(this, messageOutput, $"You need to select a water body first!");
                return;
            }

            var pos = SelectedWater.GetCenter(true);
            character.ForceDismount();
            character.DisabledSetPosition = true;
            character.SendPacket(new SCTeleportUnitPacket(TeleportReason.Portal, ErrorMessageType.NoErrorMessage, pos.X + 1f, pos.Y + 1f, pos.Z + 3f, 0));
        }
        else if (subCommand is "clear" or "c")
        {
            if (SelectedWater == null)
            {
                character.SendDebugMessage($"[WaterEdit] You had nothing selected.");
            }

            SelectedWater = null;
            ShowSelectedArea(character);
        }
        else if (subCommand is "next")
        {
            var lastId = SelectedWater?.Id ?? 0;
            var doSelectNext = lastId <= 0;
            foreach (var area in world.Water.Areas)
            {
                if (doSelectNext)
                {
                    SelectedWater = area;
                    SelectedWorld = world;
                    NextPoint = 0;
                    break;
                }

                if (area.Id == lastId)
                {
                    doSelectNext = true;
                }
            }

            if (SelectedWater != null)
            {
                CommandManager.SendNormalText(this, messageOutput, $"Selected |cFFFFFFFF{SelectedWater.Name}|r ({SelectedWater.Id}), height: |cFF00FF00{SelectedWater.Height}|r");
            }
            else
            {
                CommandManager.SendErrorText(this, messageOutput, $"Nothing selected");
            }

            ShowSelectedArea(character);
        }
        else if (subCommand is "movepoint" or "mp")
        {
            if (SelectedWater == null)
            {
                CommandManager.SendErrorText(this, messageOutput, $"You need to select a water body first!");
                return;
            }

            var pointIndex = NextPoint;
            if (args.Length > 1)
            {
                if (!int.TryParse(args[1], out pointIndex))
                {
                    CommandManager.SendErrorText(this, messageOutput, $"Error parsing point index!");
                    return;
                }
            }

            if (pointIndex >= SelectedWater.Points.Count - 1 || pointIndex < 0)
            {
                CommandManager.SendErrorText(this, messageOutput, $"{pointIndex} is not a valid point index (0~{SelectedWater.Points.Count - 2})!");
                return;
            }

            var newPos = new Vector3(character.Transform.World.Position.X, character.Transform.World.Position.Y,
                SelectedWater.Points[pointIndex].Z);
            lock (SelectedWorld.Water._lock)
            {
                SelectedWater.Points[pointIndex] = newPos;
                if (pointIndex == 0)
                {
                    SelectedWater.Points[^1] = newPos;
                }
            }

            NextPoint = pointIndex + 1;

            ShowSelectedArea(character);
            CommandManager.SendNormalText(this, messageOutput, $"|cFFFFFFFF{SelectedWater.Name} #{pointIndex}|r moved to set to |cFF00FF00{newPos}|r");
        }
        else if (subCommand is "insertpoint" or "ip")
        {
            if (SelectedWater == null)
            {
                CommandManager.SendErrorText(this, messageOutput, $"You need to select a water body first!");
                return;
            }

            if (args.Length <= 1)
            {
                CommandManager.SendErrorText(this, messageOutput, $"No point index provided!");
                return;
            }

            if (!int.TryParse(args[1], out var pointIndex))
            {
                CommandManager.SendErrorText(this, messageOutput, $"Error parsing point index!");
                return;
            }

            if (pointIndex > SelectedWater.Points.Count - 1 || pointIndex < 0)
            {
                CommandManager.SendErrorText(this, messageOutput, $"{pointIndex} is not a valid point index (0~{SelectedWater.Points.Count - 1})!");
                return;
            }

            var newPos = new Vector3(character.Transform.World.Position.X, character.Transform.World.Position.Y,
                SelectedWater.Points[pointIndex].Z);
            lock (SelectedWorld.Water._lock)
            {
                SelectedWater.Points.Insert(pointIndex, newPos);
                if (pointIndex == 0)
                {
                    SelectedWater.Points[^1] = newPos;
                }
            }

            ShowSelectedArea(character);
            CommandManager.SendNormalText(this, messageOutput, $"Added new point before |cFFFFFFFF#{pointIndex}|r at |cFF00FF00{newPos}|r");
        }
        else if (subCommand is "removepoint" or "rp")
        {
            if (SelectedWater == null)
            {
                CommandManager.SendErrorText(this, messageOutput, $"You need to select a water body first!");
                return;
            }

            if (args.Length <= 1)
            {
                CommandManager.SendErrorText(this, messageOutput, $"No point index provided!");
                return;
            }

            if (SelectedWater.Points.Count <= 4)
            {
                CommandManager.SendErrorText(this, messageOutput, $"You must keep at least 3 unique points !");
                return;
            }

            if (!int.TryParse(args[1], out var pointIndex))
            {
                CommandManager.SendErrorText(this, messageOutput, $"Error parsing point index!");
                return;
            }

            if (pointIndex >= SelectedWater.Points.Count - 1 || pointIndex <= 0)
            {
                CommandManager.SendErrorText(this, messageOutput, $"{pointIndex} is not a valid point index (1~{SelectedWater.Points.Count - 2})!");
                return;
            }

            lock (SelectedWorld.Water._lock)
            {
                SelectedWater.Points.RemoveAt(pointIndex);
            }

            ShowSelectedArea(character);
            CommandManager.SendNormalText(this, messageOutput, $"Removed point |cFFFFFFFF#{pointIndex}|r");
        }
        else if (subCommand is "removewater")
        {
            if (SelectedWater == null)
            {
                CommandManager.SendErrorText(this, messageOutput, $"You need to select a water body first!");
                return;
            }

            if (args.Length <= 1)
            {
                CommandManager.SendErrorText(this, messageOutput, $"No point count provided!");
                return;
            }

            if (!int.TryParse(args[1], out var pointCount))
            {
                CommandManager.SendErrorText(this, messageOutput, $"Error parsing point count!");
                return;
            }

            if (pointCount != SelectedWater.Points.Count)
            {
                CommandManager.SendErrorText(this, messageOutput, $"Security check failed !");
                return;
            }

            lock (world.Water._lock)
            {
                world.Water.Areas.Remove(SelectedWater);
            }

            SelectedWater = null;
            ShowSelectedArea(character);
            CommandManager.SendNormalText(this, messageOutput, $"Removed water body !!");
        }
        else if (subCommand is "createwater")
        {
            if (args.Length <= 1)
            {
                CommandManager.SendErrorText(this, messageOutput, $"No name provided!");
                return;
            }

            var newName = args[1];
            var newBody = new WaterBodyArea(newName);
            newBody.Id = (uint)Rand.Next(8000000, 9000000);
            var centerPos = character.Transform.World.Position with { Z = character.Transform.World.Position.Z - 5f };
            newBody.Points.Add(new Vector3(centerPos.X - 15f, centerPos.Y - 15f, centerPos.Z));
            newBody.Points.Add(new Vector3(centerPos.X - 15f, centerPos.Y + 15f, centerPos.Z));
            newBody.Points.Add(new Vector3(centerPos.X + 15f, centerPos.Y + 15f, centerPos.Z));
            newBody.Points.Add(new Vector3(centerPos.X + 15f, centerPos.Y - 15f, centerPos.Z));
            newBody.Points.Add(new Vector3(centerPos.X - 15f, centerPos.Y - 15f, centerPos.Z));
            newBody.Height = 10f;
            newBody.UpdateBounds();

            lock (world.Water._lock)
            {
                world.Water.Areas.Add(newBody);
            }

            SelectedWater = newBody;
            SelectedWorld = world;
            NextPoint = 0;

            ShowSelectedArea(character);

            CommandManager.SendNormalText(this, messageOutput,
                $"Create new water cube {SelectedWater.Name} - {SelectedWater.Id}!!");
        }
        else if (subCommand == "save")
        {
            var saveFileName = Path.Combine(FileManager.AppPath, "Data", "Worlds", world.Name, "water_bodies.json");
            if (WaterBodies.Save(saveFileName, world.Water))
            {
                CommandManager.SendNormalText(this, messageOutput, $"|cFFFFFFFF{saveFileName}|r has been saved.");
            }
            else
            {
                CommandManager.SendErrorText(this, messageOutput, $"Error saving {saveFileName} !");
            }
        }
        else if (subCommand == "load")
        {
            var loadFileName = Path.Combine(FileManager.AppPath, "Data", "Worlds", world.Name, "water_bodies.json");
            if (!WaterBodies.Load(loadFileName, out var newWater))
            {
                CommandManager.SendErrorText(this, messageOutput, $"Error loading {loadFileName} !");
            }
            else
            {
                world.Water = newWater;
                CommandManager.SendNormalText(this, messageOutput, $"|cFFFFFFFF{loadFileName}|r has been loaded.");
            }
        }
        else
        {
            CommandManager.SendErrorText(this, messageOutput, $"Unknown subcommand: {subCommand}");
        }
    }
}
