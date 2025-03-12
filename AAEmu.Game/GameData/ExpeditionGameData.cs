using System.Collections.Concurrent;

using AAEmu.Commons.Utils;
using AAEmu.Game.GameData.Framework;
using AAEmu.Game.Models.Game.Expeditions;
using AAEmu.Game.Models.Game.Family;
using AAEmu.Game.Utils.DB;

using Microsoft.Data.Sqlite;

namespace AAEmu.Game.GameData;

[GameData]
public class ExpeditionGameData : Singleton<ExpeditionGameData>, IGameDataLoader
{
    public static ConcurrentDictionary<int, ExpeditionLevelBuff> ExpeditionLevelBuffs = new ConcurrentDictionary<int, ExpeditionLevelBuff>();
    public static ConcurrentDictionary<int, ExpeditionLevelRequirement> ExpeditionLevelRequirements = new ConcurrentDictionary<int, ExpeditionLevelRequirement>();
    public static ConcurrentDictionary<int, ExpeditionLevel> ExpeditionLevels = new ConcurrentDictionary<int, ExpeditionLevel>();

    
    public static uint? GetBuffIdByLevelId(uint ExpeditionLevelId)
    {
        var id = (int)ExpeditionLevelId + 6;
        if (ExpeditionLevelBuffs.TryGetValue(id, out var buff))
        {
            return (uint?)buff.BuffId;
        }
        return null;
    }

    public void Load(SqliteConnection connection, SqliteConnection connection2)
    {
        LoadExpeditionLevels(connection, connection2);
        LoadExpeditionLevelBuffs(connection, connection2);
        LoadExpeditionLevelRequirements(connection, connection2);
    }

    private static void LoadExpeditionLevelBuffs(SqliteConnection connection, SqliteConnection connection2)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM expedition_level_buffs";
        command.Prepare();
        using var sqliteReader = command.ExecuteReader();
        using var reader = new SQLiteWrapperReader(sqliteReader);
        while (reader.Read())
        {
            var buff = new ExpeditionLevelBuff();
            buff.Id = reader.GetInt32("id");
            buff.BuffId = reader.GetInt32("buff_id");
            buff.ExpeditionLevelId = reader.GetInt32("expedition_level_id");

            ExpeditionLevelBuffs.TryAdd(buff.Id, buff);
        }
    }

    private static void LoadExpeditionLevelRequirements(SqliteConnection connection, SqliteConnection connection2)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM expedition_level_reqs";
        command.Prepare();
        using var sqliteReader = command.ExecuteReader();
        using var reader = new SQLiteWrapperReader(sqliteReader);
        while (reader.Read())
        {
            var requirement = new ExpeditionLevelRequirement();
            requirement.Id = reader.GetInt32("id");
            requirement.Amount = reader.GetInt32("amount");
            requirement.ExpeditionLevelId = reader.GetInt32("expedition_level_id");
            requirement.ItemId = reader.GetInt32("item_id");

            ExpeditionLevelRequirements.TryAdd(requirement.Id, requirement);
        }
    }

    private static void LoadExpeditionLevels(SqliteConnection connection, SqliteConnection connection2)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM expedition_levels";
        command.Prepare();
        using var sqliteReader = command.ExecuteReader();
        using var reader = new SQLiteWrapperReader(sqliteReader);
        while (reader.Read())
        {
            var level = new ExpeditionLevel();
            level.Id = reader.GetInt32("id");
            level.DailyExp = reader.GetInt32("daily_exp");
            level.MemberLimit = reader.GetInt32("member_limit");
            level.NeedMoney = reader.GetInt32("need_money");
            level.SimilarBuffTagId = reader.GetInt32("similar_buff_tag_id");
            level.SummonLimit = reader.GetInt32("summon_limit");
            level.TotalExp = reader.GetInt32("total_exp");

            ExpeditionLevels.TryAdd(level.Id, level);
        }
    }
    public void PostLoad()
    {
    }
}
