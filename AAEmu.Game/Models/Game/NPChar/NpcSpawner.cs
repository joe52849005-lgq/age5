using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.Id;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Items.Containers;
using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.World;
using AAEmu.Game.Models.Game.World.Transform;
using AAEmu.Game.Models.Tasks.World;
using AAEmu.Game.Utils;

using Newtonsoft.Json;

using NLog;

namespace AAEmu.Game.Models.Game.NPChar;

public class NpcSpawner : Spawner<Npc>
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    //private readonly ConcurrentBag<Npc> _spawned = [];
    //private Npc _lastSpawn;
    private int _scheduledCount;
    private int _spawnCount;
    private bool _isSpawnScheduled;
    private bool isNotFoundInScheduler;
    private readonly object _spawnLock = new(); // Блокировка для потокобезопасности

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
    [DefaultValue(1f)]
    public uint Count { get; set; } = 1;

    public List<uint> NpcSpawnerIds { get; set; } = [];
    public NpcSpawnerTemplate Template { get; set; }
    public List<NpcSpawnerNpc> SpawnableNpcs { get; set; } = []; // Список NPC, которые могут быть заспавнены
    public ConcurrentDictionary<uint, List<Npc>> SpawnedNpcs { get; set; } = []; // SpawnerId, Список NPC, которые заспавнены
    private DateTime _lastSpawnTime = DateTime.MinValue;

    public NpcSpawner()
    {
        _isSpawnScheduled = false;
    }

    /// <summary>
    /// Инициализирует список SpawnableNpcs на основе Template.Npcs.
    /// </summary>
    /// <param name="template"></param>
    internal void InitializeSpawnableNpcs(NpcSpawnerTemplate template)
    {
        if (template?.Npcs == null)
        {
            Logger.Warn("Template or template.Npcs is null. SpawnableNpcs will not be initialized.");
            return;
        }

        SpawnableNpcs = [.. template.Npcs];
    }

    /// <summary>
    /// Обновляет состояние спавнера.
    /// </summary>
    public void Update()
    {
        // Проверяем, можно ли деспавнить NPCs
        if (CanDespawn())
        {
            if (SpawnedNpcs.TryGetValue(SpawnerId, out var npcs))
            {
                DoDespawn(npcs);
                SpawnedNpcs[SpawnerId].Clear();
                return;
            }
        }

        // Проверяем, можно ли спавнить NPCs
        if (!CanSpawn())
            return;

        // Проверяем, прошло ли достаточно времени с последнего спавна
        if (_lastSpawnTime != DateTime.MinValue && (DateTime.UtcNow - _lastSpawnTime).TotalSeconds < Template.SpawnDelayMin)
            return;

        // Спавним NPCs
        DoSpawn();
    }

    /// <summary>
    /// Проверяет, можно ли спавнить NPC.
    /// </summary>
    /// <returns>True, если можно спавнить, иначе False.</returns>
    private bool CanSpawn()
    {
        if (Template == null)
        {
            //Logger.Warn($"Template is null. Cannot determine if NPC {SpawnerId}:{UnitId} can be spawned.");
            return false;
        }

        //// Проверяем, находится ли спавнер в правильной категории
        //if (Template.NpcSpawnerCategoryId == NpcSpawnerCategory.Autocreated && NpcSpawnerIds.Count > 1)
        //{
        //    Logger.Warn($"Cannot spawn NPC: {SpawnerId}:{UnitId} No players in spawn Autocreated.");
        //    return false;
        //}

        if (!ScheduleSpawn())
        {
            //Logger.Warn($"Cannot spawn NPC: {SpawnerId}:{UnitId} Some NPCs are outside the period.");
            return false; // не спавним, если период закончился
        }

        if (!IsPlayerInSpawnRadius())
        {
            //Logger.Warn($"Cannot spawn NPC: {SpawnerId}:{UnitId} No players in spawn radius.");
            return false;
        }

        //if (SpawnedNpcs?.Values.Any(npcs => npcs != null && npcs.Any(npc => !IsNpcInSpawnRadius(npc))) != false)
        //{
        //    var outOfRadiusCount = SpawnedNpcs?.Values.Sum(npcs => npcs?.Count(npc => !IsNpcInSpawnRadius(npc)) ?? 0);
        //    Logger.Warn($"Cannot spawn NPC: {SpawnerId}:{UnitId}. {outOfRadiusCount} NPCs are outside spawn radius.");
        //    return false;
        //}

        if (Template.SuspendSpawnCount > 0 && _spawnCount >= Template.SuspendSpawnCount)
        {
            //Logger.Warn($"Cannot spawn NPC: {SpawnerId}:{UnitId} Suspended spawn count ({Template.SuspendSpawnCount}) reached.");
            return false;
        }

        if (_spawnCount >= Template.MaxPopulation)
        {
            //Logger.Warn($"Cannot spawn NPC: {SpawnerId}:{UnitId} Max population ({Template.MaxPopulation}) reached.");
            return false;
        }

        if (_spawnCount < Template.MinPopulation)
        {
            //Logger.Warn($"Can spawn NPC: {SpawnerId}:{UnitId} Current population ({_spawnCount}) is below minimum ({Template.MinPopulation}).");
            return true;
        }

        //Logger.Warn($"All conditions met. NPC {SpawnerId}:{UnitId} can be spawned.");
        return true;
    }

    private bool CanSpawn0()
    {
        // Проверяем, достигнуто ли максимальное количество NPC
        if (_spawnCount >= Template.MaxPopulation)
            return false;

        // Проверяем, достигнуто ли минимальное количество NPC
        if (_spawnCount < Template.MinPopulation)
            return true;

        // Проверяем, не превышен ли SuspendSpawnCount
        if (Template.SuspendSpawnCount > 0 && _spawnCount >= Template.SuspendSpawnCount)
            return false;

        //// Проверяем, находится ли спавнер в активном состоянии
        //if (!Template.ActivationState)
        //    return false;

        //// Проверяем, находится ли спавнер в правильной категории
        //if (Template.NpcSpawnerCategoryId == NpcSpawnerCategory.Autocreated && NpcSpawnerIds.Count > 1)
        //    return false;

        // Проверяем, находится ли игрок в радиусе спавна
        if (!IsPlayerInSpawnRadius())
            return false;

        // Проверяем, находится ли NPC в радиусе спавна
        if (SpawnedNpcs?.Values.Any(npcs => npcs != null && npcs.Any(npc => !IsNpcInSpawnRadius(npc))) != false)
        {
            var outOfRadiusCount = SpawnedNpcs?.Values.Sum(npcs => npcs?.Count(npc => !IsNpcInSpawnRadius(npc)) ?? 0);
            Logger.Warn($"Cannot spawn NPC: {SpawnerId}:{UnitId}. {outOfRadiusCount} NPCs are outside spawn radius.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Проверяет, можно ли деспавнить NPC.
    /// </summary>
    /// <returns>True, если можно спавнить, иначе False.</returns>
    private bool CanDespawn()
    {
        if (ScheduleDespawn(SpawnerId))
            return true; // Деспавним, если период закончился

        // Проверяем, находится ли игрок в радиусе спавна
        if (IsPlayerInSpawnRadius())
        {
            //Logger.Warn($"Can spawn NPC: {SpawnerId}:{UnitId} Players in spawn radius or radius == 0.");
            return false; // Игрок в радиусе, не деспавним
        }

        // Проверяем, находится ли NPC в радиусе спавна
        //if (_spawned.Any(npc => IsNpcInSpawnRadius(npc)))
        //    return false; // NPC в радиусе, не деспавним

        //Logger.Warn($"Cannot spawn NPC: {SpawnerId}:{UnitId} No players in spawn radius.");
        return true; // Деспавним, если игрок и NPC не в радиусе
    }

    /// <summary>
    /// Спавнит указанного NPC.
    /// </summary>
    /// <param name="npc">NPC для спавна.</param>
    public void SpawnNpc(Npc npc)
    {
        if (npc == null)
        {
            Logger.Warn("Attempted to spawn a null NPC.");
            return;
        }

        lock (_spawnLock)
        {
            npc.Transform.ApplyWorldSpawnPosition(Position);
            npc.RegisterNpcEvents();
            AddNpcToSpawned(SpawnerId, npc);
            Interlocked.Increment(ref _spawnCount);
            _lastSpawnTime = DateTime.UtcNow;
        }

        Logger.Trace($"Spawned NPC {npc.TemplateId} at {Position}.");
    }

    public void SpawnNpc0(Npc npc)
    {
        if (npc == null)
            return;

        // Устанавливаем позицию спавна
        npc.Transform.ApplyWorldSpawnPosition(Position);

        // Регистрируем события NPC
        npc.RegisterNpcEvents();

        // Добавляем NPC в список заспавненных
        AddNpcToSpawned(SpawnerId, npc);
        _spawnCount++;

        // Обновляем время последнего спавна
        _lastSpawnTime = DateTime.UtcNow;

        Logger.Trace($"Spawned NPC {npc.TemplateId} at {Position}.");
    }

    /// <summary>
    /// Выбирает NPC для спавна на основе SpawnableNpcs.
    /// </summary>
    /// <returns>Выбранный NPC или null, если не удалось выбрать.</returns>
    private Npc ChooseNpcToSpawn()
    {
        if (SpawnableNpcs == null || SpawnableNpcs.Count == 0)
        {
            Logger.Warn("No spawnable NPCs available.");
            return null;
        }

        var totalWeight = SpawnableNpcs.Sum(n => n.Weight);
        var randomValue = Rand.Next(0, (int)totalWeight);

        foreach (var npcTemplate in SpawnableNpcs)
        {
            if (randomValue < npcTemplate.Weight)
            {
                var npc = NpcManager.Instance.Create(0, npcTemplate.MemberId);
                if (npc != null)
                {
                    return npc;
                }
                else
                {
                    Logger.Error($"Failed to create NPC from template {npcTemplate.MemberId}.");
                }
            }
            randomValue -= (int)npcTemplate.Weight;
        }

        Logger.Warn("No NPC was chosen to spawn.");
        return null;
    }

    private Npc ChooseNpcToSpawn0()
    {
        if (SpawnableNpcs == null || SpawnableNpcs.Count == 0)
            return null;

        // Выбираем случайного NPC из списка, учитывая вес
        var totalWeight = SpawnableNpcs.Sum(n => n.Weight);
        var randomValue = Rand.Next(0, (int)totalWeight);

        foreach (var npcTemplate in SpawnableNpcs)
        {
            if (randomValue < npcTemplate.Weight)
            {
                var npc = NpcManager.Instance.Create(0, npcTemplate.MemberId);
                if (npc != null)
                {
                    return npc;
                }
            }
            randomValue -= (int)npcTemplate.Weight;
        }

        return null;
    }


    /// <summary>
    /// Проверяет, находится ли NPC в радиусе спавна.
    /// </summary>
    /// <param name = "npc" > NPC для проверки.</param>
    /// <returns>True, если NPC находится в радиусе, иначе False.</returns>
    private bool IsNpcInSpawnRadius(Npc npc)
    {
        if (npc == null)
            return false;

        if (Template.TestRadiusNpc == 0)
            return true;

        var distance = MathUtil.CalculateDistance(npc.Spawner.Position, Position);
        return distance <= Template.TestRadiusNpc;
    }

    /// <summary>
    /// Проверяет, находится ли игрок в радиусе спавна.
    /// </summary>
    /// <returns>True, если игрок находится в радиусе, иначе False.</returns>
    private bool IsPlayerInSpawnRadius()
    {

        if (Template.TestRadiusPc == 0)
            return true;

        var players = WorldManager.Instance.GetAllCharacters();
        foreach (var player in players)
        {
            var distance = MathUtil.CalculateDistance(player.Transform.World.Position, new Vector3(Position.X, Position.Y, Position.Z));
            if (distance <= Template.TestRadiusPc * 3)
                return true;
        }

        return false;
    }

    //private bool AreNpcsInRadius(WorldSpawnPosition position, float radius)
    //{
    //    // Логика проверки наличия NPC в радиусе
    //    // Например, проверка списка активных NPC
    //    return SpawnableNpcs.Any(npc => Vector3.Distance(npc.Spawner.Position.ToVector3(), position.ToVector3()) <= radius);
    //}

    private bool ArePlayersInRadius(WorldSpawnPosition position, float radius)
    {
        // Логика проверки наличия игроков в радиусе
        // Например, проверка списка активных игроков
        return true; // Заглушка для примера
    }

    /// <summary>
    /// Spawns all NPCs associated with this spawner.
    /// </summary>
    /// <param name="beginning">Indicates if this is the initial spawn at the start of the game.</param>
    /// <returns>A list of spawned NPCs, or null if spawning was scheduled.</returns>
    public List<Npc> SpawnAll(bool beginning = false)
    {
        if (ScheduleSpawn())
        {
            return null;
        }

        DoSpawn();

        if (_isSpawnScheduled)
        {
            ScheduleDespawn(SpawnerId);
        }

        return SpawnedNpcs[SpawnerId];
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

        if (_isSpawnScheduled)
        {
            ScheduleDespawn(SpawnerId);
        }

        return SpawnedNpcs[SpawnerId][0];
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

        lock (_spawnLock) // Блокировка для потокобезопасности
        {
            if (npc.Respawn == DateTime.MinValue)
            {
                //_spawned.TryTake(out _);
                ObjectIdManager.Instance.ReleaseId(npc.ObjId);
                Interlocked.Decrement(ref _spawnCount);

                // Если количество заспавненных NPC стало отрицательным, сбрасываем его в 0
                if (_spawnCount < 0)
                {
                    Interlocked.Exchange(ref _spawnCount, 0);
                }

                //SpawnedNpcs[SpawnerId].Clear();
            }
        }

        // Освобождаем ресурсы NPC
        //npc.Dispose();
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
        if (npc == null)
        {
            Logger.Warn("Attempted to decrease count for a null NPC.");
            return;
        }

        Interlocked.Decrement(ref _spawnCount);
        //_spawned.TryTake(out _);

        if (RespawnTime > 0 && _spawnCount + _scheduledCount < Count)
        {
            npc.Respawn = DateTime.UtcNow.AddSeconds(RespawnTime);
            SpawnManager.Instance.AddRespawn(npc);
            Interlocked.Increment(ref _scheduledCount);
            Logger.Trace($"Scheduled respawn for NPC {npc.ObjId} in {RespawnTime} seconds.");
        }

        npc.Despawn = DateTime.UtcNow.AddSeconds(DespawnTime);
        if (npc.LootingContainer?.Items.Count > 0)
        {
            npc.Despawn += TimeSpan.FromSeconds(LootingContainer.LootDespawnExtensionTime);
        }
        SpawnManager.Instance.AddDespawn(npc);

        Logger.Trace($"Decreased spawn count for NPC {npc.ObjId}. New count: {_spawnCount}.");
    }

    public void DecreaseCount0(Npc npc)
    {
        if (npc == null) return;

        Interlocked.Decrement(ref _spawnCount);
        //_spawned.TryTake(out _);

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
        //_spawned.TryTake(out _);

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
        // Проверяем, что NPC не null
        if (npc == null)
        {
            Logger.Warn($"Attempted to despawn a null NPC {SpawnerId}:{UnitId}.");
            return;
        }

        // Проверяем, находится ли NPC в бою
        if (npc.IsInBattle)
        {
            Logger.Trace($"NPC {SpawnerId}:{UnitId}{npc.ObjId} is in battle and cannot be despawned.");
            return;
        }

        // Блокировка для потокобезопасности
        lock (_spawnLock)
        {
            if (all)
            {
                // Деспавним всех NPC
                Logger.Debug($"Despawning all NPCs {SpawnerId}:{UnitId} from spawner {Template.Id}.");

                //// Создаем копию списка, чтобы избежать изменения коллекции во время итерации
                //var npcsToDespawn = _spawned.ToList();

                foreach (var npcsToDespawn in SpawnedNpcs.Values)
                {
                    foreach (var npcToDespawn in npcsToDespawn)
                    {
                        Despawn(npcToDespawn);
                    }
                }
            }
            else
            {
                // Деспавним конкретного NPC
                Logger.Debug($"Despawning NPC  {SpawnerId}:{UnitId}:{npc.ObjId} from spawner {Template.Id}.");
                Despawn(npc);
            }
        }
    }

    public void DoDespawn(List<Npc> npcs)
    {
        // Блокировка для потокобезопасности
        lock (_spawnLock)
        {
            // Проверяем, что NPCs не null
            if (npcs == null)
            {
                //Logger.Warn($"Attempted to despawn a null NPC {SpawnerId}:{UnitId}.");
                return;
            }

            if (npcs.Count != 0)
            {
                foreach (var npc in npcs)
                {
                    // Проверяем, находится ли NPC в бою
                    if (npc.IsInBattle)
                    {
                        //Logger.Trace($"NPC {SpawnerId}:{UnitId}{npc.ObjId} is in battle and cannot be despawned.");
                        continue;
                    }

                    //Logger.Debug($"Despawning NPC  {SpawnerId}:{UnitId}:{npc.ObjId} from spawner {Template.Id}.");
                    Despawn(npc);
                }
            }
        }
    }

    /// <summary>
    /// Spawns NPCs, optionally spawning all NPCs.
    /// </summary>
    /// <param name="all">Indicates whether to spawn all NPCs.</param>
    /// <param name="beginning">Indicates if this is the initial spawn at the start of the game.</param>
    public void DoSpawn()
    {
        // Список для хранения всех заспавненных NPC
        var allNpcs = new List<Npc>();

        // Проходим по всем шаблонам NPC, которые могут быть заспавнены
        foreach (var spawnableNpc in SpawnableNpcs)
        {
            try
            {
                // Проверяем, что шаблон NPC не null
                if (spawnableNpc != null)
                {
                    var npcs = spawnableNpc.Spawn(this);
                    if (npcs == null || npcs.Count == 0)
                    {
                        //Logger.Warn($"Failed to spawn NPC from template {spawnableNpc.MemberId}: Spawn returned null or empty list.");
                        continue;
                    }

                    // Добавляем заспавненных NPC в общий список
                    foreach (var npc in npcs)
                    {
                        //Logger.Debug($"Spawned {allNpcs.Count} NPCs {npc.Spawner.SpawnerId}:{npc.TemplateId}.");
                        AddNpcToSpawned(npc.Spawner.SpawnerId, npc);
                    }
                    allNpcs.AddRange(npcs);
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, если что-то пошло не так
                Logger.Error(ex, $"Failed to spawn NPC from template {spawnableNpc?.MemberId}");
            }
        }

        // Если ни одного NPC не удалось заспавнить, выводим ошибку и завершаем метод
        if (allNpcs.Count == 0)
        {
            Logger.Error($"Can't spawn npc {UnitId} from spawnerId {Template.Id}");
            return;
        }

        // Блокировка для потокобезопасности
        lock (_spawnLock)
        {
            //// Добавляем всех заспавненных NPC в список _spawned
            //foreach (var npc in allNpcs)
            //{
            //    AddNpcToSpawned(SpawnerId, npc);
            //}

            // Обновляем счетчик запланированных NPC
            if (_scheduledCount > 0)
            {
                Interlocked.Add(ref _scheduledCount, -allNpcs.Count);
            }

            // Обновляем общее количество заспавненных NPC
            Interlocked.Add(ref _spawnCount, allNpcs.Count);

            // Если количество заспавненных NPC стало отрицательным, сбрасываем его в 0
            if (_spawnCount < 0)
            {
                Interlocked.Exchange(ref _spawnCount, 0);
            }
        }
    }

    public async Task DoSpawnAsync(bool all = false, bool beginning = false)
    {
        // Список для хранения всех заспавненных NPC
        var allNpcs = new List<Npc>();

        // Проходим по всем шаблонам NPC, которые могут быть заспавнены
        foreach (var spawnableNpcTask in SpawnableNpcs)
        {
            try
            {
                // Проверяем, что шаблон NPC не null
                if (spawnableNpcTask != null)
                {
                    // Спавним NPC асинхронно
                    var npcs = await spawnableNpcTask.SpawnAsync(this);
                    if (npcs == null || npcs.Count == 0)
                    {
                        Logger.Warn($"Failed to spawn NPC from template {spawnableNpcTask.MemberId}: SpawnAsync returned null or empty list.");
                        continue;
                    }

                    // Добавляем заспавненных NPC в общий список
                    allNpcs.AddRange(npcs);
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, если что-то пошло не так
                Logger.Error(ex, $"Failed to spawn NPC from template {spawnableNpcTask?.MemberId}");
            }
        }

        // Если ни одного NPC не удалось заспавнить, выводим ошибку и завершаем метод
        if (allNpcs.Count == 0)
        {
            Logger.Error($"Can't spawn npc {UnitId} from spawnerId {Template.Id}");
            return;
        }

        // Блокировка для потокобезопасности
        lock (_spawnLock)
        {
            // Добавляем всех заспавненных NPC в список _spawned
            foreach (var npc in allNpcs)
            {
                AddNpcToSpawned(SpawnerId, npc);
            }

            // Обновляем счетчик запланированных NPC
            if (_scheduledCount > 0)
            {
                Interlocked.Add(ref _scheduledCount, -allNpcs.Count);
            }

            // Обновляем общее количество заспавненных NPC
            Interlocked.Add(ref _spawnCount, allNpcs.Count);

            // Если количество заспавненных NPC стало отрицательным, сбрасываем его в 0
            if (_spawnCount < 0)
            {
                Interlocked.Exchange(ref _spawnCount, 0);
            }
        }

        Logger.Trace($"Spawned {allNpcs.Count} NPCs from spawnerId {Template.Id}.");
    }

    public async Task DoSpawn0(bool all = false, bool beginning = false)
    {
        // Список для хранения всех заспавненных NPC
        var allNpcs = new List<Npc>();

        // Проходим по всем шаблонам NPC, которые могут быть заспавнены
        foreach (var spawnableNpcTask in SpawnableNpcs)
        {
            try
            {
                // Проверяем, что шаблон NPC не null
                if (spawnableNpcTask != null)
                {
                    // Спавним NPC асинхронно
                    var npcs = await spawnableNpcTask.SpawnAsync(this);
                    if (npcs == null)
                    {
                        Logger.Warn($"Failed to spawn NPC from template {spawnableNpcTask.MemberId}: SpawnAsync returned null.");
                        continue;
                    }

                    // Добавляем заспавненных NPC в общий список
                    allNpcs.AddRange(npcs);
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, если что-то пошло не так
                Logger.Error(ex, $"Failed to spawn NPC from template {spawnableNpcTask?.MemberId}");
            }
        }

        // Если ни одного NPC не удалось заспавнить, выводим ошибку и завершаем метод
        if (allNpcs.Count == 0)
        {
            Logger.Error($"Can't spawn npc {UnitId} from spawnerId {Template.Id}");
            return;
        }

        // Блокировка для потокобезопасности
        lock (_spawnLock)
        {
            // Добавляем всех заспавненных NPC в список _spawned
            foreach (var npc in allNpcs)
            {
                AddNpcToSpawned(SpawnerId, npc);
            }

            // Обновляем счетчик запланированных NPC
            if (_scheduledCount > 0)
            {
                Interlocked.Add(ref _scheduledCount, -allNpcs.Count);
            }

            // Обновляем общее количество заспавненных NPC
            Interlocked.Add(ref _spawnCount, allNpcs.Count);

            // Если количество заспавненных NPC стало отрицательным, сбрасываем его в 0
            if (_spawnCount < 0)
            {
                Interlocked.Exchange(ref _spawnCount, 0);
            }
        }

        Logger.Trace($"Spawned {allNpcs.Count} NPCs from spawnerId {Template.Id}.");
    }

    /// <summary>
    /// Schedules the spawning of NPCs.
    /// </summary>
    /// <param name="all">Indicates whether to schedule spawning for all NPCs.</param>
    /// <returns>True if spawning was scheduled, otherwise false.</returns>
    private bool ScheduleSpawn()
    {
        if (Template == null)
        {
            Logger.Warn($"Can't spawn npc {SpawnerId}:{UnitId} from index={Id}");
            return false;
        }

        _isSpawnScheduled = false;
        //if (Template.StartTime > 0.0f | Template.EndTime > 0.0f)
        //{
        //    var curTime = TimeManager.Instance.GetTime;
        //    if (!TimeSpan.FromHours(curTime).IsTimeBetween(TimeSpan.FromHours(Template.StartTime), TimeSpan.FromHours(Template.EndTime)))
        //    {
        //        _isSpawnScheduled = true;
        //        Logger.Warn($"ScheduleSpawn: Can spawn. The period is already underway. Npc templateId={SpawnerId}:{UnitId} from index={Template.Id}");
        //        return true;
        //    }

        //    Logger.Warn($"ScheduleSpawn: Can't spawn. The period has not yet begun. Npc templateId={SpawnerId}:{UnitId} from index={Template.Id}");
        //    return false;
        //}
        if (Template.StartTime > 0.0f || Template.EndTime > 0.0f)
        {
            var curTime = TimeManager.Instance.GetTime;

            var startTime = TimeSpan.FromHours(Template.StartTime);
            var endTime = TimeSpan.FromHours(Template.EndTime);
            var currentTime = TimeSpan.FromHours(curTime);

            if (IsTimeBetween(currentTime, startTime, endTime))
            {
                _isSpawnScheduled = false;
                //Logger.Warn($"ScheduleSpawn: Can spawn. The period is already underway. Npc templateId={SpawnerId}:{UnitId} from index={Template.Id}");
                return true;
            }

            //Logger.Warn($"ScheduleSpawn: Can't spawn. The period has not yet begun. Npc templateId={SpawnerId}:{UnitId} from index={Template.Id}");
            return false;

        }

        var status = GameScheduleManager.Instance.GetPeriodStatusNpc((int)Template.Id);
        switch (status)
        {
            case GameScheduleManager.PeriodStatus.NotFound:
                isNotFoundInScheduler = true;
                _isSpawnScheduled = false;
                //Logger.Warn($"ScheduleSpawn: Npc was not found in the schedule, we will spawn it templateId={SpawnerId}:{UnitId} from index={Template.Id}");
                return true;
            case GameScheduleManager.PeriodStatus.NotStarted:
                //Logger.Warn($"ScheduleSpawn: Can't spawn. The period has not yet begun. Npc templateId={SpawnerId}:{UnitId} from index={Template.Id}");
                isNotFoundInScheduler = false;
                _isSpawnScheduled = false;
                return false;
            case GameScheduleManager.PeriodStatus.InProgress:
                //Logger.Warn($"ScheduleSpawn: Can spawn. The period is already underway. Npc templateId={SpawnerId}:{UnitId} from index={Template.Id}");
                isNotFoundInScheduler = false;
                _isSpawnScheduled = true;
                return true;
            case GameScheduleManager.PeriodStatus.Ended:
                //Logger.Warn($"ScheduleSpawn: Can't spawn. The period has ended. Npc templateId={SpawnerId}:{UnitId} from index={Template.Id}");
                isNotFoundInScheduler = false;
                _isSpawnScheduled = false;
                return false;
            default:
                isNotFoundInScheduler = true;
                _isSpawnScheduled = false;
                return false;
        }
    }

    /// <summary>
    /// Schedules the despawning of the specified NPC.
    /// </summary>
    /// <param name="npc">The NPC to despawn.</param>
    /// <param name="all">Indicates whether to despawn all NPCs.</param>
    /// <param name="timeToDespawn">The time in seconds before despawning.</param>
    private void ScheduleDespawn2(Npc npc, bool all = false, float timeToDespawn = 0)
    {
        isNotFoundInScheduler = false;
        if (Template.StartTime > 0.0f || Template.EndTime > 0.0f)
        {
            var curTime = TimeManager.Instance.GetTime;
            if (TimeSpan.FromHours(curTime).IsTimeBetween(TimeSpan.FromHours(Template.StartTime), TimeSpan.FromHours(Template.EndTime)))
            {
                _isSpawnScheduled = false;
                return;
            }
        }
        else
        {
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
                Logger.Trace("Период еще не кончился.");
                return;
            }
            else if (status == GameScheduleManager.PeriodStatus.Ended)
            {
                Logger.Trace($"ScheduleDespawn: The period has ended. Can despawn Npc templateId={UnitId} from spawnerId {Template.Id}");
            }
        }

        DoDespawn(npc, all);

        if (isNotFoundInScheduler)
        {
            return;
        }

        ScheduleSpawn();
    }

    private static bool IsTimeBetween(TimeSpan currentTime, TimeSpan startTime, TimeSpan endTime)
    {
        if (startTime <= endTime)
            return currentTime >= startTime && currentTime <= endTime;

        return currentTime >= startTime || currentTime <= endTime;
    }

    private bool ScheduleDespawn(uint spawnerId)
    {

        if (!SpawnedNpcs.TryGetValue(spawnerId, out var npcs))
            return false;

        var res = false;

        foreach (var npc in npcs)
        {
            isNotFoundInScheduler = false;
            //if (npc.Spawner.Template.StartTime > 0.0f | npc.Spawner.Template.EndTime > 0.0f)
            //{
            //    var curTime = TimeManager.Instance.GetTime;
            //    if (TimeSpan.FromHours(curTime).IsTimeBetween(TimeSpan.FromHours(npc.Spawner.Template.StartTime), TimeSpan.FromHours(npc.Spawner.Template.EndTime)))
            //    {
            //        _isSpawnScheduled = false;
            //        Logger.Warn($"ScheduleDespawn: Can't despawn. The period is already underway. Npc templateId={SpawnerId}:{UnitId} from index={npc.Spawner.Template.Id}");
            //        res = false;
            //    }
            //    else
            //    {
            //        Logger.Warn($"ScheduleDespawn: Can despawn Npc templateId={npc.Spawner.SpawnerId}:{npc.Spawner.UnitId} from index={npc.Spawner.Template.Id}");
            //        res = true;
            //    }
            //}

            if (npc.Spawner.Template.StartTime > 0.0f || npc.Spawner.Template.EndTime > 0.0f)
            {
                var curTime = TimeManager.Instance.GetTime;

                var startTime = TimeSpan.FromHours(npc.Spawner.Template.StartTime);
                var endTime = TimeSpan.FromHours(npc.Spawner.Template.EndTime);
                var currentTime = TimeSpan.FromHours(curTime);

                if (IsTimeBetween(currentTime, startTime, endTime))
                {
                    _isSpawnScheduled = false;
                    //Logger.Warn($"ScheduleDespawn: Can't despawn. The period is already underway. Npc templateId={SpawnerId}:{UnitId} from index={npc.Spawner.Template.Id}");
                    res = false;
                    break;
                }

                //Logger.Warn($"ScheduleDespawn: Can despawn Npc templateId={npc.Spawner.SpawnerId}:{npc.Spawner.UnitId} from index={npc.Spawner.Template.Id}");
                res = true;
                break;
            }

            var status = GameScheduleManager.Instance.GetPeriodStatusNpc((int)npc.Spawner.Template.Id);
            switch (status)
            {
                case GameScheduleManager.PeriodStatus.NotFound:
                    isNotFoundInScheduler = true;
                    //Logger.Warn($"ScheduleDespawn: Npc was not found in the schedule, we will despawn it templateId={npc.Spawner.SpawnerId}:{npc.Spawner.UnitId} from index={npc.Spawner.Template.Id}");
                    res = false;
                    break;
                case GameScheduleManager.PeriodStatus.NotStarted:
                    //Logger.Warn($"ScheduleDespawn: The period has not yet begun. Can despawn Npc templateId={npc.Spawner.SpawnerId}:{npc.Spawner.UnitId} from index={npc.Spawner.Template.Id}");
                    res = true;
                    break;
                case GameScheduleManager.PeriodStatus.InProgress:
                    //Logger.Warn($"ScheduleDespawn: Can't despawn. The period is already underway. Npc templateId={npc.Spawner.SpawnerId}:{npc.Spawner.UnitId} from index={npc.Spawner.Template.Id}");
                    res = false;
                    break;
                case GameScheduleManager.PeriodStatus.Ended:
                    //Logger.Warn($"ScheduleDespawn: The period has ended. Can despawn Npc templateId={npc.Spawner.SpawnerId}:{npc.Spawner.UnitId} from index={npc.Spawner.Template.Id}");
                    res = true;
                    break;
                default:
                    res = true;
                    break;
            }
        }

        return res;
    }

    /// <summary>
    /// Spawns NPCs for an event.
    /// </summary>
    public void DoEventSpawn()
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
            n = nsnTask2.Spawn(this); // Заменяем SpawnAsync на Spawn
        }
        try
        {
            foreach (var npc in n)
            {
                AddNpcToSpawned(SpawnerId, npc);
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

        if (_scheduledCount > 0)
        {
            Interlocked.Add(ref _scheduledCount, -n.Count);
        }

        if (SpawnedNpcs.TryGetValue(SpawnerId, out var npcList))
        {
            lock (npcList) // Блокируем список для потокобезопасности
            {
                Interlocked.Exchange(ref _spawnCount, npcList.Count);
            }
        }
        else
        {
            // Если список для SpawnerId не существует, устанавливаем _spawnCount в 0
            Interlocked.Exchange(ref _spawnCount, 0);
        }

        if (_spawnCount < 0)
        {
            Interlocked.Exchange(ref _spawnCount, 0);
        }
    }

    public async Task DoEventSpawnAsync()
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
                AddNpcToSpawned(SpawnerId, npc);
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

        if (_scheduledCount > 0)
        {
            Interlocked.Add(ref _scheduledCount, -n.Count);
        }

        if (SpawnedNpcs.TryGetValue(SpawnerId, out var npcList))
        {
            lock (npcList) // Блокируем список для потокобезопасности
            {
                Interlocked.Exchange(ref _spawnCount, npcList.Count);
            }
        }
        else
        {
            // Если список для SpawnerId не существует, устанавливаем _spawnCount в 0
            Interlocked.Exchange(ref _spawnCount, 0);
        }

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
    public void DoSpawnEffect(uint spawnerId, SpawnEffect effect, BaseUnit caster, BaseUnit target)
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
            n = templateNsnTask2.Spawn(this); // Заменяем SpawnAsync на Spawn
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
            AddNpcToSpawned(SpawnerId, npc);
        }
        if (_scheduledCount > 0)
        {
            Interlocked.Add(ref _scheduledCount, -n.Count);
        }

        if (SpawnedNpcs.TryGetValue(SpawnerId, out var npcList))
        {
            lock (npcList) // Блокируем список для потокобезопасности
            {
                Interlocked.Exchange(ref _spawnCount, npcList.Count);
            }
        }
        else
        {
            // Если список для SpawnerId не существует, устанавливаем _spawnCount в 0
            Interlocked.Exchange(ref _spawnCount, 0);
        }

        if (_spawnCount < 0)
        {
            Interlocked.Exchange(ref _spawnCount, 0);
        }
    }
    public async Task DoSpawnEffectAsync(uint spawnerId, SpawnEffect effect, BaseUnit caster, BaseUnit target)
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
            n = await templateNsnTask2.SpawnAsync(this);
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
            AddNpcToSpawned(SpawnerId, npc);
        }
        if (_scheduledCount > 0)
        {
            Interlocked.Add(ref _scheduledCount, -n.Count);
        }

        if (SpawnedNpcs.TryGetValue(SpawnerId, out var npcList))
        {
            lock (npcList) // Блокируем список для потокобезопасности
            {
                Interlocked.Exchange(ref _spawnCount, npcList.Count);
            }
        }
        else
        {
            // Если список для SpawnerId не существует, устанавливаем _spawnCount в 0
            Interlocked.Exchange(ref _spawnCount, 0);
        }

        if (_spawnCount < 0)
        {
            Interlocked.Exchange(ref _spawnCount, 0);
        }
    }

    /// <summary>
    /// Clears the spawn count and all spawned NPCs.
    /// </summary>
    public void ClearSpawnCount()
    {
        lock (_spawnLock)
        {
            SpawnedNpcs[SpawnerId].Clear();
            Interlocked.Exchange(ref _spawnCount, 0);
        }

        Logger.Trace("Spawn count cleared.");
    }

    public void ClearSpawnCount0()
    {
        lock (_spawnLock) // Блокировка для потокобезопасности
        {
            //foreach (var npc in _spawned)
            //{
            //    npc.Dispose(); // Очистка ресурсов
            //}
            SpawnedNpcs[SpawnerId].Clear();
            Interlocked.Exchange(ref _spawnCount, 0);
        }
    }

    public static T Clone<T>(T obj)
    {
        var inst = obj.GetType().GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        return (T)inst?.Invoke(obj, null);
    }

    private void AddNpcToSpawned(uint key, Npc newNpc)
    {
        // Проверяем, что новый NPC не null
        if (newNpc == null)
        {
            Logger.Warn("Attempted to add a null NPC to SpawnedNpcs.");
            return;
        }

        // Используем потокобезопасный метод AddOrUpdate для работы с ConcurrentDictionary
        SpawnedNpcs.AddOrUpdate(
            key, // Ключ, по которому добавляем NPC
            k =>
            {
                // Если ключа нет, создаем новый список и добавляем NPC
                var newNpcList = new List<Npc> { newNpc };
                Logger.Trace($"Created new NPC list for key {k} and added NPC {newNpc.ObjId}.");
                return newNpcList;
            },
            (k, existingNpcList) =>
            {
                // Если ключ уже существует, добавляем NPC в существующий список
                lock (existingNpcList) // Блокируем список для потокобезопасности
                {
                    existingNpcList.Add(newNpc);
                    Logger.Trace($"Added NPC {newNpc.ObjId} to existing list for key {k}.");
                    return existingNpcList;
                }
            }
        );
    }
}
