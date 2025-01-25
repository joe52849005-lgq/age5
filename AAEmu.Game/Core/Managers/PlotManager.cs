﻿using System.Collections.Generic;
using System.Linq;

using AAEmu.Commons.Utils;
using AAEmu.Game.Models.Game.Skills.Plots;
using AAEmu.Game.Models.Game.Skills.Plots.Tree;
using AAEmu.Game.Models.Game.Skills.Plots.Type;
using AAEmu.Game.Utils.DB;

using NLog;

namespace AAEmu.Game.Core.Managers;

public class PlotManager : Singleton<PlotManager>
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();
    private bool _loaded = false;

    private Dictionary<uint, Plot> _plots;
    private Dictionary<uint, PlotEventTemplate> _eventTemplates;
    private Dictionary<uint, PlotCondition> _conditions;
    //private Dictionary<uint, PlotAoeCondition> _aoeConditions;

    public Plot GetPlot(uint id)
    {
        return _plots.TryGetValue(id, out var plot) ? plot : null;
    }

    public PlotEventTemplate GetEventByPlotId(uint plotId)
    {
        return _plots.TryGetValue(plotId, out var plot) ? plot.EventTemplate : null;
    }

    public void Load()
    {
        if (_loaded)
            return;

        _plots = new Dictionary<uint, Plot>();
        _eventTemplates = new Dictionary<uint, PlotEventTemplate>();
        _conditions = new Dictionary<uint, PlotCondition>();
        //_aoeConditions = new Dictionary<uint, PlotAoeCondition>();
        using (var connection = SQLite.CreateConnection())
        {
            Logger.Info("Loading plots...");
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM plots";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new Plot();
                        template.Id = reader.GetUInt32("id");
                        template.TargetTypeId = reader.GetUInt32("target_type_id");
                        _plots.Add(template.Id, template);
                    }
                }
            }

            Logger.Info("Loaded {0} plots", _plots.Count);

            Logger.Info("Loading plot events...");
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM plot_events";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new PlotEventTemplate();
                        template.Id = reader.GetUInt32("id");
                        template.AoeDiminishing = reader.GetBoolean("aoe_diminishing", true);
                        template.Name = reader.GetString("name");
                        template.OnlyDieUnit = reader.GetBoolean("only_die_unit", true);
                        template.OnlyMyPet = reader.GetBoolean("only_my_pet", true);
                        template.OnlyMySlave = reader.GetBoolean("only_my_slave", true);
                        template.OnlyPetOwner = reader.GetBoolean("only_pet_owner", true);
                        template.PlotId = reader.GetUInt32("plot_id");
                        template.Position = reader.GetInt32("position");
                        template.SourceUpdateMethodId = reader.GetUInt32("source_update_method_id");
                        template.TargetUpdateMethodId = reader.GetUInt32("target_update_method_id");
                        template.TargetUpdateMethodParam1 = reader.GetInt32("target_update_method_param1");
                        template.TargetUpdateMethodParam2 = reader.GetInt32("target_update_method_param2");
                        template.TargetUpdateMethodParam3 = reader.GetInt32("target_update_method_param3");
                        template.TargetUpdateMethodParam4 = reader.GetInt32("target_update_method_param4");
                        template.TargetUpdateMethodParam5 = reader.GetInt32("target_update_method_param5");
                        template.TargetUpdateMethodParam6 = reader.GetInt32("target_update_method_param6");
                        template.TargetUpdateMethodParam7 = reader.GetInt32("target_update_method_param7");
                        template.TargetUpdateMethodParam8 = reader.GetInt32("target_update_method_param8");
                        template.TargetUpdateMethodParam9 = reader.GetInt32("target_update_method_param9");
                        template.TargetUpdateMethodParam10 = reader.GetInt32("target_update_method_param10");
                        template.TargetUpdateMethodParam11 = reader.GetInt32("target_update_method_param11");
                        template.Tickets = reader.GetInt32("tickets");

                        _eventTemplates.Add(template.Id, template);

                        if (template.Position == 1 && _plots.TryGetValue(template.PlotId, out var plot))
                            plot.EventTemplate = template;
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM plot_conditions";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new PlotCondition();
                        template.Id = reader.GetUInt32("id");
                        template.Kind = (PlotConditionType)reader.GetInt32("kind_id");
                        template.KindId = reader.GetInt32("kind_id");
                        template.NotCondition = reader.GetBoolean("not_condition", true);
                        template.OrUnitReqs = reader.GetBoolean("or_unit_reqs", true);
                        template.Param1 = reader.GetInt32("param1");
                        template.Param2 = reader.GetInt32("param2");
                        template.Param3 = reader.GetInt32("param3");
                        template.Param4 = reader.GetInt32("param4");

                        _conditions.Add(template.Id, template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM plot_event_conditions";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        //var id = reader.GetUInt32("event_id");
                        //var condId = reader.GetUInt32("condition_id");
                        var template = new PlotEventCondition();
                        template.ConditionId = reader.GetUInt32("condition_id");
                        template.Condition = _conditions[template.ConditionId];
                        template.EventId = reader.GetUInt32("event_id");
                        template.NotifyFailure = reader.GetBoolean("notify_failure", true);
                        template.Position = reader.GetInt32("position");
                        template.SourceId = (PlotEffectSource)reader.GetInt32("source_id");
                        template.TargetId = (PlotEffectTarget)reader.GetInt32("target_id");

                        var plotEvent = _eventTemplates[template.EventId];

                        if (plotEvent.Conditions.Count > 0)
                        {
                            var res = false;
                            for (var node = plotEvent.Conditions.First; node != null; node = node.Next)
                                if (node.Value.Position > template.Position)
                                {
                                    plotEvent.Conditions.AddBefore(node, template);
                                    res = true;
                                    break;
                                }

                            if (!res)
                                plotEvent.Conditions.AddLast(template);
                        }
                        else
                            plotEvent.Conditions.AddFirst(template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM plot_aoe_conditions";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        //var id = reader.GetUInt32("event_id");
                        //var condId = reader.GetUInt32("condition_id");
                        var template = new PlotAoeCondition();
                        template.Id = reader.GetUInt32("id");
                        template.ConditionId = reader.GetUInt32("condition_id");
                        template.EventId = reader.GetUInt32("event_id");
                        if (_conditions.TryGetValue(template.ConditionId, out var condition))
                        {
                            template.Condition = condition;
                            template.Position = reader.GetInt32("position");

                            var plotEvent = _eventTemplates[template.EventId];

                            if (plotEvent.AoeConditions.Count > 0)
                            {
                                var res = false;
                                for (var node = plotEvent.AoeConditions.First; node != null; node = node.Next)
                                    if (node.Value.Position > template.Position)
                                    {
                                        plotEvent.AoeConditions.AddBefore(node, template);
                                        res = true;
                                        break;
                                    }

                                if (!res)
                                    plotEvent.AoeConditions.AddLast(template);
                            }
                            else
                                plotEvent.AoeConditions.AddFirst(template);
                        }
                        else
                        {
                            Logger.Warn($"Plot condition: {template.ConditionId} not found");
                        }
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM plot_effects";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        //var id = reader.GetUInt32("event_id");
                        var template = new PlotEventEffect();
                        template.Id = reader.GetInt32("id");
                        template.ActualType = reader.GetString("actual_type");
                        template.ActualId = reader.GetUInt32("actual_id");
                        template.EventId = reader.GetUInt32("event_id");
                        template.Position = reader.GetInt32("position");
                        template.SourceId = (PlotEffectSource)reader.GetInt32("source_id");
                        template.TargetId = (PlotEffectTarget)reader.GetInt32("target_id");

                        var evnt = _eventTemplates[template.EventId];

                        if (evnt.Effects.Count > 0)
                        {
                            var res = false;
                            for (var node = evnt.Effects.First; node != null; node = node.Next)
                                if (node.Value.Position > template.Position)
                                {
                                    evnt.Effects.AddBefore(node, template);
                                    res = true;
                                    break;
                                }

                            if (!res)
                                evnt.Effects.AddLast(template);
                        }
                        else
                            evnt.Effects.AddFirst(template);
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM plot_next_events";
                command.Prepare();
                using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                {
                    while (reader.Read())
                    {
                        var template = new PlotNextEvent();
                        //var id = reader.GetUInt32("event_id");
                        //var nextId = reader.GetUInt32("next_event_id");
                        template.Id = reader.GetUInt32("id");
                        template.AddAnimCsTime = reader.GetBoolean("add_anim_cs_time", true);
                        template.CancelOnBigHit = reader.GetBoolean("cancel_on_big_hit", true);
                        template.Casting = reader.GetBoolean("casting", true);
                        template.CastingCancelable = reader.GetBoolean("casting_cancelable", true);
                        template.CastingDelayable = reader.GetBoolean("casting_delayable", true);
                        template.CastingInc = reader.GetInt32("casting_inc");
                        template.CastingUseable = reader.GetBoolean("casting_useable", true);
                        template.Channeling = reader.GetBoolean("channeling", true);
                        template.Delay = reader.GetInt32("delay");
                        template.EventId = reader.GetUInt32("event_id");
                        template.Fail = reader.GetBoolean("fail", true);
                        template.HighAbilityResource = reader.GetBoolean("high_ability_resource", true);
                        template.NextEventId = reader.GetUInt32("next_event_id");
                        template.Event = _eventTemplates[template.NextEventId];
                        template.PerTarget = reader.GetBoolean("per_target", true);
                        template.Position = reader.GetInt32("position");
                        template.Speed = reader.GetInt32("speed");
                        template.UseExeTime = reader.GetBoolean("use_exe_time", true);
                        template.Weight = reader.GetInt32("weight");

                        var plotEvent = _eventTemplates[template.EventId];

                        if (plotEvent.NextEvents.Count > 0)
                        {
                            var res = false;
                            for (var node = plotEvent.NextEvents.First; node != null; node = node.Next)
                                if (node.Value.Position > template.Position)
                                {
                                    plotEvent.NextEvents.AddBefore(node, template);
                                    res = true;
                                    break;
                                }

                            if (!res)
                                plotEvent.NextEvents.AddLast(template);
                        }
                        else
                            plotEvent.NextEvents.AddFirst(template);
                    }
                }
            }

            Logger.Info("Loaded {0} plot events", _eventTemplates.Count);

            foreach (var plot in _plots.Values.Where(plot => plot.EventTemplate != null))
            {
                plot.Tree = PlotBuilder.BuildTree(plot.Id);
            }
            // Task.Run(() => flameboltTree.Execute(new PlotState()));
        }

        _loaded = true;
    }
}
