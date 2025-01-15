using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.Units.Route;
using AAEmu.Game.Models.Game.World;

using NLog;

namespace AAEmu.Game.Models.Game.NPChar;

public class NpcSpawnerNpc : Spawner<Npc>
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public uint NpcSpawnerTemplateId { get; set; } // spawner template id
    public uint MemberId { get; set; } // npc template id
    public string MemberType { get; set; } // 'Npc'
    public float Weight { get; set; }

    public NpcSpawnerNpc()
    {
    }

    /// <summary>
    /// Creates a new instance of NpcSpawnerNpcs with a Spawner template id (npc_spanwers)
    /// </summary>
    /// <param name="spawnerTemplateId"></param>
    public NpcSpawnerNpc(uint spawnerTemplateId)
    {
        NpcSpawnerTemplateId = spawnerTemplateId;
    }

    public NpcSpawnerNpc(uint spawnerTemplateId, uint memberId)
    {
        NpcSpawnerTemplateId = spawnerTemplateId;
        MemberId = memberId;
        MemberType = "Npc";
    }

    public async Task<List<Npc>> SpawnAsync(NpcSpawner npcSpawner)
    {
        switch (MemberType)
        {
            case "Npc":
                return await SpawnNpcAsync(npcSpawner);
            case "NpcGroup":
                return await SpawnNpcGroupAsync(npcSpawner);
            default:
                throw new InvalidOperationException($"Tried spawning an unsupported line from NpcSpawnerNpc - Id: {Id}");
        }
    }

    private async Task<List<Npc>> SpawnNpcAsync(NpcSpawner npcSpawner)
    {
        var npcs = new List<Npc>();
        var npc = await Task.Run(() => NpcManager.Instance.Create(0, MemberId));
        if (npc == null)
        {
            Logger.Warn($"Npc {MemberId}, from spawner Id {npcSpawner.Id} not exist at db. Spawner Position: {npcSpawner.Position}");
            return null;
        }

        npc.RegisterNpcEvents();

        Logger.Trace($"Spawn npc templateId {MemberId} objId {npc.ObjId} from spawnerId {NpcSpawnerTemplateId} at Position: {npcSpawner.Position}");

        if (!npc.CanFly)
        {
            var newZ = await WorldManager.Instance.GetHeightAsync(npcSpawner.Position.ZoneId, npcSpawner.Position.X, npcSpawner.Position.Y);
            if (Math.Abs(npcSpawner.Position.Z - newZ) < 1f)
            {
                npcSpawner.Position.Z = newZ;
            }
        }

        npc.Transform.ApplyWorldSpawnPosition(npcSpawner.Position);
        if (npc.Transform == null)
        {
            Logger.Error($"Can't spawn npc {MemberId} from spawnerId {NpcSpawnerTemplateId}. Transform is null.");
            return null;
        }

        npc.Transform.InstanceId = npc.Transform.WorldId;
        npc.InstanceId = npc.Transform.WorldId;

        if (npc.Ai != null)
        {
            npc.Ai.HomePosition = npc.Transform.World.Position;
            npc.Ai.IdlePosition = npc.Ai.HomePosition;
            npc.Ai.GoToSpawn();
        }

        npc.Spawner = npcSpawner;
        npc.Spawner.RespawnTime = (int)Rand.Next(npc.Spawner.Template.SpawnDelayMin, npc.Spawner.Template.SpawnDelayMax);
        npc.Spawn();

        var aroundNpcs = await WorldManager.GetAroundAsync<Npc>(npc, 1);
        var count = 0u;
        foreach (var n in aroundNpcs.Where(n => n.TemplateId == MemberId))
        {
            count++;
        }

        var world = WorldManager.Instance.GetWorld(npc.Transform.WorldId);
        world.Events.OnUnitSpawn(world, new OnUnitSpawnArgs { Npc = npc });
        npc.Simulation = new Simulation(npc);

        if (npc.Ai != null && !string.IsNullOrWhiteSpace(npcSpawner.FollowPath))
        {
            if (!await npc.Ai.LoadAiPathPointsAsync(npcSpawner.FollowPath, false))
                Logger.Warn($"Failed to load {npcSpawner.FollowPath} for NPC {npc.TemplateId} ({npc.ObjId})");
        }

        npcs.Add(npc);
        return npcs;
    }

    private async Task<List<Npc>> SpawnNpcGroupAsync(NpcSpawner npcSpawner)
    {
        return await SpawnNpcAsync(npcSpawner);
    }
}
