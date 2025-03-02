﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers.Id;
using AAEmu.Game.Models.Game.Faction;
using AAEmu.Game.Models.Game.Gimmicks;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Utils.DB;

using NLog;

using static System.String;

namespace AAEmu.Game.Core.Managers;

public class GimmickManager : Singleton<GimmickManager>
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();
    private bool _loaded = false;

    private Dictionary<uint, GimmickTemplate> _templates;
    private Dictionary<uint, Gimmick> _activeGimmicks;
    private const double Delay = 50;
    //private const double DelayInit = 1;
    //private Task GimmickTickTask { get; set; }
    //private DateTime LastCheck { get; set; } = DateTime.MinValue;

    public bool Exist(uint templateId)
    {
        return _templates.ContainsKey(templateId);
    }

    public GimmickTemplate GetGimmickTemplate(uint id)
    {
        return _templates.GetValueOrDefault(id);
    }

    /// <summary>
    /// Create for spawning elevators
    /// </summary>
    /// <param name="objectId"></param>
    /// <param name="templateId"></param>
    /// <param name="spawner"></param>
    /// <returns></returns>
    public Gimmick Create(uint objectId, uint templateId, GimmickSpawner spawner)
    {
        /*
         * for elevators: templateId=0 and Template=null, but EntityGuid is used
         */

        var gimmick = new Gimmick();
        if (templateId != 0)
        {
            var template = _templates[templateId];
            //var template = GetGimmickTemplate(templateId);
            if (template == null) { return null; }
            gimmick.Template = template;
            gimmick.ModelPath = template.ModelPath;
            gimmick.EntityGuid = 0;
        }
        else
        {
            gimmick.Template = null;
            gimmick.ModelPath = Empty;
            gimmick.EntityGuid = spawner.EntityGuid;
        }

        gimmick.ObjId = objectId > 0 ? objectId : ObjectIdManager.Instance.GetNextId();
        gimmick.GimmickId = (ushort)GimmickIdManager.Instance.GetNextId();
        gimmick.Spawner = spawner;
        gimmick.TemplateId = templateId;
        gimmick.Faction = new SystemFaction();
        gimmick.Transform.ApplyWorldSpawnPosition(spawner.Position);
        gimmick.Vel = new Vector3(0f, 0f, 0f);
        var spawnRotation = new Quaternion(spawner.RotationX, spawner.RotationY, spawner.RotationZ, spawner.RotationW);
        // Apply Gimmick setting's rotation to the GameObject.Transform
        gimmick.Transform.Local.ApplyFromQuaternion(spawnRotation);
        gimmick.ModelParams = new UnitCustomModelParams();
        gimmick.SetScale(spawner.Scale);

        if (gimmick.Transform.World.IsOrigin())
        {
            Logger.Error($"Can't spawn gimmick {templateId}");
            return null;
        }

        gimmick.Spawn(); // adding to the world
        AddActiveGimmick(gimmick);

        return gimmick;
    }

    public void AddActiveGimmick(Gimmick gimmick)
    {
        // Attach movement handlers based on settings
        if ((gimmick.TemplateId == 0) && (gimmick.EntityGuid > 0))
        {
            // Elevators defined in gimmick_spawns.json
            gimmick.MovementHandler = new GimmickMovementElevator(gimmick);
        }
        else
        // TODO: Add decent Physics system to handle movement
        if (gimmick.TemplateId == 37)
        {
            // Recovered Treasure Chest
            gimmick.MovementHandler = new GimmickMovementFloatToSurface(gimmick);
        }

        gimmick.Time = (uint)(DateTime.UtcNow - DateTime.UtcNow.Date).TotalMilliseconds;
        _activeGimmicks.TryAdd(gimmick.ObjId, gimmick);
    }

    public void RemoveActiveGimmick(Gimmick gimmick)
    {
        _activeGimmicks.Remove(gimmick.ObjId);
    }

    /// <summary>
    /// Create for spawning projectiles
    /// </summary>
    /// <param name="templateId"></param>
    /// <returns></returns>
    public Gimmick Create(uint templateId)
    {
        var template = _templates[templateId];
        if (template == null) { return null; }

        var gimmick = new Gimmick();
        gimmick.ObjId = ObjectIdManager.Instance.GetNextId();
        gimmick.GimmickId = (ushort)GimmickIdManager.Instance.GetNextId();
        gimmick.Spawner = new GimmickSpawner();
        gimmick.Template = template;
        gimmick.TemplateId = template.Id;
        gimmick.Faction = new SystemFaction();
        gimmick.ModelPath = template.ModelPath;
        gimmick.ModelParams = new UnitCustomModelParams();

        return gimmick;
    }

    public void Load()
    {
        if (_loaded)
            return;

        _templates = [];
        _activeGimmicks = [];

        Logger.Info("Loading gimmick templates...");

        #region SQLLite
        using (var connection = SQLite.CreateConnection())
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM gimmicks";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new GimmickTemplate();

                        template.Id = reader.GetUInt32("id"); // GimmickId
                        template.AirResistance = reader.GetFloat("air_resistance");
                        template.CollisionMinSpeed = reader.GetFloat("collision_min_speed");
                        //template.CollisionSkillId = reader.GetUInt32("collision_skill_id");
                        //template.CollisionSkillId = reader.IsDBNull("collision_skill_id") ? 0 : reader.GetUInt32("collision_skill_id");
                        template.CollisionSkillId = reader.GetUInt32("collision_skill_id", 0);

                        template.CollisionUnitOnly = reader.GetBoolean("collision_unit_only");
                        template.Damping = reader.GetFloat("damping");
                        template.Density = reader.GetFloat("density");
                        template.DisappearByCollision = reader.GetBoolean("disappear_by_collision");
                        template.FadeInDuration = reader.GetUInt32("fade_in_duration");
                        template.FadeOutDuration = reader.GetUInt32("fade_out_duration");
                        template.FreeFallDamping = reader.GetFloat("free_fall_damping");
                        template.Graspable = reader.GetBoolean("graspable");
                        template.Gravity = reader.GetFloat("gravity");
                        template.LifeTime = reader.GetUInt32("life_time");
                        template.Mass = reader.GetFloat("mass");
                        template.ModelPath = reader.GetString("model_path");
                        template.Name = reader.GetString("name");
                        template.NoGroundCollider = reader.GetBoolean("no_ground_collider");
                        template.PushableByPlayer = reader.GetBoolean("pushable_by_player");
                        template.SkillDelay = reader.GetUInt32("skill_delay");
                        //template.SkillId = reader.GetUInt32("skill_id");
                        //template.CollisionSkillId = reader.IsDBNull("skill_id") ? 0 : reader.GetUInt32("skill_id");
                        template.SkillId = reader.GetUInt32("skill_id", 0);

                        template.SpawnDelay = reader.GetUInt32("spawn_delay");
                        template.WaterDamping = reader.GetFloat("water_damping");
                        template.WaterDensity = reader.GetFloat("water_density");
                        template.WaterResistance = reader.GetFloat("water_resistance");

                        _templates.Add(template.Id, template);
                    }
                }
            }
        }
        #endregion

        _loaded = true;
    }

    public void Initialize()
    {
        Logger.Info("GimmickTickTask: Started");
        TickManager.Instance.OnTick.Subscribe(GimmickTick, TimeSpan.FromMilliseconds(Delay), true);
    }

    /// <summary>
    /// Callback function for global gimmick ticks
    /// </summary>
    /// <param name="delta"></param>
    private void GimmickTick(TimeSpan delta)
    {
        var activeGimmicks = GetActiveGimmicks();
        foreach (var gimmick in activeGimmicks)
        {
            gimmick.GimmickTick(delta);
        }
    }

    private Gimmick[] GetActiveGimmicks()
    {
        lock (_activeGimmicks)
        {
            return _activeGimmicks.Values.ToArray();
        }
    }
}
