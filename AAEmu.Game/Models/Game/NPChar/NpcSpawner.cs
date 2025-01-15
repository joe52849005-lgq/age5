using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.Id;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Items.Containers;
using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.World;
using AAEmu.Game.Models.Tasks.World;

using Newtonsoft.Json;

using NLog;

namespace AAEmu.Game.Models.Game.NPChar;

public class NpcSpawner : Spawner<Npc>
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly ConcurrentBag<Npc> _spawned = [];
    private Npc _lastSpawn;
    private int _scheduledCount;
    private int _spawnCount;
    private bool _isSpawnScheduled;
    private bool isNotFoundInScheduler;

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
    [DefaultValue(1f)]
    public uint Count { get; set; } = 1;

    public List<uint> NpcSpawnerIds { get; set; } = [];
    public NpcSpawnerTemplate Template { get; set; }

    public NpcSpawner()
    {
        _isSpawnScheduled = false;
    }

    /// <summary>
    /// Spawns all NPCs associated with this spawner.
    /// </summary>
    /// <param name="beginning">Indicates if this is the initial spawn at the start of the game.</param>
    /// <returns>A list of spawned NPCs, or null if spawning was scheduled.</returns>
    public List<Npc> SpawnAll(bool beginning = false)
    {
        if (ScheduleSpawn(true))
        {
            return null;
        }

        DoSpawn(true, beginning);
        return _spawned.ToList();
    }

    /// <summary>
    /// Spawns a single NPC with the specified object ID.
    /// </summary>
    /// <param name="objId">The object ID of the NPC to spawn.</param>
    /// <returns>The spawned NPC, or null if spawning was scheduled.</returns>
    public override Npc Spawn(uint objId)
    {
        if (ScheduleSpawn())
        {
            return null;
        }

        DoSpawn();
        return _lastSpawn;
    }

    /// <summary>
    /// Despawns the specified NPC.
    /// </summary>
    /// <param name="npc">The NPC to despawn.</param>
    public override void Despawn(Npc npc)
    {
        if (npc == null) return;

        npc.UnregisterNpcEvents();
        npc.Delete();

        if (npc.Respawn == DateTime.MinValue)
        {
            _spawned.TryTake(out _);
            ObjectIdManager.Instance.ReleaseId(npc.ObjId);
            Interlocked.Decrement(ref _spawnCount);
        }

        if (_lastSpawn == null || _lastSpawn.ObjId == npc.ObjId)
        {
            _lastSpawn = _spawned.LastOrDefault();
        }
    }

    /// <summary>
    /// Clears the last spawn count.
    /// </summary>
    public void ClearLastSpawnCount()
    {
        Interlocked.Exchange(ref _spawnCount, 0);
    }

    /// <summary>
    /// Decreases the spawn count and handles respawn logic for the specified NPC.
    /// </summary>
    /// <param name="npc">The NPC to decrease the count for.</param>
    public void DecreaseCount(Npc npc)
    {
        if (npc == null) return;

        Interlocked.Decrement(ref _spawnCount);
        _spawned.TryTake(out _);

        if (RespawnTime > 0 && _spawnCount + _scheduledCount < Count)
        {
            npc.Respawn = DateTime.UtcNow.AddSeconds(RespawnTime);
            SpawnManager.Instance.AddRespawn(npc);
            Interlocked.Increment(ref _scheduledCount);
        }

        npc.Despawn = DateTime.UtcNow.AddSeconds(DespawnTime);
        if (npc.LootingContainer?.Items.Count > 0)
        {
            npc.Despawn += TimeSpan.FromSeconds(LootingContainer.LootDespawnExtensionTime);
        }
        SpawnManager.Instance.AddDespawn(npc);
    }

    /// <summary>
    /// Despawns the specified NPC and schedules a respawn if necessary.
    /// </summary>
    /// <param name="npc">The NPC to despawn.</param>
    public void DespawnWithRespawn(Npc npc)
    {
        if (npc == null) return;

        npc.Delete();
        Interlocked.Decrement(ref _spawnCount);
        _spawned.TryTake(out _);

        if (RespawnTime > 0 && _spawnCount + _scheduledCount < Count)
        {
            npc.Respawn = DateTime.UtcNow.AddSeconds(RespawnTime);
            SpawnManager.Instance.AddRespawn(npc);
            Interlocked.Increment(ref _scheduledCount);
        }
    }

    /// <summary>
    /// Despawns the specified NPC, optionally despawn all NPCs.
    /// </summary>
    /// <param name="npc">The NPC to despawn.</param>
    /// <param name="all">Indicates whether to despawn all NPCs.</param>
    public void DoDespawn(Npc npc, bool all = false)
    {
        if (npc == null) return;

        if (npc.IsInBattle)
        {
            return;
        }

        if (all)
        {
            for (var i = 0; i < _spawnCount; i++)
            {
                Despawn(_lastSpawn);
            }
        }
        else
        {
            Despawn(npc);
        }
    }

    /// <summary>
    /// Spawns NPCs, optionally spawning all NPCs.
    /// </summary>
    /// <param name="all">Indicates whether to spawn all NPCs.</param>
    /// <param name="beginning">Indicates if this is the initial spawn at the start of the game.</param>
    public async Task DoSpawn(bool all = false, bool beginning = false)
    {
        var spawnerIds = NpcGameData.Instance.GetSpawnerIds(UnitId);
        var npcSpawnerIds = spawnerIds.Count > NpcSpawnerIds.Count ? spawnerIds : NpcSpawnerIds;

        var npcs = new List<Npc>();
        var delnpcs = new List<Npc>();

        foreach (var spawnerId in npcSpawnerIds)
        {
            var template = NpcGameData.Instance.GetNpcSpawnerTemplate(spawnerId);
            if (template == null)
            {
                var nsnTask = Template.Npcs.FirstOrDefault(nsn => nsn.MemberId == UnitId);
                if (nsnTask != null)
                {
                    npcs = await nsnTask.SpawnAsync(this, all ? Template.MaxPopulation : 1);
                    if (npcs == null) return;
                }
            }
            else
            {
                if (template.NpcSpawnerCategoryId == NpcSpawnerCategory.Normal && npcSpawnerIds.Count > 1)
                {
                    continue;
                }

                var suspendSpawnCount = template.SuspendSpawnCount > 0 ? template.SuspendSpawnCount : 1;
                var maxPopulation = template.MaxPopulation;
                var quantity = suspendSpawnCount;

                if (quantity > maxPopulation) quantity = maxPopulation;
                if (!all) quantity = 1;

                if (_spawnCount > maxPopulation)
                {
                    Logger.Trace($"Let's not spawn Npc templateId {UnitId} from spawnerId {Template.Id} since exceeded MaxPopulation {maxPopulation}");
                    return;
                }

                var templateNsnTask = template.Npcs.FirstOrDefault(nsn => nsn.MemberId == UnitId);
                if (templateNsnTask != null)
                {
                    npcs = await templateNsnTask.SpawnAsync(this, quantity, maxPopulation);
                }
                if (npcs == null) continue;
            }

            if (npcs.Count == 0)
            {
                Logger.Error($"Can't spawn npc {UnitId} from spawnerId {Template.Id}");
                continue;
            }

            delnpcs.AddRange(npcs);
            foreach (var npc in npcs)
            {
                _spawned.Add(npc);
            }
            if (_scheduledCount > 0)
            {
                Interlocked.Add(ref _scheduledCount, -npcs.Count);
            }

            Interlocked.Add(ref _spawnCount, npcs.Count);

            if (_spawnCount < 0)
            {
                Interlocked.Exchange(ref _spawnCount, 0);
            }

            _lastSpawn = _spawned.LastOrDefault();
        }

        if (_isSpawnScheduled)
        {
            ScheduleDespawn(_lastSpawn, all);
        }

        var deleteCount = delnpcs.Count - 1;
        if (deleteCount > 1)
        {
            for (var i = 0; i < deleteCount; i++)
            {
                Logger.Trace($"Let's schedule npc removal {UnitId} from spawnerId {Template.Id}");
                ScheduleDespawn(delnpcs[i], false, 60);
            }
        }
    }

    /// <summary>
    /// Schedules the spawning of NPCs.
    /// </summary>
    /// <param name="all">Indicates whether to schedule spawning for all NPCs.</param>
    /// <returns>True if spawning was scheduled, otherwise false.</returns>
    private bool ScheduleSpawn(bool all = false)
    {
        if (Template == null)
        {
            Logger.Warn($"Can't spawn npc {UnitId} from spawnerId {Id}");
            return true;
        }

        if (_spawnCount >= Template.MaxPopulation)
        {
            Logger.Trace($"Let's not spawn Npc templateId {UnitId} from spawnerId {Template.Id} since exceeded MaxPopulation");
            return true;
        }

        _isSpawnScheduled = false;
        if (Template.StartTime > 0.0f | Template.EndTime > 0.0f)
        {
            var curTime = TimeManager.Instance.GetTime;
            if (!TimeSpan.FromHours(curTime).IsBetween(TimeSpan.FromHours(Template.StartTime), TimeSpan.FromHours(Template.EndTime)))
            {
                var start = (int)Math.Round(Template.StartTime);
                if (start == 0) start = 24;
                var delay = start - curTime;
                if (delay < 0f)
                {
                    delay = curTime + delay;
                }
                delay = delay * 60f * 10f;
                if (delay < 1f)
                {
                    delay = 5f;
                }
                _isSpawnScheduled = true;
                TaskManager.Instance.Schedule(new NpcSpawnerDoSpawnTask(this), TimeSpan.FromSeconds(delay));
                return true;
            }
        }
        else
        {
            //var scheduleSpawner = GameScheduleManager.Instance.CheckSpawnerInScheduleSpawners((int)Template.Id);
            //if (scheduleSpawner)
            //{
            //    var inGameSchedule = GameScheduleManager.Instance.CheckSpawnerInGameSchedules((int)Template.Id);
            //    if (inGameSchedule)
            //    {
            _isSpawnScheduled = true;
            var status = GameScheduleManager.Instance.GetPeriodStatusNpc((int)Template.Id);
            if (status == GameScheduleManager.PeriodStatus.NotFound)
            {
                isNotFoundInScheduler = true;
                Logger.Trace($"ScheduleSpawn: Npc was not found in the schedule, we will spawn it templateId={UnitId} from spawnerId={Template.Id}");
            }
            else if (status == GameScheduleManager.PeriodStatus.NotStarted)
            {
                Logger.Trace("Период еще не начался.");
                var cronExpression = GameScheduleManager.Instance.GetCronRemainingTime((int)Template.Id, true);
                if (cronExpression is "" or "0 0 0 0 0 ?")
                {
                    Logger.Trace($"ScheduleSpawn: Can't schedule spawn Npc templateId={UnitId} from spawnerId={Template.Id}");
                    Logger.Trace($"ScheduleSpawn: cronExpression {cronExpression}");
                    _isSpawnScheduled = false;
                    return true;
                }

                try
                {
                    TaskManager.Instance.CronSchedule(new NpcSpawnerDoSpawnTask(this), cronExpression);
                    Logger.Trace($"ScheduleSpawn: Schedule the spawn of Npc templateId={UnitId} from spawnerId={Template.Id}");
                    Logger.Trace($"ScheduleSpawn: cronExpression {cronExpression}");
                    return true;
                }
                catch (Exception)
                {
                    Logger.Trace($"ScheduleSpawn: Can't schedule spawn Npc templateId={UnitId} from spawnerId={Template.Id} on exception.");
                    Logger.Trace($"ScheduleSpawn: cronExpression {cronExpression}");
                    _isSpawnScheduled = false;
                    return true;
                }
            }
            else if (status == GameScheduleManager.PeriodStatus.InProgress)
            {
                Logger.Trace($"ScheduleSpawn: Can spawn. The period is already underway. Npc templateId={UnitId} from spawnerId={Template.Id}");
            }
            else if (status == GameScheduleManager.PeriodStatus.Ended)
            {
                Logger.Trace($"ScheduleSpawn: Can't spawn. The period has ended. Npc templateId={UnitId} from spawnerId={Template.Id}");
                _isSpawnScheduled = false;
                return true;
            }
            //    }
            //}
        }

        return false;
    }

    /// <summary>
    /// Schedules the despawning of the specified NPC.
    /// </summary>
    /// <param name="npc">The NPC to despawn.</param>
    /// <param name="all">Indicates whether to despawn all NPCs.</param>
    /// <param name="timeToDespawn">The time in seconds before despawning.</param>
    private void ScheduleDespawn(Npc npc, bool all = false, float timeToDespawn = 0)
    {
        if (timeToDespawn > 0)
        {
            TaskManager.Instance.Schedule(new NpcSpawnerDoDespawnTask(npc), TimeSpan.FromSeconds(timeToDespawn));
            return;
        }

        if (Template.StartTime > 0.0f | Template.EndTime > 0.0f)
        {
            var curTime = TimeManager.Instance.GetTime;
            if (TimeSpan.FromHours(curTime).IsBetween(TimeSpan.FromHours(Template.StartTime), TimeSpan.FromHours(Template.EndTime)))
            {
                var end = (int)Math.Round(Template.EndTime);
                if (end == 0) end = 24;
                var delay = end - curTime;
                if (delay < 0f)
                {
                    delay = curTime + delay;
                }
                delay = delay * 60f * 10f;
                if (delay < 1f)
                {
                    delay = 5f;
                }
                TaskManager.Instance.Schedule(new NpcSpawnerDoDespawnTask(npc), TimeSpan.FromSeconds(delay));
                return;
            }
        }
        else
        {
            //var scheduleSpawner = GameScheduleManager.Instance.CheckSpawnerInScheduleSpawners((int)Template.Id);
            //if (scheduleSpawner)
            //{
            //    var inGameSchedule = GameScheduleManager.Instance.CheckSpawnerInGameSchedules((int)Template.Id);
            //    if (inGameSchedule)
            //    {
            var status = GameScheduleManager.Instance.GetPeriodStatusNpc((int)Template.Id);
            if (status == GameScheduleManager.PeriodStatus.NotFound)
            {
                isNotFoundInScheduler = true;
                Logger.Trace($"ScheduleDespawn: Npc was not found in the schedule, we will despawn it templateId={UnitId} from spawnerId {Template.Id}");
            }
            else if (status == GameScheduleManager.PeriodStatus.NotStarted)
            {
                Logger.Trace($"ScheduleDespawn: The period has not yet begun. Can despawn Npc templateId={UnitId} from spawnerId {Template.Id}");
            }
            else if (status == GameScheduleManager.PeriodStatus.InProgress)
            {
                var cronExpression = GameScheduleManager.Instance.GetCronRemainingTime((int)Template.Id, false);
                if (cronExpression is "" or "0 0 0 0 0 ?")
                {
                    Logger.Trace($"ScheduleDespawn: Can't schedule despawn Npc templateId={UnitId} from spawnerId {Template.Id}");
                    Logger.Trace($"ScheduleDespawn: cronExpression {cronExpression}");
                    return;
                }
                try
                {
                    TaskManager.Instance.CronSchedule(new NpcSpawnerDoDespawnTask(npc), cronExpression);
                    Logger.Trace($"ScheduleDespawn: Schedule the despawn of Npc templateId={UnitId} from spawnerId {Template.Id}");
                    Logger.Trace($"ScheduleDespawn: cronExpression {cronExpression}");
                    return;
                }
                catch (Exception)
                {
                    Logger.Trace($"ScheduleDespawn: Can't schedule despawn Npc templateId={UnitId} from spawnerId {Template.Id}");
                    Logger.Trace($"ScheduleDespawn: cronExpression {cronExpression}");
                    return;
                }
            }
            else if (status == GameScheduleManager.PeriodStatus.Ended)
            {
                Logger.Trace($"ScheduleDespawn: The period has ended. Can despawn Npc templateId={UnitId} from spawnerId {Template.Id}");
            }
            //    }
            //}
        }

        DoDespawn(npc, all);

        if (isNotFoundInScheduler)
        {
            return;
        }

        ScheduleSpawn(all);
    }

    /// <summary>
    /// Spawns NPCs for an event.
    /// </summary>
    public async Task DoEventSpawn()
    {
        if (Template == null)
        {
            Logger.Error("Can't spawn npc {0} from spawnerId {1}", UnitId, Id);
            return;
        }

        if (_spawnCount >= Template.MaxPopulation)
        {
            return;
        }

        if (Template.SuspendSpawnCount > 0 && _spawnCount > Template.SuspendSpawnCount)
        {
            return;
        }

        var n = new List<Npc>();
        var nsnTask2 = Template.Npcs.FirstOrDefault(nsn => nsn.MemberId == UnitId);
        if (nsnTask2 != null)
        {
            n = await nsnTask2.SpawnAsync(this);
        }
        try
        {
            foreach (var npc in n)
            {
                _spawned.Add(npc);
            }
        }
        catch (Exception)
        {
            Logger.Error("Can't spawn npc {0} from spawnerId {1}", UnitId, Template.Id);
        }

        if (n.Count == 0)
        {
            Logger.Error("Can't spawn npc {0} from spawnerId {1}", UnitId, Template.Id);
            return;
        }
        _lastSpawn = n.LastOrDefault();
        if (_scheduledCount > 0)
        {
            Interlocked.Add(ref _scheduledCount, -n.Count);
        }
        Interlocked.Exchange(ref _spawnCount, _spawned.Count);
        if (_spawnCount < 0)
        {
            Interlocked.Exchange(ref _spawnCount, 0);
        }
    }

    /// <summary>
    /// Spawns NPCs with a specific effect.
    /// </summary>
    /// <param name="spawnerId">The ID of the spawner.</param>
    /// <param name="effect">The effect to apply.</param>
    /// <param name="caster">The unit that cast the effect.</param>
    /// <param name="target">The target unit.</param>
    public async Task DoSpawnEffect(uint spawnerId, SpawnEffect effect, BaseUnit caster, BaseUnit target)
    {
        var template = NpcGameData.Instance.GetNpcSpawnerTemplate(spawnerId);
        if (template?.Npcs == null)
        {
            return;
        }

        var n = new List<Npc>();
        var templateNsnTask2 = template.Npcs.FirstOrDefault(nsn => nsn != null && nsn.MemberId == UnitId);
        if (templateNsnTask2 != null)
        {
            n = await templateNsnTask2.SpawnAsync(this, template.MaxPopulation);
        }

        try
        {
            if (n == null) return;

            foreach (var npc in n)
            {
                if (npc.Spawner != null)
                {
                    npc.Spawner.RespawnTime = 0;
                }

                if (effect.UseSummonerFaction)
                {
                    npc.Faction = target is Npc ? target.Faction : caster.Faction;
                }

                if (effect.UseSummonerAggroTarget && !effect.UseSummonerFaction)
                {
                    if (target is Npc)
                    {
                        npc.Ai.Owner.AddUnitAggro(AggroKind.Damage, (Unit)target, 1);
                    }
                    else
                    {
                        npc.Ai.Owner.AddUnitAggro(AggroKind.Damage, (Unit)caster, 1);
                    }

                    npc.Ai.OnAggroTargetChanged();
                }

                if (effect.LifeTime > 0)
                {
                    TaskManager.Instance.Schedule(new NpcSpawnerDoDespawnTask(npc), TimeSpan.FromSeconds(effect.LifeTime));
                }
            }
        }
        catch (Exception)
        {
            Logger.Error("Can't spawn npc {0} from spawner {1}", UnitId, template.Id);
            return;
        }
        if (n.Count == 0)
        {
            Logger.Error("Can't spawn npc {0} from spawner {1}", UnitId, template.Id);
            return;
        }
        foreach (var npc in n)
        {
            _spawned.Add(npc);
        }
        if (_scheduledCount > 0)
        {
            Interlocked.Add(ref _scheduledCount, -n.Count);
        }
        Interlocked.Exchange(ref _spawnCount, _spawned.Count);
        if (_spawnCount < 0)
        {
            Interlocked.Exchange(ref _spawnCount, 0);
        }
        _lastSpawn = n.LastOrDefault();
        _lastSpawn.Spawner = this;
    }

    /// <summary>
    /// Clears the spawn count and all spawned NPCs.
    /// </summary>
    public void ClearSpawnCount()
    {
        Interlocked.Exchange(ref _spawnCount, 0);
        _spawned.Clear();
        _lastSpawn = null;
    }

    /// <summary>
    /// Checks if the spawner can spawn more NPCs.
    /// </summary>
    /// <returns>True if the spawner can spawn more NPCs, otherwise false.</returns>
    public bool CanSpawn()
    {
        return _spawnCount < Template.MaxPopulation;
    }

    /// <summary>
    /// Gets the last spawned NPC.
    /// </summary>
    /// <returns>The last spawned NPC.</returns>
    public Unit GetLastSpawn()
    {
        return _lastSpawn;
    }
}
