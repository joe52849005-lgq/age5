using System.Collections.Concurrent;

using AAEmu.Commons.Utils;
using AAEmu.Game.GameData.Framework;
using AAEmu.Game.Models.Game.Family;
using AAEmu.Game.Utils.DB;

using Microsoft.Data.Sqlite;

namespace AAEmu.Game.GameData;

[GameData]
public class FamilyGameData : Singleton<FamilyGameData>, IGameDataLoader
{
    public static ConcurrentDictionary<int, FamilyLevel> FamilyLevels = new ConcurrentDictionary<int, FamilyLevel>();
    public static ConcurrentDictionary<int, FamilyMemberLimit> FamilyMemberLimits = new ConcurrentDictionary<int, FamilyMemberLimit>();

    public static uint? GetBuffIdByLevelId(int levelId)
    {
        if (FamilyLevels.TryGetValue(levelId, out var level))
        {
            return (uint?)level.BuffId;
        }
        return null;
    }

    public void Load(SqliteConnection connection, SqliteConnection connection2)
    {
        LoadFamilyLevels(connection, connection2);
        LoadFamilyMemberLimits(connection, connection2);
    }

    private static void LoadFamilyLevels(SqliteConnection connection, SqliteConnection connection2)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM family_levels";
        command.Prepare();
        using var sqliteReader = command.ExecuteReader();
        using var reader = new SQLiteWrapperReader(sqliteReader);
        while (reader.Read())
        {
            var level = new FamilyLevel();
            level.Id = reader.GetInt32("id");
            level.BuffId = reader.GetInt32("buff_id");
            level.Exp = reader.GetInt32("exp");
            level.GradeName = reader.GetString("grade_name");
            level.Level = reader.GetInt32("level");

            FamilyLevels.TryAdd(level.Id, level);
        }
    }

    private static void LoadFamilyMemberLimits(SqliteConnection connection, SqliteConnection connection2)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM family_member_limits";
        command.Prepare();
        using var sqliteReader = command.ExecuteReader();
        using var reader = new SQLiteWrapperReader(sqliteReader);
        while (reader.Read())
        {
            var limit = new FamilyMemberLimit();
            limit.Id = reader.GetInt32("id");
            limit.Count = reader.GetInt32("count");
            limit.ItemId = reader.GetInt32("item_id");
            limit.ItemCount = reader.GetInt32("item_count");

            FamilyMemberLimits.TryAdd(limit.Id, limit);
        }
    }

    public void PostLoad()
    {
    }
}
