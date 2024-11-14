using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using AAEmu.Commons.Exceptions;
using AAEmu.Commons.IO;
using AAEmu.Commons.Utils;
using AAEmu.Commons.Utils.Creatures;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.World;
using AAEmu.Game.Models.Json;
using AAEmu.Game.Utils;
using AAEmu.Game.Utils.Converters;
using AAEmu.Game.Utils.Scripts;
using AAEmu.Game.Utils.Scripts.SubCommands;

using Newtonsoft.Json;

namespace AAEmu.Game.Scripts.SubCommands.Doodads;

public class DoodadSaveSubCommand : SubCommandBase
{
    private static Dictionary<uint, Creature> _creatures;

    public DoodadSaveSubCommand()
    {
        Title = "[Doodad Save]";
        Description = "Save current state of a doodad to the doodads world file.";
        CallPrefix = $"{CommandManager.CommandPrefix}doodad save";
        AddParameter(new StringSubCommandParameter("target", "target", true, "all", "id"));
        AddParameter(new NumericSubCommandParameter<uint>("ObjId", "Object Id", false));
    }

    public override void Execute(ICharacter character, string triggerArgument, IDictionary<string, ParameterValue> parameters, IMessageOutput messageOutput)
    {
        // Запускаем метод в отдельной задаче (нити)
        Task.Run(() =>
        {
            try
            {
                if (parameters.TryGetValue("ObjId", out var doodadObjId))
                {
                    SaveById(character, doodadObjId, messageOutput);
                }
                else
                {
                    SaveAll(character, messageOutput);
                }
            }
            catch (Exception ex)
            {
                // Обработка исключения, например, запись в лог
                Logger.Error($"Ошибка при выполнении метода: {ex.Message}");
            }
        });
    }

    private void SaveAll(ICharacter character, IMessageOutput messageOutput)
    {
        _creatures = Creature.GetAllDoodads();
        var currentWorld = WorldManager.Instance.GetWorld(((Character)character).Transform.WorldId);
        //var allDoodads = WorldManager.Instance.GetAllDoodads();
        var doodadsInWorld = WorldManager.Instance.GetAllDoodadsFromWorld(currentWorld.Id);
        //var doodadSpawnersFromFile = LoadDoodadsFromFileByWorld(currentWorld);
        //var doodadSpawnersToFile = new ConcurrentDictionary<int, JsonDoodadSpawns>(doodadSpawnersFromFile.ToDictionary(d => d.Id, d => d) as IEnumerable<KeyValuePair<int, JsonDoodadSpawns>>);
        var doodadSpawnersToFile = new ConcurrentDictionary<int, JsonDoodadSpawns>();
        var index = 0u;

        Parallel.ForEach(doodadsInWorld.Where(d => d.Spawner is not null), doodad =>
        {
            switch (doodad.Spawner.Id)
            {
                // spawned into the game manually
                case 0:
                default:
                    {
                        var pos = doodad.Transform.World;
                        var newDoodadSpawn = new JsonDoodadSpawns
                        {
                            Id = index++,
                            UnitId = doodad.TemplateId,
                            Title = GetSpawnName(doodad.TemplateId), // обновим Title
                            Position = new JsonPosition
                            {
                                X = pos.Position.X,
                                Y = pos.Position.Y,
                                Z = pos.Position.Z,
                                Roll = pos.Rotation.X.RadToDeg(),
                                Pitch = pos.Rotation.Y.RadToDeg(),
                                Yaw = pos.Rotation.Z.RadToDeg()
                            },
                            Scale = doodad.Scale,
                            FuncGroupId = doodad.FuncGroupId
                        };

                        doodadSpawnersToFile.TryAdd((int)newDoodadSpawn.Id, newDoodadSpawn);
                        break;
                    }

                // removed from the game manually
                case 0xffffffff: //(uint)-1
                    {
                        break;
                    }
            }
        });

        var jsonPathOut = Path.Combine(FileManager.AppPath, "Data", "Worlds", currentWorld.Name, "doodad_spawns_new.json");
        var json = JsonConvert.SerializeObject(doodadSpawnersToFile.Values.ToArray(), Formatting.Indented, new JsonModelsConverter());
        File.WriteAllText(jsonPathOut, json);
        SendMessage(messageOutput, "All doodads have been saved!");
    }

    private void SaveById(ICharacter character, uint doodadObjId, IMessageOutput messageOutput)
    {
        _creatures = Creature.GetAllDoodads();
        var doodad = WorldManager.Instance.GetDoodad(doodadObjId);
        if (doodad is null)
        {
            SendColorMessage(messageOutput, Color.Red, $"Doodad with objId {doodadObjId} does not exist");
            return;
        }

        var world = WorldManager.Instance.GetWorld(doodad.Transform.WorldId);
        if (world is null)
        {
            SendColorMessage(messageOutput, Color.Red, $"Could not find the worldId {doodad.Transform.WorldId}");
            return;
        }

        var spawn = new JsonDoodadSpawns
        {
            Id = doodad.Id,
            UnitId = doodad.TemplateId,
            Title = GetSpawnName(doodad.TemplateId), // обновим Title
            Position = new JsonPosition
            {
                X = doodad.Transform.Local.Position.X,
                Y = doodad.Transform.Local.Position.Y,
                Z = doodad.Transform.Local.Position.Z,
                Roll = doodad.Transform.Local.Rotation.X.RadToDeg(),
                Pitch = doodad.Transform.Local.Rotation.Y.RadToDeg(),
                Yaw = doodad.Transform.Local.Rotation.Z.RadToDeg()
            },
            Scale = doodad.Scale,
            FuncGroupId = doodad.FuncGroupId
        };

        Dictionary<uint, JsonDoodadSpawns> spawnersFromFile = new();
        foreach (var spawnerFromFile in LoadDoodadsFromFileByWorld(world))
        {
            spawnersFromFile.TryAdd(spawnerFromFile.Id, spawnerFromFile);
        }

        spawnersFromFile[spawn.Id] = spawn;

        var jsonPathOut = Path.Combine(FileManager.AppPath, "Data", "Worlds", world.Name, "doodad_spawns_new.json");
        var json = JsonConvert.SerializeObject(spawnersFromFile.Values.ToArray(), Formatting.Indented, new JsonModelsConverter());
        File.WriteAllText(jsonPathOut, json);
        //SendMessage(messageOutput, $"Doodad ObjId: {doodad.ObjId} has been saved!");
        SendMessage(messageOutput, $"All doodads have been saved with added doodad ObjId:{doodad.ObjId}, TemplateId:{doodad.TemplateId}");
    }

    private List<JsonDoodadSpawns> LoadDoodadsFromFileByWorld(World world)
    {
        var jsonPathIn = Path.Combine(FileManager.AppPath, "Data", "Worlds", world.Name, "doodad_spawns.json");
        if (!File.Exists(jsonPathIn))
        {
            throw new GameException($"File {jsonPathIn} doesn't exists.");
        }

        var contents = FileManager.GetFileContents(jsonPathIn);
        Logger.Info($"Loading spawns from file {jsonPathIn} ...");

        return string.IsNullOrWhiteSpace(contents) ? [] : JsonHelper.DeserializeObject<List<JsonDoodadSpawns>>(contents);
    }

    private static string GetSpawnName(uint id)
    {
        return _creatures.TryGetValue(id, out var creature) ? creature.Title : string.Empty;
    }
}
