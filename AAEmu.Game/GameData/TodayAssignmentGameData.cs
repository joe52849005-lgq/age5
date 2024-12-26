using System.Collections.Concurrent;

using AAEmu.Commons.Utils;
using AAEmu.Game.GameData.Framework;
using AAEmu.Game.Models.Game.TodayAssignments;
using AAEmu.Game.Utils.DB;

using Microsoft.Data.Sqlite;

namespace AAEmu.Game.GameData
{
    [GameData]
    public class TodayAssignmentGameData : Singleton<TodayAssignmentGameData>, IGameDataLoader
    {
        //private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        // TodayAssignment
        private ConcurrentDictionary<int, TodayQuestStep> _todayQuestSteps;
        private ConcurrentDictionary<int, TodayQuestGroup> _todayQuestGroups;
        private ConcurrentDictionary<int, ConcurrentBag<TodayQuestGroupQuest>> _todayQuestGroupQuests;

        #region TodayAssignment

        // по realStep из таблицы today_quest_steps берем today_quest_step_id
        public int GetTodayQuestStepId(int realStep)
        {
            var value = 0;
            foreach (var todayQuestStep in _todayQuestSteps)
            {
                if (todayQuestStep.Value.RealStep == realStep)
                {
                    value = todayQuestStep.Key;
                    break;
                }
            }

            return value;
        }

        public int GetTodayQuestGroupId(int todayQuestStepId)
        {
            var value = 0;
            foreach (var todayQuestGroup in _todayQuestGroups)
            {
                if (todayQuestGroup.Value.StepId == todayQuestStepId)
                {
                    value = todayQuestGroup.Key;
                    break;
                }
            }

            return value;
        }

        public int GetTodayQuestGroupQuestContextId(int todayQuestGroupId)
        {
            var value = 0;
            if (_todayQuestGroupQuests.TryGetValue(todayQuestGroupId, out var todayQuestGroupQuests))
            {
                foreach (var todayQuestGroupQuest in todayQuestGroupQuests)
                {
                    if (todayQuestGroupQuest.TodayQuestGroupId == todayQuestGroupId)
                    {
                        value = todayQuestGroupQuest.QuestContextId;
                        break;
                    }
                }
            }

            return value;
        }

        #endregion TodayAssignment

        #region Sqlite
        public void Load(SqliteConnection connection, SqliteConnection connection2)
        {
            #region TodayAssignment

            InitializeDictionaries();
            LoadTodayQuestSteps(connection);
            LoadTodayQuestGroups(connection);
            LoadTodayQuestGroupQuests(connection);

            #endregion TodayAssignment
        }

        private void InitializeDictionaries()
        {
            #region TodayAssignment
            _todayQuestSteps = new ConcurrentDictionary<int, TodayQuestStep>();
            _todayQuestGroups = new ConcurrentDictionary<int, TodayQuestGroup>();
            _todayQuestGroupQuests = new ConcurrentDictionary<int, ConcurrentBag<TodayQuestGroupQuest>>();
            #endregion TodayAssignment
        }

        #region TodayAssignment
        private void LoadTodayQuestSteps(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM today_quest_steps";
            command.Prepare();
            using var sqliteReader = command.ExecuteReader();
            using var reader = new SQLiteWrapperReader(sqliteReader);
            while (reader.Read())
            {
                var step = new TodayQuestStep();
                step.Id = reader.GetInt32("id");
                step.Description = reader.GetString("description");
                step.ExpeditionLevelMax = reader.GetInt32("expedition_level_max");
                step.ExpeditionLevelMin = reader.GetInt32("expedition_level_min");
                step.ExpeditionOnly = reader.GetBoolean("expedition_only");
                step.FamilyLevelMax = reader.GetInt32("family_level_max");
                step.FamilyLevelMin = reader.GetInt32("family_level_min");
                step.FamilyOnly = reader.GetBoolean("family_only");
                step.IconId = reader.GetInt32("icon_id");
                step.ItemNum = reader.GetInt32("item_num");
                step.ItemId = reader.GetInt32("item_id");
                step.Name = reader.GetString("name");
                step.OrUnitReqs = reader.GetBoolean("or_unit_reqs");
                step.RealStep = reader.GetInt32("real_step");

                _todayQuestSteps[step.Id] = step;
            }
        }

        private void LoadTodayQuestGroups(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM today_quest_groups";
            command.Prepare();
            using var sqliteReader = command.ExecuteReader();
            using var reader = new SQLiteWrapperReader(sqliteReader);
            while (reader.Read())
            {
                var group = new TodayQuestGroup();
                group.Id = reader.GetInt32("id");
                group.AutomaticRestart = reader.GetBoolean("automatic_restart");
                group.Description = reader.GetString("description");
                group.ExpeditionLevelMax = reader.GetInt32("expedition_level_max");
                group.ExpeditionLevelMin = reader.GetInt32("expedition_level_min");
                group.Name = reader.GetString("name");
                group.OrUnitReqs = reader.GetBoolean("or_unit_reqs");
                group.StepId = reader.GetInt32("step_id");

                _todayQuestGroups[group.Id] = group;
            }
        }

        private void LoadTodayQuestGroupQuests(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM today_quest_group_quests";
            command.Prepare();
            using var sqliteReader = command.ExecuteReader();
            using var reader = new SQLiteWrapperReader(sqliteReader);
            while (reader.Read())
            {
                var groupQuest = new TodayQuestGroupQuest();
                groupQuest.TodayQuestGroupId = reader.GetInt32("today_quest_group_id");
                groupQuest.QuestContextId = reader.GetInt32("quest_context_id");

                if (!_todayQuestGroupQuests.ContainsKey(groupQuest.TodayQuestGroupId))
                {
                    _todayQuestGroupQuests[groupQuest.TodayQuestGroupId] = [];
                }
                _todayQuestGroupQuests[groupQuest.TodayQuestGroupId].Add(groupQuest);
            }
        }


        #endregion TodayAssignment

        public void PostLoad()
        {
            // Handle any post-loading logic here
        }
        #endregion Sqlite
    }
}
