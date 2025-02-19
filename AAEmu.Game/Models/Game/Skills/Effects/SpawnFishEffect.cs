using System;

using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Packets;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.DoodadObj.Funcs;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects;

public class SpawnFishEffect : EffectTemplate
{
    public uint Range { get; set; }
    public uint DoodadId { get; set; }

    public override bool OnActionTime => false;

    public override void Apply(BaseUnit caster, SkillCaster casterObj, BaseUnit target, SkillCastTarget targetObj,
        CastAction castObj, EffectSource source, SkillObject skillObject, DateTime time,
        CompressedGamePackets packetBuilder = null)
    {
        Logger.Info($"SpawnFishEffect:");
        if (caster is Character player)
        {
            var fishSpawnerId = GetFishSpawnerId(player);
            if (fishSpawnerId == 0)
            {
                Logger.Info($"Fish Spawner ID not found.");
                return;
            }
            else
            {
                //Process: Spawn the fish, add it to the world at the correct location, then: combat engaged, target, aggro target before starting fish AI.
                Logger.Info($"Fish Spawner ID: {fishSpawnerId}");
                fishSpawnerId = 14475; // карп - подменяем для теста
                //We need to get the spawner at the target location.
                var npcSpawnerNpc = NpcGameData.Instance.GetNpcSpawnerNpc(fishSpawnerId);
                if (npcSpawnerNpc != null)
                {
                    var unitId = npcSpawnerNpc.MemberId;
                    var spawnerId = 0;
                    var spawner = SpawnManager.Instance.GetNpcSpawner(fishSpawnerId, (byte)caster.Transform.WorldId);
                    Logger.Info($"Returned {spawner.Count} Fish Spawners matching ID: {fishSpawnerId}");
                    spawnerId = spawner.Count;
                    Logger.Info($"No existing spawners, spawning one.");
                    spawner.Add(new NpcSpawner());
                    spawner[spawnerId].UnitId = unitId;
                    spawner[spawnerId].Id = fishSpawnerId;
                    spawner[spawnerId].NpcSpawnerIds = [fishSpawnerId];
                    spawner[spawnerId].Template = NpcGameData.Instance.GetNpcSpawnerTemplate(fishSpawnerId);
                    if (spawner[spawnerId].Template == null) { return; }
                    if (spawner[spawnerId].Template.Npcs.Count == 0)
                    {
                        spawner[spawnerId].Template.Npcs = [];
                        var nsn = NpcGameData.Instance.GetNpcSpawnerNpc(fishSpawnerId);
                        if (nsn == null) { return; }
                        spawner[spawnerId].Template.Npcs.Add(nsn);
                    }
                    if (spawner[spawnerId].Template.Npcs == null) { return; }
                    spawner[spawnerId].Template.Npcs[0].MemberId = unitId;
                    spawner[spawnerId].Template.Npcs[0].UnitId = unitId;
                    using var spawnPos = target.Transform.Clone();
                    spawner[spawnerId].Position = spawnPos.CloneAsSpawnPosition();
                    // Spawn the NPC
                    var fish = spawner[spawnerId].DoRandomSpawn(fishSpawnerId);
                    Logger.Info($"Fish spawn triggered");
                    if (fish != null) // Ensure the fish spawned
                    {
                        fish.OwnerId = player.ObjId;
                        Logger.Info($"Fish targeting: {fish}");
                        player.CurrentTarget = fish;
                        player.SendPacket(new SCUnitStatePacket(fish));
                        fish.AddUnitAggro(AggroKind.Damage, player, 1);
                        fish.Ai.OnAggroTargetChanged();
                        fish.Buffs.RemoveBuff(1);
                        player.BroadcastPacket(new SCTargetChangedPacket(player.ObjId, fish.ObjId), true);
                        fish.UseSkill(21608, player);
                    }
                    else
                    {
                        Logger.Info("No fish to target.");
                    }
                }
                else
                {
                    Logger.Warn($"No fish with spawnerId={fishSpawnerId}.");
                }
            }
        }
    }
    public uint GetFishSpawnerId(Character player)
    {
        var doodads = WorldManager.GetAround<Doodad>(player, 100);
        for (var i = 0; i < doodads.Count; i++)
        {
            if (doodads[i].Template.GroupId == 65)
            {
                foreach (var func in doodads[i].CurrentPhaseFuncs)
                {
                    var template = DoodadManager.Instance.GetPhaseFuncTemplate(func.FuncId, func.FuncType);
                    if (template is DoodadFuncFishSchool doodadFuncFishSchoolTemplate)
                    {
                        return doodadFuncFishSchoolTemplate.NpcSpawnerId;
                    }
                }
            }
        }
        return 0; // not found
    }
}
