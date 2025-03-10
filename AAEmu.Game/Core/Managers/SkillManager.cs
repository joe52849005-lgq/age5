﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using AAEmu.Commons.Utils;
using AAEmu.Game.Models.Game.AI.Enums;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Details;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Buffs;
using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Models.Game.Skills.Effects.Enums;
using AAEmu.Game.Models.Game.Skills.Static;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.World;
using AAEmu.Game.Models.StaticValues;
using AAEmu.Game.Utils.DB;

using Microsoft.Data.Sqlite;

using Newtonsoft.Json.Linq;

using NLog;

namespace AAEmu.Game.Core.Managers;

public class SkillManager : Singleton<SkillManager>, ISkillManager
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();
    private bool _loaded = false;

    private Dictionary<uint, SkillTemplate> _skills;
    private Dictionary<uint, DefaultSkill> _defaultSkills;
    private Dictionary<int, List<CharacterDefaultSkill>> _characterDefaultSkills;
    private List<uint> _commonSkills;
    private Dictionary<AbilityType, List<SkillTemplate>> _startAbilitySkills;
    private Dictionary<uint, PassiveBuffTemplate> _passiveBuffs;
    private Dictionary<int, BuffPassiveBuffTemplate> _buffPassiveBuffs;
    private Dictionary<uint, EffectType> _types;
    private Dictionary<string, Dictionary<uint, EffectTemplate>> _effects;
    private Dictionary<uint, BuffTemplate> _buffs;
    private Dictionary<uint, List<uint>> _buffTags;
    private Dictionary<uint, List<uint>> _taggedBuffs;
    private Dictionary<uint, List<uint>> _skillTags;
    private Dictionary<uint, List<uint>> _taggedSkills;
    private Dictionary<uint, List<SkillModifier>> _skillModifiers;
    private Dictionary<uint, List<BuffTriggerTemplate>> _buffTriggers;
    private Dictionary<uint, List<CombatBuffTemplate>> _combatBuffs;
    private Dictionary<uint, SkillReagent> _skillReagents;
    private Dictionary<uint, SkillProduct> _skillProducts;
    private DynamicEffects _dynamicEffects;

    // private HashSet<ushort> _skillIds = new();
    // private ushort _skillIdIndex = 1;

    //Events
    public event EventHandler OnSkillsLoaded;

    /*
    // Replaced with SkillTlIdManager 
    public ushort NextId()
    {
        lock (_skillIds)
        {
            var id = _skillIdIndex;
            while (_skillIds.Contains(id))
            {
                if (id == ushort.MaxValue)
                    id = 1;
                else
                    id++;
            }
            _skillIds.Add(id);
            _skillIdIndex = (ushort)(id + 1u);
            if (_skillIdIndex == 0)
                _skillIdIndex = 1;
            return id;
        }
    }

    public void ReleaseId(ushort id)
    {
        lock (_skillIds)
        {
            _skillIds.Remove(id);
        }
    }
    */

    public SkillTemplate GetSkillTemplate(uint id)
    {
        if (_skills.TryGetValue(id, out var template))
            return template;
        return null;
    }

    public bool IsDefaultSkill(uint id)
    {
        return _defaultSkills.ContainsKey(id);
    }

    public bool IsCommonSkill(uint id)
    {
        return _commonSkills.Contains(id);
    }

    public List<SkillTemplate> GetStartAbilitySkills(AbilityType ability)
    {
        return _startAbilitySkills[ability];
    }

    public List<DefaultSkill> GetDefaultSkills()
    {
        return [.. _defaultSkills.Values];
    }

    public BuffTemplate GetBuffTemplate(uint id)
    {
        // if(_effects["Buff"].ContainsKey(id))
        //     return (BuffTemplate)_effects["Buff"][id];
        // return null;
        if (_buffs.TryGetValue(id, out var template))
            return template;
        return null;
    }

    public List<BuffTriggerTemplate> GetBuffTriggerTemplates(uint buffId)
    {
        if (_buffTriggers.TryGetValue(buffId, out var triggers))
        {
            return triggers;
        }
        return [];
    }

    public EffectTemplate GetEffectTemplate(uint id)
    {
        if (_types.ContainsKey(id))
        {
            var type = _types[id];

            Logger.Trace($"Get Effect Template: type = {type.Type}, id = {type.ActualId}");

            if (_effects.TryGetValue(type.Type, out var effect))
            {
                if (effect.TryGetValue(type.ActualId, out var res))
                {
                    return res;
                }
            }
            else
            {
                Logger.Trace($"No such Effect type = {type.Type}, id = {id}");
                return null;
            }
        }
        Logger.Trace($"No such Effect id = {id}");
        return null;
    }

    public EffectTemplate GetEffectTemplate(uint id, string type)
    {
        Logger.Trace("Get Effect Template: type = {0}, id = {1}", type, id);

        if (_effects.TryGetValue(type, out var value))
        {
            if (value.TryGetValue(id, out var res))
            {
                return res;
            }
        }
        return null;
    }

    public List<uint> GetBuffTags(uint buffId)
    {
        if (_buffTags.TryGetValue(buffId, out var tags))
            return tags;
        return [];
    }

    public List<uint> GetBuffsByTagId(uint tagId)
    {
        if (_taggedBuffs.TryGetValue(tagId, out var id))
            return id;
        return null;
    }

    public List<uint> GetSkillTags(uint skillId)
    {
        if (_skillTags.TryGetValue(skillId, out var tags))
            return tags;
        return [];
    }

    public List<uint> GetSkillsByTag(uint tagId)
    {
        if (_taggedSkills.TryGetValue(tagId, out var tag))
            return tag;
        return [];
    }

    public PassiveBuffTemplate GetPassiveBuffTemplate(uint id)
    {
        return _passiveBuffs.GetValueOrDefault(id);
    }

    public SelectiveItems GetSelectiveItems(uint skillId)
    {
        return _dynamicEffects.selectiveItems.GetValueOrDefault(skillId);
    }
    public BlessUthstin GetBlessUthstinItems(uint skillId)
    {
        return _dynamicEffects.blessUthstins.GetValueOrDefault(skillId);
    }

    public List<SkillModifier> GetModifiersByOwnerId(uint id)
    {
        if (_skillModifiers.TryGetValue(id, out var ownerId))
            return ownerId;
        return [];
    }

    public List<CombatBuffTemplate> GetCombatBuffs(uint reqBuffId)
    {
        if (_combatBuffs.TryGetValue(reqBuffId, out var buffs))
            return buffs;
        return [];
    }

    public List<SkillReagent> GetSkillReagentsBySkillId(uint id)
    {
        var reagents = new List<SkillReagent>();

        foreach (var reagent in _skillReagents)
        {
            if (reagent.Value.SkillId == id)
                reagents.Add(reagent.Value);
        }

        return reagents;
    }

    public List<SkillProduct> GetSkillProductsBySkillId(uint id)
    {
        var products = new List<SkillProduct>();

        foreach (var product in _skillProducts)
        {
            if (product.Value.SkillId == id)
                products.Add(product.Value);
        }

        return products;
    }

    /// <summary>
    /// Returns a skill to be used for npcs with np_skills.
    /// </summary>
    /// <param name="npcSkill"></param>
    /// <returns></returns>
    public Skill GetNpSkillTemplate(NpcSkill npcSkill)
    {
        var skillTemplate = GetSkillTemplate(npcSkill.SkillId);

        // Temporary condition to filter for dungeon OnCombat skills.
        if (npcSkill.SkillUseCondition == SkillUseConditionKind.InCombat)
        {
            if (skillTemplate.IgnoreGlobalCooldown == false || skillTemplate.Plot is not null)
            {
                return null;
            }
        }

        return skillTemplate != null ? new Skill(skillTemplate) : null;
    }

    public void Load()
    {
        if (_loaded)
            return;

        _skills = new Dictionary<uint, SkillTemplate>();
        _defaultSkills = new Dictionary<uint, DefaultSkill>();
        _characterDefaultSkills = new Dictionary<int, List<CharacterDefaultSkill>>();
        _commonSkills = [];
        _startAbilitySkills = new Dictionary<AbilityType, List<SkillTemplate>>();
        _passiveBuffs = new Dictionary<uint, PassiveBuffTemplate>();
        _buffPassiveBuffs = new Dictionary<int, BuffPassiveBuffTemplate>();
        _types = new Dictionary<uint, EffectType>();
        _effects = new Dictionary<string, Dictionary<uint, EffectTemplate>>();
        _dynamicEffects = new DynamicEffects();
        _effects.Add("Buff", new Dictionary<uint, EffectTemplate>()); // missing from the effect table
        _effects.Add("AcceptQuestEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("AccountAttributeEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("AggroEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("BubbleEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("BuffEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("CinemaEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("CleanupUccEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("ConversionEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("CraftEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("DamageEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("DispelEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("FlyingStateChangeEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("GainLootPackItemEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("HealEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("ImprintUccEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("ImpulseEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("InteractionEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("KillNpcWithoutCorpseEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("ManaBurnEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("MoveToRezPointEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("NpcControlEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("NpcSpawnerDespawnEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("NpcSpawnerSpawnEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("OpenPortalEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("PhysicalExplosionEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("PutDownBackpackEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("RecoverExpEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("RepairSlaveEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("ReportCrimeEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("RestoreManaEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("ScopedFEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("SpawnEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("SpawnGimmickEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("SpecialEffect", new Dictionary<uint, EffectTemplate>());
        _effects.Add("TrainCraftEffect", new Dictionary<uint, EffectTemplate>());

        _effects.Add("TrainCraftRankEffect", new Dictionary<uint, EffectTemplate>()); // missing from the effect table
        _effects.Add("SkillController", new Dictionary<uint, EffectTemplate>()); // missing from the effect table
        _effects.Add("SpawnFishEffect", new Dictionary<uint, EffectTemplate>()); // missing from the effect table
        _effects.Add("ResetAoeDiminishingEffect", new Dictionary<uint, EffectTemplate>()); // missing from the effect table
        _effects.Add("MoveToLocationEffect", new Dictionary<uint, EffectTemplate>()); // missing from the effect table

        _buffs = new Dictionary<uint, BuffTemplate>();
        // TODO 
        /*
            _effects.Add("PlayLogEffect", new Dictionary<uint, EffectTemplate>()); // missing from the effect table
        */

        _buffTags = new Dictionary<uint, List<uint>>();
        _taggedBuffs = new Dictionary<uint, List<uint>>();
        _skillModifiers = new Dictionary<uint, List<SkillModifier>>();
        _skillTags = new Dictionary<uint, List<uint>>();
        _taggedSkills = new Dictionary<uint, List<uint>>();
        _combatBuffs = new Dictionary<uint, List<CombatBuffTemplate>>();
        _skillReagents = new Dictionary<uint, SkillReagent>();
        _skillProducts = new Dictionary<uint, SkillProduct>();

        using (var connection2 = SQLite.CreateConnection("Data", "compact.server.table.sqlite3"))
        using (var connection = SQLite.CreateConnection())
        {
            Logger.Info("Loading skills...");
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM skills";
                command.Prepare();
                using (var sqliteReader = command.ExecuteReader())
                using (var reader = new SQLiteWrapperReader(sqliteReader))
                {
                    while (reader.Read())
                    {
                        var template = new SkillTemplate();
                        template.Id = reader.GetUInt32("id");
                        template.Cost = reader.GetInt32("cost");
                        template.Show = reader.GetBoolean("show", true);
                        template.FireAnim = AnimationManager.Instance.GetAnimation(reader.GetUInt32("fire_anim_id", 0));
                        template.AbilityId = (AbilityType)reader.GetByte("ability_id");
                        template.ManaCost = reader.GetInt32("mana_cost");
                        template.TimingId = reader.GetInt32("timing_id");
                        template.CooldownTime = reader.GetUInt32("cooldown_time");
                        template.CastingTime = reader.GetInt32("casting_time");
                        template.IgnoreGlobalCooldown = reader.GetBoolean("ignore_global_cooldown", true);
                        template.IsDropableBackpack = reader.GetBoolean("is_dropable_backpack");
                        template.EffectDelay = reader.GetInt32("effect_delay");
                        template.EffectSpeed = reader.GetFloat("effect_speed");
                        template.EffectRepeatCount = reader.GetInt32("effect_repeat_count");
                        template.EffectRepeatTick = reader.GetInt32("effect_repeat_tick");
                        template.ActiveWeaponId = reader.GetInt32("active_weapon_id");
                        template.TargetType = (SkillTargetType)reader.GetInt32("target_type_id");
                        template.TargetSelection = (SkillTargetSelection)reader.GetInt32("target_selection_id");
                        template.TargetRelation = (SkillTargetRelation)reader.GetInt32("target_relation_id");
                        template.TargetAreaCount = reader.GetInt32("target_area_count");
                        template.TargetAreaRadius = reader.GetInt32("target_area_radius");
                        //template.TargetSiege = reader.GetBoolean("target_siege", true);
                        template.WeaponSlotForAngleId = reader.GetInt32("weapon_slot_for_angle_id");
                        template.TargetAngle = reader.GetInt32("target_angle");
                        template.WeaponSlotForRangeId = reader.GetInt32("weapon_slot_for_range_id");
                        template.WeaponSlotForAutoAttackId = reader.GetInt32("weapon_slot_for_autoattack_id");
                        template.MinRange = reader.GetInt32("min_range");
                        template.MaxRange = reader.GetInt32("max_range");
                        template.KeepStealth = reader.GetBoolean("keep_stealth", true);
                        template.Aggro = reader.GetInt32("aggro");
                        template.ChannelingTime = reader.GetInt32("channeling_time");
                        template.ChannelingTick = reader.GetInt32("channeling_tick");
                        template.ChannelingMana = reader.GetInt32("channeling_mana");
                        template.ChannelingTargetBuffId = reader.GetUInt32("channeling_target_buff_id", 0);
                        template.TargetAreaAngle = reader.GetInt32("target_area_angle");
                        template.AbilityLevel = reader.GetInt32("ability_level");
                        template.ChannelingDoodadId = reader.GetUInt32("channeling_doodad_id", 0);
                        var value = reader.GetString("cooldown_tag_id", "0");
                        template.CooldownTagId = value.Contains("null") ? 0 : int.Parse(value);
                        value = reader.GetString("skill_controller_id", "0");
                        template.SkillControllerId = value.Contains("null") ? 0 : uint.Parse(value);
                        template.RepeatCount = reader.GetInt32("repeat_count");
                        template.RepeatTick = reader.GetInt32("repeat_tick");
                        template.ReqPoints = reader.GetInt32("req_points");
                        template.ToggleBuffId = reader.GetUInt32("toggle_buff_id", 0);
                        template.TargetDead = reader.GetBoolean("target_dead", true);
                        template.ChannelingBuffId = reader.GetUInt32("channeling_buff_id", 0);
                        template.ReagentCorpseStatusId = reader.GetInt32("reagent_corpse_status_id");
                        template.SourceDead = reader.GetBoolean("source_dead", true);
                        template.LevelStep = reader.GetInt32("level_step");
                        template.ValidHeight = reader.GetFloat("valid_height");
                        template.TargetValidHeight = reader.GetFloat("target_valid_height");
                        template.SourceMount = reader.GetBoolean("source_mount", true);
                        template.StopCastingOnBigHit = reader.GetBoolean("stop_casting_on_big_hit", true);
                        template.StopChannelingOnBigHit = reader.GetBoolean("stop_channeling_on_big_hit", true);
                        template.AutoLearn = reader.GetBoolean("auto_learn", true);
                        //template.NeedLearn = reader.GetBoolean("need_learn", true); // there is no such field in the database for version 3.0.3.0
                        template.MainhandToolId = reader.GetUInt32("mainhand_tool_id", 0);
                        template.OffhandToolId = reader.GetUInt32("offhand_tool_id", 0);
                        template.FrontAngle = reader.GetInt32("front_angle");
                        template.ManaLevelMd = reader.GetFloat("mana_level_md");
                        template.Unmount = reader.GetBoolean("unmount", true);
                        template.DamageTypeId = reader.GetUInt32("damage_type_id", 0);
                        template.AllowToPrisoner = reader.GetBoolean("allow_to_prisoner", true);
                        //template.MilestoneId = reader.GetUInt32("milestone_id", 0); // there is no such field in the database for version 3.0.3.0
                        template.MatchAnimation = reader.GetBoolean("match_animation", true);
                        template.Plot = reader.IsDBNull("plot_id") ? null : PlotManager.Instance.GetPlot(reader.GetUInt32("plot_id"));
                        template.UseAnimTime = reader.GetBoolean("use_anim_time", true);
                        template.ConsumeLaborPower = reader.GetInt32("consume_lp", 0);
                        template.SourceStun = reader.GetBoolean("source_stun", true);
                        template.TargetAlive = reader.GetBoolean("target_alive", true);
                        template.TargetWater = reader.GetBoolean("target_water", true);
                        template.CastingInc = reader.GetInt32("casting_inc");
                        template.CastingCancelable = reader.GetBoolean("casting_cancelable", true);
                        template.CastingDelayable = reader.GetBoolean("casting_delayable", true);
                        template.ChannelingCancelable = reader.GetBoolean("channeling_cancelable", true);
                        template.TargetOffsetAngle = reader.GetFloat("target_offset_angle");
                        template.TargetOffsetDistance = reader.GetFloat("target_offset_distance");
                        template.ActabilityGroupId = reader.GetInt32("actability_group_id", 0);
                        template.PlotOnly = reader.GetBoolean("plot_only", true);
                        template.SkillControllerAtEnd = reader.GetBoolean("skill_controller_at_end", true);
                        template.EndSkillController = reader.GetBoolean("end_skill_controller", true);
                        template.OrUnitReqs = reader.GetBoolean("or_unit_reqs", true);
                        template.DefaultGcd = reader.GetBoolean("default_gcd", true);
                        template.KeepManaRegen = reader.GetBoolean("keep_mana_regen", true);
                        template.CrimePoint = reader.GetInt32("crime_point");
                        template.LevelRuleNoConsideration = reader.GetBoolean("level_rule_no_consideration", true);
                        template.UseWeaponCooldownTime = reader.GetBoolean("use_weapon_cooldown_time", true);
                        template.CombatDiceId = reader.GetInt32("combat_dice_id");
                        template.CustomGcd = reader.GetInt32("custom_gcd");
                        template.CancelOngoingBuffs = reader.GetBoolean("cancel_ongoing_buffs", true);
                        template.CancelOngoingBuffExceptionTagId = reader.GetUInt32("cancel_ongoing_buff_exception_tag_id", 0);
                        template.SourceCannotUseWhileWalk = reader.GetBoolean("source_cannot_use_while_walk", true);
                        template.SourceCrippled = reader.GetBoolean("source_crippled");
                        template.SourceMountMate = reader.GetBoolean("source_mount_mate", true);
                        template.CheckTerrain = reader.GetBoolean("check_terrain", true);
                        template.TargetOnlyWater = reader.GetBoolean("target_only_water", true);
                        template.SourceNotSwim = reader.GetBoolean("source_not_swim", true);
                        template.TargetPreoccupied = reader.GetBoolean("target_preoccupied", true);
                        template.StopChannelingOnStartSkill = reader.GetBoolean("stop_channeling_on_start_skill", true);
                        template.StopCastingByTurn = reader.GetBoolean("stop_casting_by_turn", true);
                        template.TargetMyNpc = reader.GetBoolean("target_my_npc", true);
                        template.GainLifePoint = reader.GetInt32("gain_life_point");
                        template.TargetFishing = reader.GetBoolean("target_fishing", true);
                        template.SourceNoSlave = reader.GetBoolean("source_no_slave", true);
                        template.AutoReUse = reader.GetBoolean("auto_reuse", true);
                        template.AutoReUseDelay = reader.GetInt32("auto_reuse_delay", 0);
                        template.SourceNotCollided = reader.GetBoolean("source_not_collided", true);
                        template.SkillPoints = reader.GetInt32("skill_points");
                        template.DoodadHitFamily = reader.GetInt32("doodad_hit_family");
                        template.FirstReagentOnly = reader.GetBoolean("first_reagent_only", true);
                        // added in 5.0.7.0
                        template.AccountCooldown = reader.GetBoolean("account_cooldown");
                        template.AutoFire = reader.GetBoolean("auto_fire");
                        template.AutoReuse = reader.GetBoolean("auto_reuse");
                        template.AutoReuseDelay = reader.GetInt32("auto_reuse_delay");
                        template.CalcUserLevel = reader.GetBoolean("calc_user_level");
                        template.CameraAcceleration = reader.GetFloat("camera_acceleration");
                        template.CameraDuration = reader.GetFloat("camera_duration");
                        template.CameraHoldZ = reader.GetBoolean("camera_hold_z");
                        template.CameraMaxDistance = reader.GetFloat("camera_max_distance");
                        template.CameraSlowDownDistance = reader.GetFloat("camera_slow_down_distance");
                        template.CameraSpeed = reader.GetFloat("camera_speed");
                        template.CanActiveWeaponWithoutAnim = reader.GetBoolean("can_active_weapon_without_anim");
                        template.CastingUseable = reader.GetBoolean("casting_useable");
                        template.CategoryId = reader.GetInt32("category_id");
                        template.ChannelingAnimId = reader.GetInt32("channeling_anim_id");
                        template.CharRaceId = reader.GetInt32("char_race_id");
                        template.ChargeCooldownTime = reader.GetInt32("charge_cooldown_time");
                        template.ChargeCount = reader.GetInt32("charge_count");
                        template.CheckObstacle = reader.GetBoolean("check_obstacle");
                        template.ConsumeLp = reader.GetInt32("consume_lp");
                        template.ControllerCamera = reader.GetBoolean("controller_camera");
                        template.ControllerCameraSpeed = reader.GetInt32("controller_camera_speed");
                        template.Desc = reader.GetString("desc");
                        template.DoodadBundleId = reader.GetInt32("doodad_bundle_id");
                        template.DualWieldFireAnimId = reader.GetInt32("dual_wield_fire_anim_id");
                        template.FxGroupId = reader.GetInt32("fx_group_id");
                        template.HighAbilityId = reader.GetInt32("high_ability_id");
                        template.IconId = reader.GetInt32("icon_id");
//                        template.LinkBackpackTypeId = reader.GetInt32("link_backpack_type_id");
                        template.LinkEquipSlotId = reader.GetInt32("link_equip_slot_id");
                        template.MatchAnimationCount = reader.GetBoolean("match_animation_count");
                        template.MaxHighAbilityResource = reader.GetInt32("max_high_ability_resource");
                        template.MinHighAbilityResource = reader.GetInt32("min_high_ability_resource");
                        template.Name = reader.GetString("name");
                        template.PercussionInstrumentFireAnimId = reader.GetInt32("percussion_instrument_fire_anim_id");
                        template.PercussionInstrumentStartAnimId = reader.GetInt32("percussion_instrument_start_anim_id");
                        template.PitchAngle = reader.GetFloat("pitch_angle");
                        template.PlotId = reader.GetInt32("plot_id");
                        template.PrecedenceSkillId = reader.GetInt32("precedence_skill_id");
                        template.ProjectileId = reader.GetInt32("projectile_id");
                        template.SecondCooldownTagId = reader.GetInt32("second_cooldown_tag_id");
                        template.SensitiveOperation = reader.GetBoolean("sensitive_operation");
                        template.ShowTargetCastingTime = reader.GetBoolean("show_target_casting_time");
                        template.SkipQuestApplyUseItem = reader.GetBoolean("skip_quest_apply_use_item");
                        template.SkipValidateSource = reader.GetBoolean("skip_validate_source");
                        template.SourceAlive = reader.GetBoolean("source_alive");
                        template.SourceShouldSwim = reader.GetBoolean("source_should_swim");
                        template.SourceSilence = reader.GetBoolean("source_silence");
                        template.SourceSleep = reader.GetBoolean("source_sleep");
                        template.StartAnimId = reader.GetInt32("start_anim_id");
                        template.StartAutoattack = reader.GetBoolean("start_autoattack");
                        template.StopAutoattack = reader.GetBoolean("stop_autoattack");
                        template.StringInstrumentFireAnimId = reader.GetInt32("string_instrument_fire_anim_id");
                        template.StringInstrumentStartAnimId = reader.GetInt32("string_instrument_start_anim_id");
                        template.SwitchToSkillCooldown = reader.GetBoolean("switch_to_skill_cooldown");
                        template.SynergyIcon1Buffkind = reader.GetBoolean("synergy_icon1_buffkind");
                        template.SynergyIcon1Id = reader.GetInt32("synergy_icon1_id");
                        template.SynergyIcon2Buffkind = reader.GetBoolean("synergy_icon2_buffkind");
                        template.SynergyIcon2Id = reader.GetInt32("synergy_icon2_id");
                        template.TargetDecalRadius = reader.GetInt32("target_decal_radius");
                        template.TargetRelationId = reader.GetInt32("target_relation_id");
                        template.TargetSelectionId = reader.GetInt32("target_selection_id");
                        template.TargetTypeId = reader.GetInt32("target_type_id");
                        template.ThirdCooldownTagId = reader.GetInt32("third_cooldown_tag_id");
                        template.TubeInstrumentFireAnimId = reader.GetInt32("tube_instrument_fire_anim_id");
                        template.TubeInstrumentStartAnimId = reader.GetInt32("tube_instrument_start_anim_id");
                        template.TwohandFireAnimId = reader.GetInt32("twohand_fire_anim_id");
                        template.UseSkillCamera = reader.GetBoolean("use_skill_camera");
                        template.ValidHeightEdgeToEdge = reader.GetBoolean("valid_height_edge_to_edge");
                        template.WeaponGcdId = reader.GetInt32("weapon_gcd_id");
                        template.WeaponSlotForAutoattackId = reader.GetInt32("weapon_slot_for_autoattack_id");

                        _skills.Add(template.Id, template);
                    }
                }
            }

            Logger.Info("Loaded {0} skills", _skills.Count);

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM default_skills";
                command.Prepare();
                using (var sqliteReader = command.ExecuteReader())
                using (var reader = new SQLiteWrapperReader(sqliteReader))
                {
                    while (reader.Read())
                    {
                        var id = (uint)reader.GetInt32("skill_id");
                        var skill = new DefaultSkill
                        {
                            Template = _skills[id],
                            Slot = reader.GetByte("slot_index"),
                            AddToSlot = reader.GetBoolean("add_to_slot", true),
                            // added in 5.0.7.0
                            SkillActiveTypeId = reader.GetInt32("skill_active_type_id"),
                            SkillBookCategoryId = reader.GetInt32("skill_book_category_id"),
                            SkillId = reader.GetInt32("skill_id"),
                        };
                        _defaultSkills.TryAdd(skill.Template.Id, skill);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM character_default_skills";
                command.Prepare();
                using (var sqliteReader = command.ExecuteReader())
                using (var reader = new SQLiteWrapperReader(sqliteReader))
                {
                    while (reader.Read())
                    {
                        var characterDefaultSkill = new CharacterDefaultSkill();
                        characterDefaultSkill.CharacterId = reader.GetInt32("character_id");
                        characterDefaultSkill.DefaultSkillId = reader.GetInt32("default_skill_id");

                        if (!_characterDefaultSkills.ContainsKey(characterDefaultSkill.CharacterId))
                            _characterDefaultSkills[characterDefaultSkill.CharacterId] = new List<CharacterDefaultSkill>();
                        _characterDefaultSkills[characterDefaultSkill.CharacterId].Add(characterDefaultSkill);
                    }
                }
            }

            Logger.Info("Loading skill effects/buffs...");

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM passive_buffs";
                command.Prepare();
                using (var sqliteReader = command.ExecuteReader())
                using (var reader = new SQLiteWrapperReader(sqliteReader))
                {
                    while (reader.Read())
                    {
                        var template = new PassiveBuffTemplate
                        {
                            Id = reader.GetUInt32("id"),
                            AbilityId = (AbilityType)reader.GetByte("ability_id"),
                            Level = reader.GetByte("level"),
                            BuffId = reader.GetUInt32("buff_id"),
                            ReqPoints = reader.GetInt32("req_points"),
                            Active = reader.GetBoolean("active", true),
                            // added in 5.0.7.0
                            HighAbilityId = reader.GetInt32("high_ability_id"),
                            SkillPoints = reader.GetInt32("skill_points")
                        };
                        _passiveBuffs.Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM buff_passive_buffs";
                command.Prepare();
                using (var sqliteReader = command.ExecuteReader())
                using (var reader = new SQLiteWrapperReader(sqliteReader))
                {
                    while (reader.Read())
                    {
                        var template = new BuffPassiveBuffTemplate
                        {
                            Id = reader.GetInt32("id"),
                            BuffId = reader.GetInt32("buff_id"),
                            PassiveBuffId = reader.GetInt32("passive_buff_id")
                        };
                        _buffPassiveBuffs.Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM buffs";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new BuffTemplate();
                        template.Id = reader.GetUInt32("id");
                        template.AnimStartId = reader.GetUInt32("anim_start_id", 0);
                        template.AnimEndId = reader.GetUInt32("anim_end_id", 0);
                        template.Duration = reader.GetInt32("duration");
                        template.Tick = reader.GetDouble("tick");
                        template.Silence = reader.GetBoolean("silence", true);
                        template.Root = reader.GetBoolean("root", true);
                        template.Sleep = reader.GetBoolean("sleep", true);
                        template.Stun = reader.GetBoolean("stun", true);
                        template.Cripled = reader.GetBoolean("crippled", true);
                        template.Stealth = reader.GetBoolean("stealth", true);
                        template.RemoveOnSourceDead = reader.GetBoolean("remove_on_source_dead", true);
                        template.LinkBuffId = reader.GetUInt32("link_buff_id", 0);
                        template.TickManaCost = reader.GetInt32("tick_mana_cost");
                        template.StackRule = (BuffStackRule)reader.GetUInt32("stack_rule_id");
                        template.InitMinCharge = reader.GetInt32("init_min_charge");
                        template.InitMaxCharge = reader.GetInt32("init_max_charge");
                        template.MaxStack = reader.GetInt32("max_stack");
                        template.DamageAbsorptionTypeId = reader.GetUInt32("damage_absorption_type_id");
                        template.DamageAbsorptionPerHit = reader.GetInt32("damage_absorption_per_hit");
                        template.AuraRadius = reader.GetInt32("aura_radius");
                        template.ManaShieldRatio = reader.GetInt32("mana_shield_ratio");
                        template.FrameHold = reader.GetBoolean("framehold", true);
                        template.Ragdoll = reader.GetBoolean("ragdoll", true);
                        template.OneTime = reader.GetBoolean("one_time", true);
                        template.ReflectionChance = reader.GetInt32("reflection_chance");
                        //template.ReflectionTypeId = reader.GetUInt32("reflection_type_id");
                        template.RequireBuffId = reader.GetUInt32("require_buff_id", 0);
                        template.Taunt = reader.GetBoolean("taunt", true);
                        template.TauntWithTopAggro = reader.GetBoolean("taunt_with_top_aggro", true);
                        template.RemoveOnUseSkill = reader.GetBoolean("remove_on_use_skill", true);
                        template.MeleeImmune = reader.GetBoolean("melee_immune", true);
                        template.SpellImmune = reader.GetBoolean("spell_immune", true);
                        template.RangedImmune = reader.GetBoolean("ranged_immune", true);
                        template.SiegeImmune = reader.GetBoolean("siege_immune", true);
                        template.ImmuneDamage = reader.GetInt32("immune_damage");
                        var value = reader.GetString("skill_controller_id", "0");
                        template.SkillControllerId = value.Contains("null") ? 0 : uint.Parse(value);
                        template.ResurrectionHealth = reader.GetInt32("resurrection_health");
                        template.ResurrectionMana = reader.GetInt32("resurrection_mana");
                        template.ResurrectionPercent = reader.GetBoolean("resurrection_percent", true);
                        template.LevelDuration = reader.GetInt32("level_duration");
                        template.ReflectionRatio = reader.GetInt32("reflection_ratio");
                        template.ReflectionTargetRatio = reader.GetInt32("reflection_target_ratio");
                        template.KnockbackImmune = reader.GetBoolean("knockback_immune");
                        //template.ImmuneBuffTagId = reader.GetUInt32("immune_buff_tag_id", 0); // there is no such field in the database for version 3.0.3.0
                        template.AuraRelationId = reader.GetUInt32("aura_relation_id");
                        template.GroupId = reader.GetUInt32("group_id", 0);
                        template.GroupRank = reader.GetInt32("group_rank");
                        template.PerUnitCreation = reader.GetBoolean("per_unit_creation");
                        template.TickAreaRadius = reader.GetFloat("tick_area_radius");
                        template.TickAreaRelationId = reader.GetUInt32("tick_area_relation_id");
                        template.RemoveOnMove = reader.GetBoolean("remove_on_move", true);
                        template.UseSourceFaction = reader.GetBoolean("use_source_faction", true);
                        template.FactionId = (FactionsEnum)reader.GetUInt32("faction_id", 0);
                        template.Exempt = reader.GetBoolean("exempt", true);
                        template.TickAreaFrontAngle = reader.GetInt32("tick_area_front_angle");
                        template.TickAreaAngle = reader.GetInt32("tick_area_angle");
                        template.Psychokinesis = reader.GetBoolean("psychokinesis", true);
                        template.NoCollide = reader.GetBoolean("no_collide", true);
                        template.PsychokinesisSpeed = reader.GetFloat("psychokinesis_speed");
                        template.RemoveOnDeath = reader.GetBoolean("remove_on_death", true);
                        template.TickAnimId = reader.GetUInt32("tick_anim_id", 0);
                        template.TickActiveWeaponId = reader.GetUInt32("tick_active_weapon_id");
                        template.ConditionalTick = reader.GetBoolean("conditional_tick", true);
                        template.System = reader.GetBoolean("system", true);
                        template.AuraSlaveBuffId = reader.GetUInt32("aura_slave_buff_id", 0);
                        template.NonPushable = reader.GetBoolean("non_pushable", true);
                        template.ActiveWeaponId = reader.GetUInt32("active_weapon_id");
                        template.MaxCharge = reader.GetInt32("max_charge");
                        template.DetectStealth = reader.GetBoolean("detect_stealth", true);
                        template.RemoveOnExempt = reader.GetBoolean("remove_on_exempt", true);
                        template.RemoveOnLand = reader.GetBoolean("remove_on_land", true);
                        template.Gliding = reader.GetBoolean("gliding", true);
                        template.GlidingRotateSpeed = reader.GetInt32("gliding_rotate_speed");
                        template.Knockdown = reader.GetBoolean("knock_down", true);
                        template.TickAreaExcludeSource = reader.GetBoolean("tick_area_exclude_source", true);
                        template.FallDamageImmune = reader.GetBoolean("fall_damage_immune", true);
                        template.Kind = (BuffKind)reader.GetInt32("kind_id");
                        template.TransformBuffId = reader.GetUInt32("transform_buff_id", 0);
                        template.BlankMinded = reader.GetBoolean("blank_minded", true);
                        template.Fastened = reader.GetBoolean("fastened", true);
                        template.SlaveApplicable = reader.GetBoolean("slave_applicable", true);
                        template.Pacifist = reader.GetBoolean("pacifist", true);
                        template.RemoveOnInteraction = reader.GetBoolean("remove_on_interaction", true);
                        //template.Crime = reader.GetBoolean("crime", true); // there is no such field in the database for version 3.0.3.0
                        template.RemoveOnUnmount = reader.GetBoolean("remove_on_unmount", true);
                        template.AuraChildOnly = reader.GetBoolean("aura_child_only", true);
                        template.RemoveOnMount = reader.GetBoolean("remove_on_mount", true);
                        template.RemoveOnStartSkill = reader.GetBoolean("remove_on_start_skill", true);
                        template.SprintMotion = reader.GetBoolean("sprint_motion", true);
                        template.TelescopeRange = reader.GetFloat("telescope_range");
                        value = reader.GetString("mainhand_tool_id", "0");
                        template.MainhandToolId = value.Length > 0 ? uint.Parse(value) : 0;
                        value = reader.GetString("offhand_tool_id", "0");
                        template.OffhandToolId = value.Length > 0 ? uint.Parse(value) : 0;
                        value = reader.GetString("tick_mainhand_tool_id", "0");
                        template.TickMainhandToolId = value.Length > 0 ? uint.Parse(value) : 0;
                        value = reader.GetString("tick_offhand_tool_id", "0");
                        template.TickOffhandToolId = value.Length > 0 ? uint.Parse(value) : 0;
                        template.TickLevelManaCost = reader.GetFloat("tick_level_mana_cost");
                        template.WalkOnly = reader.GetBoolean("walk_only", true);
                        template.CannnotJump = reader.GetBoolean("cannot_jump", true);
                        template.CrowdBuffId = reader.GetUInt32("crowd_buff_id", 0);
                        template.CrowdRadius = reader.GetFloat("crowd_radius");
                        template.CrowdNumber = reader.GetInt32("crowd_number");
                        template.EvadeTelescope = reader.GetBoolean("evade_telescope", true);
                        template.TransferTelescopeRange = reader.GetFloat("transfer_telescope_range");
                        template.RemoveOnAttackSpellDot = reader.GetBoolean("remove_on_attack_spell_dot", true);
                        template.RemoveOnAttackEtcDot = reader.GetBoolean("remove_on_attack_etc_dot", true);
                        template.RemoveOnAttackBuffTrigger = reader.GetBoolean("remove_on_attack_buff_trigger", true);
                        template.RemoveOnAttackEtc = reader.GetBoolean("remove_on_attack_etc", true);
                        template.RemoveOnAttackedSpellDot = reader.GetBoolean("remove_on_attacked_spell_dot", true);
                        template.RemoveOnAttackedEtcDot = reader.GetBoolean("remove_on_attacked_etc_dot", true);
                        template.RemoveOnAttackedBuffTrigger = reader.GetBoolean("remove_on_attacked_buff_trigger", true);
                        template.RemoveOnAttackedEtc = reader.GetBoolean("remove_on_attacked_etc", true);
                        template.RemoveOnDamageSpellDot = reader.GetBoolean("remove_on_damage_spell_dot", true);
                        template.RemoveOnDamageEtcDot = reader.GetBoolean("remove_on_damage_etc_dot", true);
                        template.RemoveOnDamageBuffTrigger = reader.GetBoolean("remove_on_damage_buff_trigger", true);
                        template.RemoveOnDamageEtc = reader.GetBoolean("remove_on_damage_etc", true);
                        template.RemoveOnDamagedSpellDot = reader.GetBoolean("remove_on_damaged_spell_dot", true);
                        template.RemoveOnDamagedEtcDot = reader.GetBoolean("remove_on_damaged_etc_dot", true);
                        template.RemoveOnDamagedBuffTrigger = reader.GetBoolean("remove_on_damaged_buff_trigger", true);
                        template.RemoveOnDamagedEtc = reader.GetBoolean("remove_on_damaged_etc", true);
                        template.OwnerOnly = reader.GetBoolean("owner_only", true);
                        template.RemoveOnAutoAttack = reader.GetBoolean("remove_on_autoattack", true);
                        template.SaveRuleId = reader.GetUInt32("save_rule_id");
                        template.AntiStealth = reader.GetBoolean("anti_stealth", true);
                        template.Scale = reader.GetFloat("scale");
                        template.ScaleDuration = reader.GetFloat("scaleDuration");
                        template.ImmuneExceptCreator = reader.GetBoolean("immune_except_creator", true);
                        template.ImmuneExceptSkillTagId = reader.GetUInt32("immune_except_skill_tag_id", 0);
                        template.FindSchoolOfFishRange = reader.GetFloat("find_school_of_fish_range");
                        template.AnimActionId = reader.GetUInt32("anim_action_id", 0);
                        template.DeadApplicable = reader.GetBoolean("dead_applicable", true);
                        template.TickAreaUseOriginSource = reader.GetBoolean("tick_area_use_origin_source", true);
                        template.RealTime = reader.GetBoolean("real_time", true);
                        template.DoNotRemoveByOtherSkillController = reader.GetBoolean("do_not_remove_by_other_skill_controller", true);
                        template.CooldownSkillId = reader.GetUInt32("cooldown_skill_id", 0);
                        template.CooldownSkillTime = reader.GetInt32("cooldown_skill_time");
                        template.ManaBurnImmune = reader.GetBoolean("mana_burn_immune", true);
                        template.FreezeShip = reader.GetBoolean("freeze_ship", true);
                        template.CrowdFriendly = reader.GetBoolean("crowd_friendly", true);
                        template.CrowdHostile = reader.GetBoolean("crowd_hostile", true);
                        // added in 5.0.7.0
                        template.AddDurationBuffMul = reader.GetInt32("add_duration_buff_mul");
                        template.AddDurationBuffId = reader.GetInt32("add_duration_buff_id");
                        template.AgStance = reader.GetString("ag_stance");
                        template.AuraCreatorOnly = reader.GetBoolean("aura_creator_only");
                        template.BalanceLevel = reader.GetInt32("balance_level");
                        template.BossTelescopeRange = reader.GetFloat("boss_telescope_range");
                        template.CannotJump = reader.GetBoolean("cannot_jump");
                        template.CombatTextEnd = reader.GetBoolean("combat_text_end");
                        template.CombatTextStart = reader.GetBoolean("combat_text_start");
                        template.Crippled = reader.GetBoolean("crippled");
                        template.CustomDualMaterialFadeTime = reader.GetFloat("custom_dual_material_fade_time");
                        template.CustomDualMaterialId = reader.GetInt32("custom_dual_material_id");
                        template.Desc = reader.GetString("desc");
                        template.DisarmamentMainHand = reader.GetBoolean("disarmament_main_hand");
                        template.DisarmamentMusical = reader.GetBoolean("disarmament_musical");
                        template.DisarmamentOffHand = reader.GetBoolean("disarmament_off_hand");
                        template.DisarmamentRanged = reader.GetBoolean("disarmament_ranged");
//                        template.ExtraEffects = reader.GetString("extra_effects");
                        template.FallDamageImmortality = reader.GetBoolean("fall_damage_immortality");
                        template.FixAbilityLevelToOne = reader.GetBoolean("fix_ability_level_to_one");
                        template.Framehold = reader.GetBoolean("framehold");
                        template.FxGroupId = reader.GetInt32("fx_group_id");
                        template.GlidingFallSpeedFast = reader.GetFloat("gliding_fall_speed_fast");
                        template.GlidingFallSpeedNormal = reader.GetFloat("gliding_fall_speed_normal");
                        template.GlidingFallSpeedSlow = reader.GetFloat("gliding_fall_speed_slow");
                        template.GlidingLandHeight = reader.GetFloat("gliding_land_height");
                        template.GlidingLiftCount = reader.GetInt32("gliding_lift_count");
                        template.GlidingLiftDuration = reader.GetFloat("gliding_lift_duration");
                        template.GlidingLiftHeight = reader.GetFloat("gliding_lift_height");
                        template.GlidingLiftSpeed = reader.GetFloat("gliding_lift_speed");
                        template.GlidingLiftValidTime = reader.GetFloat("gliding_lift_valid_time");
                        template.GlidingMoveSpeedFast = reader.GetFloat("gliding_move_speed_fast");
                        template.GlidingMoveSpeedNormal = reader.GetFloat("gliding_move_speed_normal");
                        template.GlidingMoveSpeedSlow = reader.GetFloat("gliding_move_speed_slow");
                        template.GlidingSlidingTime = reader.GetFloat("gliding_sliding_time");
                        template.GlidingSmoothTime = reader.GetFloat("gliding_smooth_time");
                        template.GlidingStartupSpeed = reader.GetFloat("gliding_startup_speed");
                        template.GlidingStartupTime = reader.GetFloat("gliding_startup_time");
                        template.HeadScale = reader.GetFloat("head_scale");
                        template.IconId = reader.GetInt32("icon_id");
                        template.IdleAnim = reader.GetString("idle_anim");
                        template.ImmuneExceptCreatorRelationId = reader.GetInt32("immune_except_creator_relation_id");
                        template.ImmuneExceptCreatorRelationCheck = reader.GetBoolean("immune_except_creator_relation_check");
                        template.ImmuneHealth = reader.GetFloat("immune_health");
                        template.ImpossibleChangeTargeting = reader.GetBoolean("impossible_change_targeting");
                        template.ImpossibleRotate = reader.GetBoolean("impossible_rotate");
                        template.ImpossibleTargeting = reader.GetBoolean("impossible_targeting");
                        template.KindId = reader.GetInt32("kind_id");
                        template.KnockDown = reader.GetBoolean("knock_down");
                        template.MaxHighAbilityResource = reader.GetInt32("max_high_ability_resource");
                        template.MaxLifeTime = reader.GetInt32("max_life_time");
                        template.MeleeImmortality = reader.GetBoolean("melee_immortality");
                        template.MinHighAbilityResource = reader.GetInt32("min_high_ability_resource");
                        template.Name = reader.GetString("name");
                        template.NoCollideRigid = reader.GetBoolean("no_collide_rigid");
                        template.NoExpPenalty = reader.GetBoolean("no_exp_penalty");
                        template.NotToMateRider = reader.GetBoolean("not_to_mate_rider");
                        template.NotToSlaveRider = reader.GetBoolean("not_to_slave_rider");
                        template.OffPassive = reader.GetBoolean("off_passive");
                        template.OffPassiveExectionTagId = reader.GetInt32("off_passive_exection_tag_id");
                        template.OneTimeImmortality = reader.GetBoolean("one_time_immortality");
                        template.OnlyMyPet = reader.GetBoolean("only_my_pet");
                        template.OnlyPetOwner = reader.GetBoolean("only_pet_owner");
                        template.PercussionInstrumentStartAnimId = reader.GetInt32("percussion_instrument_start_anim_id");
                        template.PercussionInstrumentTickAnimId = reader.GetInt32("percussion_instrument_tick_anim_id");
                        template.RangedImmortality = reader.GetBoolean("ranged_immortality");
                        template.RemoveOnAutoattack = reader.GetBoolean("remove_on_autoattack");
                        template.RemoveOnUnbond = reader.GetBoolean("remove_on_unbond");
                        template.RemoveOnUnmountAttachPointId = reader.GetInt32("remove_on_unmount_attach_point_id");
                        template.RestrictActionbar = reader.GetBoolean("restrict_actionbar");
                        template.SetHeadScale = reader.GetBoolean("set_head_scale");
                        template.SiegeImmortality = reader.GetBoolean("siege_immortality");
                        template.SpellImmortality = reader.GetBoolean("spell_immortality");
                        template.StackRuleId = reader.GetInt32("stack_rule_id");
                        template.StopOnlineLpRegen = reader.GetBoolean("stop_online_lp_regen");
                        template.StringInstrumentStartAnimId = reader.GetInt32("string_instrument_start_anim_id");
                        template.StringInstrumentTickAnimId = reader.GetInt32("string_instrument_tick_anim_id");
                        template.TargetingRelationId = reader.GetInt32("targeting_relation_id");
                        template.TargetingUseOriginSource = reader.GetBoolean("targeting_use_origin_source");
                        template.TubeInstrumentStartAnimId = reader.GetInt32("tube_instrument_start_anim_id");
                        template.TubeInstrumentTickAnimId = reader.GetInt32("tube_instrument_tick_anim_id");

                        // _effects["Buff"].Add(template.Id, template);
                        _buffs.Add(template.Id, template);
                    }
                }
            }
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM buff_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new BuffEffect();
                        template.Id = reader.GetUInt32("id");
                        var buffId = reader.GetUInt32("buff_id");
                        if (_buffs.TryGetValue(buffId, out var buff))
                            template.Buff = buff;
                        template.Chance = reader.GetInt32("chance");
                        template.Stack = reader.GetInt32("stack");
                        template.AbLevel = reader.GetInt32("ab_level");
                        _effects["BuffEffect"].Add(template.Id, template);
                    }
                }
            }
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM buff_tick_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var buffId = reader.GetUInt32("buff_id");
                        if (_buffs.TryGetValue(buffId, out var template))
                        {
                            var tickEffect = new TickEffect();
                            tickEffect.EffectId = reader.GetUInt32("effect_id");
                            tickEffect.TargetBuffTagId = reader.GetUInt32("target_buff_tag_id", 0);
                            tickEffect.TargetNoBuffTagId = reader.GetUInt32("target_nobuff_tag_id", 0);
                            template.TickEffects.Add(tickEffect);
                        }
                        else
                        {
                            // Обработка случая, когда template не найден
                            Logger.Warn($"Template for buffId {buffId} not found.");
                        }
                    }
                }
            }
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM unit_modifiers WHERE owner_type='Buff'"; // TODO OwnerType: BuffUnitModifier -> buff_unit_modifiers
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var buffId = reader.GetUInt32("owner_id");
                        if (_buffs.TryGetValue(buffId, out var buff))
                        {
                            var template = new BonusTemplate();
                            template.Attribute = (UnitAttribute)reader.GetByte("unit_attribute_id");
                            template.ModifierType = (UnitModifierType)reader.GetByte("unit_modifier_type_id");
                            template.Value = reader.GetInt32("value");
                            template.LinearLevelBonus = reader.GetInt32("linear_level_bonus");
                            buff.Bonuses.Add(template);
                        }
                        else
                        {
                            // Обработка случая, когда template не найден
                            Logger.Warn($"Template for buffId {buffId} not found.");
                        }
                    }
                }
            }
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM dynamic_unit_modifiers";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var buffId = reader.GetUInt32("buff_id");
                        if (_buffs.TryGetValue(buffId, out var buff))
                        {
                            var template = new DynamicBonusTemplate();
                            template.Attribute = (UnitAttribute)reader.GetByte("unit_attribute_id");
                            template.ModifierType = (UnitModifierType)reader.GetByte("unit_modifier_type_id");
                            template.FuncId = reader.GetUInt32("func_id");
                            template.FuncType = reader.GetString("func_type");
                            buff.DynamicBonuses.Add(template);
                        }
                        else
                        {
                            // Обработка случая, когда template не найден
                            Logger.Warn($"Template for buffId {buffId} not found.");
                        }
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM skill_controllers";
                command.Prepare();
                using (var sqliteReader = command.ExecuteReader())
                using (var reader = new SQLiteWrapperReader(sqliteReader))
                {
                    while (reader.Read())
                    {
                        var template = new SkillControllerTemplate();
                        template.Id = reader.GetUInt32("id");
                        template.ActiveWeaponId = reader.GetByte("active_weapon_id");
                        template.EndAnimId = reader.GetUInt32("end_anim_id");
                        template.EndSkillId = reader.GetUInt32("end_skill_id");
                        template.KindId = reader.GetUInt32("kind_id");
                        template.StartAnimId = reader.GetUInt32("start_anim_id");
                        template.StrValue1 = reader.GetString("str_value1");
                        template.TransitionAnim1Id = reader.GetUInt32("transition_anim_1_id");
                        template.TransitionAnim2Id = reader.GetUInt32("transition_anim_2_id");
                        for (var i = 0; i < 15; i++)
                            template.Value[i] = reader.GetInt32($"value{i + 1}", 0);
                        _effects["SkillController"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM account_attribute_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new AccountAttributeEffect();
                        template.Id = reader.GetUInt32("id");
                        template.BindWorld = reader.GetBoolean("bind_world");
                        template.Count = reader.GetUInt32("count");
                        template.IsAdd = reader.GetBoolean("is_add");
                        template.KindId = reader.GetUInt32("kind_id");
                        template.KindValue = reader.GetUInt32("kind_value");
                        template.Time = reader.GetUInt32("time");
                        _effects["AccountAttributeEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM accept_quest_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new AcceptQuestEffect();
                        template.Id = reader.GetUInt32("id");
                        template.QuestId = reader.GetUInt32("quest_id");
                        _effects["AcceptQuestEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM aggro_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new AggroEffect();
                        template.Id = reader.GetUInt32("id");
                        template.UseFixedAggro = reader.GetBoolean("use_fixed_aggro", true);
                        template.FixedMin = reader.GetInt32("fixed_min");
                        template.FixedMax = reader.GetInt32("fixed_max");
                        template.UseLevelAggro = reader.GetBoolean("use_level_aggro", true);
                        template.LevelMd = reader.GetFloat("level_md");
                        template.LevelVaStart = reader.GetInt32("level_va_start");
                        template.LevelVaEnd = reader.GetInt32("level_va_end");
                        template.UseChargedBuff = reader.GetBoolean("use_charged_buff", true);
                        template.ChargedBuffId = reader.GetUInt32("charged_buff_id", 0);
                        template.ChargedMul = reader.GetFloat("charged_mul");
                        _effects["AggroEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM bubble_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new BubbleEffect();
                        template.Id = reader.GetUInt32("id");
                        template.KindId = reader.GetUInt32("kind_id");
                        _effects["BubbleEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection2.CreateCommand())
            {
                command.CommandText = "SELECT * FROM cinema_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new CinemalEffect();
                        template.Id = reader.GetUInt32("id");
                        template.CinemaId = reader.GetUInt32("cinema_id");
                        _effects["CinemaEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM cleanup_ucc_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new CleanupUccEffect();
                        template.Id = reader.GetUInt32("id");
                        _effects["CleanupUccEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM conversion_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new ConversionEffect();
                        template.Id = reader.GetUInt32("id");
                        template.CategoryId = reader.GetUInt32("category_id");
                        template.SourceCategoryId = reader.GetUInt32("source_category_id");
                        template.SourceValue = reader.GetInt32("source_value");
                        template.TargetCategoryId = reader.GetUInt32("target_category_id");
                        template.TargetValue = reader.GetInt32("target_value");
                        _effects["ConversionEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM craft_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new CraftEffect();
                        template.Id = reader.GetUInt32("id");
                        template.WorldInteraction = (WorldInteractionType)reader.GetUInt32("wi_id");
                        _effects["CraftEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM damage_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new DamageEffect();
                        template.Id = reader.GetUInt32("id");
                        template.DamageType = (DamageType)reader.GetInt32("damage_type_id");
                        template.FixedMin = reader.GetInt32("fixed_min");
                        template.FixedMax = reader.GetInt32("fixed_max");
                        template.Multiplier = reader.GetFloat("multiplier");
                        template.UseMainhandWeapon = reader.GetBoolean("use_mainhand_weapon", true);
                        template.UseOffhandWeapon = reader.GetBoolean("use_offhand_weapon", true);
                        template.UseRangedWeapon = reader.GetBoolean("use_ranged_weapon", true);
                        template.CriticalBonus = reader.GetInt32("critical_bonus");
                        template.TargetBuffTagId = reader.GetUInt32("target_buff_tag_id", 0);
                        template.TargetBuffBonus = reader.GetInt32("target_buff_bonus");
                        template.UseFixedDamage = reader.GetBoolean("use_fixed_damage", true);
                        template.UseLevelDamage = reader.GetBoolean("use_level_damage", true);
                        template.LevelMd = reader.GetFloat("level_md");
                        template.LevelVaStart = reader.GetInt32("level_va_start");
                        template.LevelVaEnd = reader.GetInt32("level_va_end");
                        template.TargetBuffBonusMul = reader.GetFloat("target_buff_bonus_mul");
                        template.UseChargedBuff = reader.GetBoolean("use_charged_buff", true);
                        template.ChargedBuffId = reader.GetUInt32("charged_buff_id", 0);
                        template.ChargedMul = reader.GetFloat("charged_mul");
                        template.AggroMultiplier = reader.GetFloat("aggro_multiplier");
                        template.HealthStealRatio = reader.GetInt32("health_steal_ratio");
                        template.ManaStealRatio = reader.GetInt32("mana_steal_ratio");
                        template.DpsMultiplier = reader.GetFloat("dps_multiplier");
                        template.WeaponSlotId = reader.GetInt32("weapon_slot_id");
                        //template.CheckCrime = reader.GetBoolean("check_crime", true); // there is no such field in the database for version 3.0.3.0
                        template.HitAnimTimingId = reader.GetUInt32("hit_anim_timing_id");
                        template.UseTargetChargedBuff = reader.GetBoolean("use_target_charged_buff", true);
                        template.TargetChargedBuffId = reader.GetUInt32("target_charged_buff_id", 0);
                        template.TargetChargedMul = reader.GetFloat("target_charged_mul");
                        template.DpsIncMultiplier = reader.GetFloat("dps_inc_multiplier");
                        template.EngageCombat = reader.GetBoolean("engage_combat", true);
                        template.Synergy = reader.GetBoolean("synergy", true);
                        template.ActabilityGroupId = reader.GetUInt32("actability_group_id", 0);
                        template.ActabilityStep = reader.GetInt32("actability_step");
                        template.ActabilityMul = reader.GetFloat("actability_mul");
                        template.ActabilityAdd = reader.GetFloat("actability_add");
                        template.ChargedLevelMul = reader.GetFloat("charged_level_mul");
                        template.AdjustDamageByHeight = reader.GetBoolean("adjust_damage_by_height", true);
                        template.UsePercentDamage = reader.GetBoolean("use_percent_damage", true);
                        template.PercentMin = reader.GetInt32("percent_min");
                        template.PercentMax = reader.GetInt32("percent_max");
                        template.UseCurrentHealth = reader.GetBoolean("use_current_health", true);
                        template.TargetHealthMin = reader.GetInt32("target_health_min");
                        template.TargetHealthMax = reader.GetInt32("target_health_max");
                        template.TargetHealthMul = reader.GetFloat("target_health_mul");
                        template.TargetHealthAdd = reader.GetInt32("target_health_add");
                        template.FireProc = reader.GetBoolean("fire_proc", true);
                        _effects["DamageEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM dispel_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new DispelEffect();
                        template.Id = reader.GetUInt32("id");
                        template.DispelCount = reader.GetInt32("dispel_count");
                        template.CureCount = reader.GetInt32("cure_count");
                        template.BuffTagId = reader.GetUInt32("buff_tag_id", 0);
                        _effects["DispelEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM flying_state_change_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new FlyingStateChangeEffect();
                        template.Id = reader.GetUInt32("id");
                        template.FlyingState = reader.GetBoolean("flying_state", true);
                        _effects["FlyingStateChangeEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM gain_loot_pack_item_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new GainLootPackItemEffect();
                        template.Id = reader.GetUInt32("id");
                        template.LootPackId = reader.GetUInt32("loot_pack_id");
                        template.ConsumeSourceItem = reader.GetBoolean("consume_source_item", true);
                        template.ConsumeItemId = reader.GetUInt32("consume_item_id", 0);
                        template.ConsumeCount = reader.GetInt32("consume_count");
                        template.InheritGrade = reader.GetBoolean("inherit_grade", true);
                        _effects["GainLootPackItemEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM heal_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new HealEffect();
                        template.Id = reader.GetUInt32("id");
                        template.UseFixedHeal = reader.GetBoolean("use_fixed_heal", true);
                        template.FixedMin = reader.GetInt32("fixed_min");
                        template.FixedMax = reader.GetInt32("fixed_max");
                        template.UseLevelHeal = reader.GetBoolean("use_level_heal", true);
                        template.LevelMd = reader.GetFloat("level_md");
                        template.LevelVaStart = reader.GetInt32("level_va_start");
                        template.LevelVaEnd = reader.GetInt32("level_va_end");
                        template.Percent = reader.GetBoolean("percent", true);
                        template.UseChargedBuff = reader.GetBoolean("use_charged_buff", true);
                        template.ChargedBuffId = reader.GetUInt32("charged_buff_id", 0);
                        template.ChargedMul = reader.GetFloat("charged_mul");
                        template.SlaveApplicable = reader.GetBoolean("slave_applicable", true);
                        template.IgnoreHealAggro = reader.GetBoolean("ignore_heal_aggro", true);
                        template.DpsMultiplier = reader.GetFloat("dps_multiplier");
                        template.ActabilityGroupId = reader.GetUInt32("actability_group_id", 0);
                        template.ActabilityStep = reader.GetInt32("actability_step");
                        template.ActabilityMul = reader.GetFloat("actability_mul");
                        template.ActabilityAdd = reader.GetFloat("actability_add");
                        _effects["HealEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM imprint_ucc_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new ImprintUccEffect();
                        template.Id = reader.GetUInt32("id");
                        template.ItemId = reader.GetUInt32("item_id", 0);
                        _effects["ImprintUccEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM impulse_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new ImpulseEffect();
                        template.Id = reader.GetUInt32("id");
                        template.VelImpulseX = reader.GetFloat("vel_impulse_x");
                        template.VelImpulseY = reader.GetFloat("vel_impulse_y");
                        template.VelImpulseZ = reader.GetFloat("vel_impulse_z");
                        template.AngvelImpulseX = reader.GetFloat("angvel_impulse_x");
                        template.AngvelImpulseY = reader.GetFloat("angvel_impulse_y");
                        template.AngvelImpulseZ = reader.GetFloat("angvel_impulse_z");
                        template.ImpulseX = reader.GetFloat("impulse_x");
                        template.ImpulseY = reader.GetFloat("impulse_y");
                        template.ImpulseZ = reader.GetFloat("impulse_z");
                        template.AngImpulseX = reader.GetFloat("ang_impulse_x");
                        template.AngImpulseY = reader.GetFloat("ang_impulse_y");
                        template.AngImpulseZ = reader.GetFloat("ang_impulse_z");
                        _effects["ImpulseEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM interaction_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new InteractionEffect();
                        template.Id = reader.GetUInt32("id");
                        template.WorldInteraction = (WorldInteractionType)reader.GetInt32("wi_id");
                        template.DoodadId = reader.GetUInt32("doodad_id", 0);
                        _effects["InteractionEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM kill_npc_without_corpse_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new KillNpcWithoutCorpseEffect();
                        template.Id = reader.GetUInt32("id");
                        template.NpcId = reader.GetUInt32("npc_id");
                        template.Radius = reader.GetFloat("radius");
                        template.GiveExp = reader.GetBoolean("give_exp", true);
                        template.Vanish = reader.GetBoolean("vanish", true);
                        _effects["KillNpcWithoutCorpseEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM mana_burn_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new ManaBurnEffect();
                        template.Id = reader.GetUInt32("id");
                        template.BaseMin = reader.GetInt32("base_min");
                        template.BaseMax = reader.GetInt32("base_max");
                        template.DamageRatio = reader.GetInt32("damage_ratio");
                        template.LevelMd = reader.GetFloat("level_md");
                        template.LevelVaStart = reader.GetInt32("level_va_start");
                        template.LevelVaEnd = reader.GetInt32("level_va_end");
                        _effects["ManaBurnEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM move_to_rez_point_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new MoveToRezPointEffect();
                        template.Id = reader.GetUInt32("id");
                        _effects["MoveToRezPointEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection2.CreateCommand())
            {
                command.CommandText = "SELECT * FROM npc_control_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new NpcControlEffect();
                        template.Id = reader.GetUInt32("id");
                        template.CategoryId = (NpcControlCategory)reader.GetUInt32("category_id");
                        template.ParamString = reader.GetString("param_string", "");
                        template.ParamInt = reader.GetUInt32("param_int");
                        _effects["NpcControlEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM open_portal_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new OpenPortalEffect();
                        template.Id = reader.GetUInt32("id");
                        template.Distance = reader.GetFloat("distance");
                        _effects["OpenPortalEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM physical_explosion_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new PhysicalExplosionEffect();
                        template.Id = reader.GetUInt32("id");
                        template.Radius = reader.GetFloat("radius");
                        template.HoleSize = reader.GetFloat("hole_size");
                        template.Pressure = reader.GetFloat("pressure");
                        _effects["PhysicalExplosionEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM put_down_backpack_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new PutDownBackpackEffect();
                        template.Id = reader.GetUInt32("id");
                        template.BackpackDoodadId = reader.GetUInt32("backpack_doodad_id");
                        _effects["PutDownBackpackEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM recover_exp_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new RecoverExpEffect();
                        template.Id = reader.GetUInt32("id");
                        template.NeedMoney = reader.GetBoolean("need_money", true);
                        template.NeedLaborPower = reader.GetBoolean("need_labor_power", true);
                        template.NeedPriest = reader.GetBoolean("need_priest", true);
                        // TODO 1.2 // template.Penaltied = reader.GetBoolean("penaltied", true);
                        _effects["RecoverExpEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM repair_slave_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new RepairSlaveEffect();
                        template.Id = reader.GetUInt32("id");
                        template.Health = reader.GetInt32("health");
                        template.Mana = reader.GetInt32("mana");
                        _effects["RepairSlaveEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM report_crime_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new ReportCrimeEffect();
                        template.Id = reader.GetUInt32("id");
                        template.Value = reader.GetInt32("value");
                        template.CrimeKindId = reader.GetUInt32("crime_kind_id");
                        _effects["ReportCrimeEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM reset_aoe_diminishing_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new ResetAoeDiminishingEffect();
                        template.Id = reader.GetUInt32("id");
                        _effects["ResetAoeDiminishingEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM move_to_location_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new MoveToLocationEffect();
                        template.Id = reader.GetUInt32("id");
                        template.OwnHouseOnly = reader.GetBoolean("own_house_only", true);
                        _effects["MoveToLocationEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM restore_mana_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new RestoreManaEffect();
                        template.Id = reader.GetUInt32("id");
                        template.UseFixedValue = reader.GetBoolean("use_fixed_value", true);
                        template.FixedMin = reader.GetInt32("fixed_min");
                        template.FixedMax = reader.GetInt32("fixed_max");
                        template.UseLevelValue = reader.GetBoolean("use_level_value", true);
                        template.LevelMd = reader.GetFloat("level_md");
                        template.LevelVaStart = reader.GetInt32("level_va_start");
                        template.LevelVaEnd = reader.GetInt32("level_va_end");
                        template.Percent = reader.GetBoolean("percent", true);
                        _effects["RestoreManaEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM scoped_f_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new ScopedFEffect();
                        template.Id = reader.GetUInt32("id");
                        template.Range = reader.GetInt32("range");
                        template.Key = reader.GetBoolean("key", true);
                        template.DoodadId = reader.GetUInt32("doodad_id");
                        _effects["ScopedFEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM spawn_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new SpawnEffect();
                        template.Id = reader.GetUInt32("id");
                        template.OwnerTypeId = (BaseUnitType)reader.GetUInt32("owner_type_id");
                        template.SubType = reader.GetUInt32("sub_type");
                        template.PosDirId = reader.GetUInt32("pos_dir_id");
                        //template.PosAngle = reader.GetFloat("pos_angle"); // there is no such field in the database for version 3.0.3.0
                        template.PosAngleMax = reader.GetFloat("pos_angle_max"); // added
                        template.PosAngleMin = reader.GetFloat("pos_angle_min"); // added
                        template.PosDistanceMax = reader.GetFloat("pos_distance_max"); // added
                        template.PosDistanceMin = reader.GetFloat("pos_distance_min"); // added
                        template.OriDirId = reader.GetUInt32("ori_dir_id");
                        template.OriAngle = reader.GetFloat("ori_angle");
                        template.UseSummonerFaction = reader.GetBoolean("use_summoner_faction", true);
                        template.LifeTime = reader.GetFloat("life_time");
                        template.DespawnOnCreatorDeath = reader.GetBoolean("despawn_on_creator_death", true);
                        template.UseSummonerAggroTarget = reader.GetBoolean("use_summoner_aggro_target", true);
                        template.MateStateId = (MateState)reader.GetUInt32("mate_state_id", 0);
                        _effects["SpawnEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection2.CreateCommand())
            {
                command.CommandText = "SELECT * FROM npc_spawner_spawn_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new NpcSpawnerSpawnEffect();
                        template.Id = reader.GetUInt32("id");
                        template.SpawnerId = reader.GetUInt32("spawner_id", 0);
                        template.LifeTime = reader.GetFloat("life_time");
                        template.DespawnOnCreatorDeath = reader.GetBoolean("despawn_on_creator_death", true);
                        template.UseSummonerAggroTarget = reader.GetBoolean("use_summoner_aggro_target", true);
                        template.ActivationState = reader.GetBoolean("activation_state", true);
                        _effects["NpcSpawnerSpawnEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection2.CreateCommand())
            {
                command.CommandText = "SELECT * FROM npc_spawner_despawn_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new NpcSpawnerDespawnEffect();
                        template.Id = reader.GetUInt32("id");
                        template.SpawnerId = reader.GetUInt32("spawner_id");
                        _effects["NpcSpawnerDespawnEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM spawn_fish_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new SpawnFishEffect();
                        template.Id = reader.GetUInt32("id");
                        template.Range = reader.GetUInt32("range");
                        template.DoodadId = reader.GetUInt32("doodad_id", 0);
                        _effects["SpawnFishEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM spawn_gimmick_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new SpawnGimmickEffect();
                        template.Id = reader.GetUInt32("id");
                        template.GimmickId = reader.GetUInt32("gimmick_id");
                        template.OffsetFromSource = reader.GetBoolean("offset_from_source", true);
                        template.OffsetCoordiateId = reader.GetUInt32("offset_coordiate_id");
                        template.OffsetX = reader.GetFloat("offset_x");
                        template.OffsetY = reader.GetFloat("offset_y");
                        template.OffsetZ = reader.GetFloat("offset_z");
                        template.Scale = reader.GetFloat("scale");
                        template.VelocityCoordiateId = reader.GetUInt32("velocity_coordiate_id");
                        template.VelocityX = reader.GetFloat("velocity_x");
                        template.VelocityY = reader.GetFloat("velocity_y");
                        template.VelocityZ = reader.GetFloat("velocity_z");
                        template.AngVelCoordiateId = reader.GetUInt32("ang_vel_coordiate_id");
                        template.AngVelX = reader.GetFloat("ang_vel_x");
                        template.AngVelY = reader.GetFloat("ang_vel_y");
                        template.AngVelZ = reader.GetFloat("ang_vel_z");
                        _effects["SpawnGimmickEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM special_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new SpecialEffect();
                        template.Id = reader.GetUInt32("id");
                        template.SpecialEffectTypeId = (SpecialType)reader.GetInt32("special_effect_type_id");
                        template.Value1 = reader.GetInt32("value1");
                        template.Value2 = reader.GetInt32("value2");
                        template.Value3 = reader.GetInt32("value3");
                        template.Value4 = reader.GetInt32("value4");
                        template.Value5 = reader.GetInt32("value5");
                        template.Value6 = reader.GetInt32("value6");
                        template.Value7 = reader.GetInt32("value7");
                        _effects["SpecialEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM train_craft_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new TrainCraftEffect();
                        template.Id = reader.GetUInt32("id");
                        template.CraftId = reader.GetUInt32("craft_id");
                        _effects["TrainCraftEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM train_craft_rank_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new TrainCraftRankEffect();
                        template.Id = reader.GetUInt32("id");
                        template.KindId = reader.GetUInt32("kind_id");
                        template.RankId = reader.GetUInt32("rank_id");
                        _effects["TrainCraftRankEffect"].Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new EffectType();
                        template.Id = reader.GetUInt32("id");
                        template.ActualId = reader.GetUInt32("actual_id");
                        template.Type = reader.GetString("actual_type");
                        _types.Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM skill_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var skillId = reader.GetUInt32("skill_id");
                        if (!_skills.ContainsKey(skillId))
                            continue;

                        var template = new SkillEffect();
                        var effectId = reader.GetUInt32("effect_id");

                        //for easier debugging
                        template.EffectId = effectId;

                        var type = _types[effectId];
                        if (_effects.ContainsKey(type.Type) && _effects[type.Type].ContainsKey(type.ActualId))
                            template.Template = _effects[type.Type][type.ActualId];

                        template.Weight = reader.GetInt32("weight");
                        template.StartLevel = reader.GetByte("start_level");
                        template.EndLevel = reader.GetByte("end_level");
                        template.Friendly = reader.GetBoolean("friendly", true);
                        template.NonFriendly = reader.GetBoolean("non_friendly", true);
                        template.TargetBuffTagId = reader.GetUInt32("target_buff_tag_id", 0);
                        template.TargetNoBuffTagId = reader.GetUInt32("target_nobuff_tag_id", 0);
                        template.SourceBuffTagId = reader.GetUInt32("source_buff_tag_id", 0);
                        template.SourceNoBuffTagId = reader.GetUInt32("source_nobuff_tag_id", 0);
                        template.Chance = reader.GetInt32("chance");
                        template.Front = reader.GetBoolean("front", true);
                        template.Back = reader.GetBoolean("back", true);
                        template.TargetNpcTagId = reader.GetUInt32("target_npc_tag_id", 0);
                        template.ApplicationMethod = (SkillEffectApplicationMethod)reader.GetUInt32("application_method_id");
                        template.ConsumeSourceItem = reader.GetBoolean("consume_source_item", true);
                        template.ConsumeItemId = reader.GetUInt32("consume_item_id", 0);
                        template.ConsumeItemCount = reader.GetInt32("consume_item_count");
                        template.AlwaysHit = reader.GetBoolean("always_hit", true);
                        template.ItemSetId = reader.GetUInt32("item_set_id", 0);
                        template.InteractionSuccessHit = reader.GetBoolean("interaction_success_hit", true);
                        template.CheckNoSourceTagSrc = reader.GetBoolean("check_no_source_tag_src", true);
                        template.CheckNoTargetTagSrc = reader.GetBoolean("check_no_target_tag_src", true);
                        template.CheckSourceTagSrc = reader.GetBoolean("check_source_tag_src", true);
                        template.CheckTargetTagSrc = reader.GetBoolean("check_target_tag_src", true);
                        _skills[skillId].Effects.Add(template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM tagged_buffs";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var tagId = reader.GetUInt32("tag_id");
                        var buffId = reader.GetUInt32("buff_id");

                        if (!_buffTags.ContainsKey(buffId))
                            _buffTags.Add(buffId, []);
                        _buffTags[buffId].Add(tagId);

                        if (!_taggedBuffs.ContainsKey(tagId))
                            _taggedBuffs.Add(tagId, []);
                        _taggedBuffs[tagId].Add(buffId);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM skill_modifiers";
                command.Prepare();
                using (var sqliteReader = command.ExecuteReader())
                using (var reader = new SQLiteWrapperReader(sqliteReader))
                {
                    while (reader.Read())
                    {
                        var template = new SkillModifier();
                        //Id = reader.GetUInt32("id"), // there is no such field in the database for version 3.0.3.0
                        template.OwnerId = reader.GetUInt32("owner_id");
                        template.OwnerType = reader.GetString("owner_type");
                        template.TagId = reader.GetUInt32("tag_id", 0);
                        template.SkillAttribute = (SkillAttribute)reader.GetUInt32("skill_attribute_id");
                        template.UnitModifierType = (UnitModifierType)reader.GetUInt32("unit_modifier_type_id");
                        template.Value = reader.GetInt32("value");
                        template.SkillId = reader.GetUInt32("skill_id", 0);
                        template.Synergy = reader.GetBoolean("synergy", true);

                        if (!_skillModifiers.ContainsKey(template.OwnerId))
                            _skillModifiers.Add(template.OwnerId, []);
                        _skillModifiers[template.OwnerId].Add(template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM tagged_skills";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var tagId = reader.GetUInt32("tag_id");
                        var skillId = reader.GetUInt32("skill_id");

                        //I guess we need this
                        if (!_skillTags.ContainsKey(skillId))
                            _skillTags.Add(skillId, []);
                        _skillTags[skillId].Add(tagId);

                        if (!_taggedSkills.ContainsKey(tagId))
                            _taggedSkills.Add(tagId, []);
                        _taggedSkills[tagId].Add(skillId);
                    }
                }
            }

            using (var command = connection2.CreateCommand())
            {
                command.CommandText = "SELECT * FROM combat_buffs";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var combatBuffTemplate = new CombatBuffTemplate()
                        {
                            Id = reader.GetUInt32("id"),
                            HitSkillId = reader.GetUInt32("hit_skill_id", 0),
                            HitType = (SkillHitType)reader.GetUInt32("hit_type_id"),
                            BuffId = reader.GetUInt32("buff_id"),
                            BuffFromSource = reader.GetBoolean("buff_from_source", true),
                            BuffToSource = reader.GetBoolean("buff_to_source", true),
                            ReqSkillId = reader.GetUInt32("req_skill_id", 0),
                            ReqBuffId = reader.GetUInt32("req_buff_id"),
                            IsHealSpell = reader.GetBoolean("is_heal_spell", true)
                        };

                        if (!_combatBuffs.ContainsKey(combatBuffTemplate.ReqBuffId))
                            _combatBuffs.Add(combatBuffTemplate.ReqBuffId, []);
                        _combatBuffs[combatBuffTemplate.ReqBuffId].Add(combatBuffTemplate);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM skill_dynamic_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var jsonData = reader.GetString("effect");
                        var skillId = reader.GetUInt32("skill_id");
                        // TODO add DecryptJson()

                        var jObj = JObject.Parse(jsonData);

                        var effect = jObj.GetValue("effect").ToString();

                        if (effect == "selective_item")
                        {
                            _dynamicEffects.selectiveItems.Add(skillId, new SelectiveItems(jObj));
                        }
                        else if (effect == "bless_uthstin")
                        {
                            _dynamicEffects.blessUthstins.TryAdd(skillId, new BlessUthstin(jObj, jsonData));
                        }
                    }
                }
            }

            Logger.Info("Skill effects loaded");

            _buffTriggers = new Dictionary<uint, List<BuffTriggerTemplate>>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM buff_triggers";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var trigger = new BuffTriggerTemplate();
                        var buffId = reader.GetUInt32("buff_id");
                        if (!_buffTriggers.ContainsKey(buffId))
                        {
                            _buffTriggers.Add(buffId, []);
                        }
                        trigger.Id = reader.GetUInt32("id");
                        trigger.CheckNoTagSrcInOwner = reader.GetBoolean("check_no_tag_src_in_owner", true);
                        trigger.CheckNoTagSrcInSource = reader.GetBoolean("check_no_tag_src_in_source", true);
                        trigger.CheckNoTagSrcInTarget = reader.GetBoolean("check_no_tag_src_in_target", true);
                        trigger.CheckTagSrcInOwner = reader.GetBoolean("check_tag_src_in_owner", true);
                        trigger.CheckTagSrcInSource = reader.GetBoolean("check_tag_src_in_source", true);
                        trigger.CheckTagSrcInTarget = reader.GetBoolean("check_tag_src_in_target", true);
                        trigger.DelayTime = reader.GetUInt32("delay_time", 0);
                        trigger.Effect = GetEffectTemplate(reader.GetUInt32("effect_id"));
                        trigger.Kind = (BuffEventTriggerKind)reader.GetUInt16("event_id");
                        trigger.OwnerBuffTagId = reader.GetUInt32("owner_buff_tag_id", 0);
                        trigger.OwnerNoBuffTagId = reader.GetUInt32("owner_no_buff_tag_id", 0);
                        trigger.SourceAgentId = reader.GetUInt32("source_agent_id", 0);
                        trigger.SourceBuffTagId = reader.GetUInt32("source_buff_tag_id", 0);
                        trigger.SourceNoBuffTagId = reader.GetUInt32("source_no_buff_tag_id", 0);
                        trigger.TargetAgentId = reader.GetUInt32("target_agent_id", 0);
                        trigger.TargetBuffTagId = reader.GetUInt32("target_buff_tag_id", 0);
                        trigger.TargetNoBuffTagId = reader.GetUInt32("target_no_buff_tag_id", 0);
                        trigger.UseCollisionImpact = reader.GetBoolean("use_collision_impact", true);
                        trigger.UseDamageAmount = reader.GetBoolean("use_damage_amount", true);
                        trigger.UseStackCount = reader.GetBoolean("use_stack_count", true);
                        //trigger.EffectOnSource = reader.GetBoolean("effect_on_source", true); // there is no such field in the database for version 3.0.3.0
                        //trigger.UseOriginalSource = reader.GetBoolean("use_original_source", true); // there is no such field in the database for version 3.0.3.0
                        //trigger.Synergy = reader.GetBoolean("synergy", true); // there is no such field in the database for version 3.0.3.0

                        //Apparently this is possible
                        if (trigger.Effect != null)
                        {
                            _buffTriggers[buffId].Add(trigger);
                        }
                    }
                }
            }
            Logger.Info("Buff triggers loaded");

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * from skill_reagents";
                command.Prepare();
                using (var sqliteReader = command.ExecuteReader())
                using (var reader = new SQLiteWrapperReader(sqliteReader))
                {
                    while (reader.Read())
                    {
                        var template = new SkillReagent
                        {
                            Id = reader.GetUInt32("id"),
                            SkillId = reader.GetUInt32("skill_id"),
                            ItemId = reader.GetUInt32("item_id"),
                            Amount = reader.GetInt16("amount")
                        };
                        _skillReagents.Add(template.Id, template);
                    }
                }
            }
            Logger.Info("Skill Reagents loaded");

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * from skill_products";
                command.Prepare();
                using (var sqliteReader = command.ExecuteReader())
                using (var reader = new SQLiteWrapperReader(sqliteReader))
                {
                    while (reader.Read())
                    {
                        var template = new SkillProduct
                        {
                            Id = reader.GetUInt32("id"),
                            SkillId = reader.GetUInt32("skill_id"),
                            ItemId = reader.GetUInt32("item_id"),
                            Amount = reader.GetInt16("amount")
                        };
                        _skillProducts.Add(template.Id, template);
                    }
                }
                Logger.Info("Skill Products loaded");

                OnSkillsLoaded?.Invoke(this, new EventArgs());
            }
        }

        foreach (var skillTemplate in _skills.Values.Where(x => x.AutoLearn))
        {
            if (skillTemplate.AbilityId == 0 && !_defaultSkills.ContainsKey(skillTemplate.Id))
                _commonSkills.Add(skillTemplate.Id);
            if (skillTemplate.AbilityId == 0 || skillTemplate.AbilityLevel > 1 || !skillTemplate.Show)
                continue;
            var ability = (AbilityType)skillTemplate.AbilityId;
            if (!_startAbilitySkills.ContainsKey(ability))
                _startAbilitySkills.Add(ability, []);
            _startAbilitySkills[ability].Add(skillTemplate);
        }

        _loaded = true;
    }

    /// <summary>
    /// Create an estimated use time for a skill
    /// </summary>
    /// <param name="skillTemplate">SkillTemplate to base on</param>
    /// <param name="caster">Unit to use the stats from for calculations</param>
    /// <param name="includeCooldown">Include the skill's cooldown time</param>
    /// <param name="additionalDelay">Additional delay to add (uses cooldown multiplier)</param>
    /// <returns></returns>
    public static double GetAttackDelay(SkillTemplate skillTemplate, Unit caster, bool includeCooldown = true, double additionalDelay = 1000.0)
    {
        // This isn't 100% accurate, but it feels "close enough"
        // TODO: Implement weapon speed delays
        var castTime = skillTemplate.CastingTime * caster.CastTimeMul * 1.0;
        var coolDownTime = includeCooldown ? skillTemplate.CooldownTime * (caster.GlobalCooldownMul / 100.0) : 0.0;
        var additionalTime = additionalDelay * (caster.GlobalCooldownMul / 100.0);
        return castTime + coolDownTime + additionalTime;
    }

    /// <summary>
    /// Gets the first spawn effect for a given Gimmick TemplateId
    /// </summary>
    /// <param name="gimmickTemplateId"></param>
    /// <returns></returns>
    public SpawnGimmickEffect GetSpawnGimmickEffect(uint gimmickTemplateId)
    {
        if (!_effects.TryGetValue("SpawnGimmickEffect", out var spawnGimmickEffects))
            return null;

        foreach (var effect in spawnGimmickEffects.Values)
        {
            if (effect is not SpawnGimmickEffect spawnGimmickEffect)
                continue;
            if (spawnGimmickEffect.GimmickId == gimmickTemplateId)
                return spawnGimmickEffect;
        }

        return null;
    }

    /// <summary>
    /// Gets the related ActAbility to a skill
    /// </summary>
    /// <param name="skillId"></param>
    /// <returns></returns>
    public ActabilityType GetSkillActAbility(uint skillId)
    {
        if (!_skills.TryGetValue(skillId, out var value))
            return ActabilityType.None;
        return (ActabilityType)value.ActabilityGroupId;
    }
}
