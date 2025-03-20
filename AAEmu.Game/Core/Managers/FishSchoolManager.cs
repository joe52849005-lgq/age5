using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.StaticValues;

using NLog;

namespace AAEmu.Game.Core.Managers
{
    public class FishSchoolManager : Singleton<FishSchoolManager>
    {
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();
        private ConcurrentDictionary<uint, List<Doodad>> FishSchools { get; set; } = new();

        private const int CheckDelay = 60; // Check interval in seconds
        private const int CacheUpdateInterval = 60; // Cache update interval in seconds

        // Fish school cache to reduce database queries
        private ConcurrentDictionary<uint, (List<Doodad> FishSchools, DateTime LastUpdate)> _cache = new();

        public void Initialize()
        {
            StartFishSchoolTick();
            Logger.Info("Initialising FishSchool Manager...");
        }

        public void Load(uint worldId)
        {
            var fishSchools = GetFishSchoolsFromWorld(worldId);
            if (fishSchools.Any())
            {
                FishSchools.AddOrUpdate(worldId, fishSchools, (key, existingList) =>
                {
                    existingList.AddRange(fishSchools);
                    return existingList;
                });
            }
            Logger.Info($"Loaded {fishSchools.Count} FishSchool for worldId={worldId}...");
            InvalidateCache(worldId);
        }

        public List<Doodad> GetAllFishSchools(uint worldId)
        {
            if (_cache.TryGetValue(worldId, out var cacheEntry) && (DateTime.UtcNow - cacheEntry.LastUpdate).TotalSeconds < CacheUpdateInterval)
            {
                return cacheEntry.FishSchools;
            }

            var res = GetFishSchoolsFromWorld(worldId);
            FishSchools[worldId] = res;
            _cache[worldId] = (res, DateTime.UtcNow);

            return res;
        }

        private List<Doodad> GetFishSchoolsFromWorld(uint worldId)
        {
            var allDoodads = WorldManager.Instance.GetAllDoodads();
            return allDoodads
                .Where(d => d.TemplateId is DoodadConstants.FreshwaterFishSchool or DoodadConstants.SaltwaterFishSchool && d.Transform.WorldId == worldId)
                .ToList();
        }

        private bool IsFishSchoolDepleted(Doodad fishSchool, uint worldId)
        {
            if (fishSchool?.Transform == null)
                return true;

            // Optimization: use cached state to avoid unnecessary queries
            var fishSchools = GetAllFishSchools(worldId);
            return !fishSchools.Any(d => d.TemplateId == fishSchool.TemplateId && d.Transform.WorldId == fishSchool.Transform.WorldId);
        }

        private void StartFishSchoolTick()
        {
            TickManager.Instance.OnTick.Subscribe(FishSchoolTick, TimeSpan.FromSeconds(CheckDelay), true);
        }

        private async void FishSchoolTick(TimeSpan delta)
        {
            try
            {
                var worldIds = FishSchools.Keys.ToList();

                // Process each worldId
                var tasks = worldIds.Select(async worldId =>
                {
                    // Get current state once
                    var currentFishSchools = await Task.Run(() => GetAllFishSchools(worldId));

                    // Find deprecated items
                    var depletedFishSchools = currentFishSchools
                        .Where(fishSchool => IsFishSchoolDepleted(fishSchool, worldId))
                        .ToList();

                    // Get new fish school state
                    var updatedFishSchools = await Task.Run(() => GetFishSchoolsFromWorld(worldId));

                    // Find new items that didn't exist before
                    var newFishSchools = updatedFishSchools.Except(currentFishSchools).ToList();

                    if (depletedFishSchools.Any() || newFishSchools.Any())
                    {
                        // Update fish school list for the target world
                        FishSchools.AddOrUpdate(worldId, updatedFishSchools, (key, existingList) =>
                        {
                            var combined = existingList.Except(depletedFishSchools).ToList();
                            combined.AddRange(newFishSchools);
                            return combined;
                        });

                        // Reset cache for new state
                        InvalidateCache(worldId);
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred during FishSchoolTick execution");
            }
        }

        private void InvalidateCache(uint worldId)
        {
            // Update cache modification time to force refresh on next access
            if (_cache.ContainsKey(worldId))
            {
                _cache[worldId] = (_cache[worldId].FishSchools, DateTime.MinValue);
            }
        }
    }
}
