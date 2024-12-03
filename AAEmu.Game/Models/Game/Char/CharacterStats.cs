using System;
using System.Collections.Generic;
using System.Linq;

using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Tasks;

using MySql.Data.MySqlClient;

using NLog;

namespace AAEmu.Game.Models.Game.Char;

public class CharacterStats
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const int BaseLimit = 200;
    private const int MaxLimit = 100; // 300 - 200
    private const int Points = 20;
    private int _currentLimit;
    private int _applyExtendCount;
    private int _attemptsLeftToday;

    public Character Owner { get; }
    public int ApplyNormalCount
    {
        get
        {
            var (_, applyNormalCount, _) = _stats[PageIndex];
            return applyNormalCount;
        }
        private set
        {
            var (_, applyNormalCount, _) = _stats[PageIndex];
            applyNormalCount = value;
        }
    }
    public int ApplySpecialCount
    {
        get
        {
            var (_, _, applySpecialCount) = _stats[PageIndex];
            return applySpecialCount;
        }
        private set
        {
            var (_, _, applySpecialCount) = _stats[PageIndex];
            applySpecialCount = value;
        }
    }

    public uint PageIndex { get; set; }
    public int PageCount { get; set; } = 1; // Current number of pages 1..3
    public int PageMax { get; } = 3; // Maximum number of pages 3

    public int ExtendMaxStats
    {
        get => _currentLimit;
        private set => _currentLimit = value;
    }

    public int ApplyExtendCount
    {
        get => _applyExtendCount;
        private set => _applyExtendCount = value;
    }

    private readonly Dictionary<uint, (int[] Stats, int ApplyNormalCount, int ApplySpecialCount)> _stats = new();
    private DateTime _lastResetTime;

    // Добавляем поля для атрибутов
    public int Str { get; set; }
    public int Dex { get; set; }
    public int Sta { get; set; }
    public int Int { get; set; }
    public int Spi { get; set; }

    public CharacterStats(Character owner)
    {
        Owner = owner;
        _currentLimit = 0;
        _attemptsLeftToday = 1;
        _lastResetTime = GetNextResetTime();
        StartResetTimer();
        InitializeStats();
    }

    private void InitializeStats()
    {
        for (uint i = 0; i < 3; i++)
        {
            _stats[i] = (new int[5], 0, 0);
        }
    }

    public bool UseSphereNormal()
    {
        if (_attemptsLeftToday <= 0) return false;

        _attemptsLeftToday--;
        ApplyNormalCount++;
        return true;
    }

    public bool UseSphereSpecial()
    {
        ApplySpecialCount++;
        return false;
    }

    private void ResetAttempts()
    {
        _attemptsLeftToday = 1;
        ApplyNormalCount = 0;
        ApplySpecialCount = 0;
        _lastResetTime = GetNextResetTime();
        StartResetTimer(); // Restart the timer after reset
    }

    private static DateTime GetNextResetTime()
    {
        var now = DateTime.UtcNow;
        return now.Date.AddDays(1); // Next day at 00:00 UTC
    }

    private void StartResetTimer()
    {
        var cronExpression = "0 0 0 * * *"; // Every day at 00:00 UTC
        TaskManager.Instance.CronSchedule(new ResetAttemptsTask(ResetAttempts), cronExpression);
    }

    public void IncreaseLimit()
    {
        if (_currentLimit + Points > MaxLimit)
        {
            Logger.Debug($"Player: {Owner.Name}. Cannot increase the limit above the maximum value.");
            return;
        }

        _currentLimit += Points;
        _applyExtendCount++;
        Logger.Debug($"Player: {Owner.Name}. Limit increased by {Points} points. Increased times: {_applyExtendCount}. Current limit: {_currentLimit + BaseLimit} points.");
    }

    public void Save(MySqlConnection connection, MySqlTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Connection = connection;
        command.Transaction = transaction;
        command.CommandText = GenerateSaveCommandText();
        AddParameters(command);
        command.Prepare();
        command.ExecuteNonQuery();
    }

    private string GenerateSaveCommandText()
    {
        var columns = string.Join(", ", Enumerable.Range(0, 15).Select(i => $"value{i}"));
        var values = string.Join(", ", Enumerable.Range(0, 15).Select(i => $"@value{i}"));
        var updates = string.Join(", ", Enumerable.Range(0, 15).Select(i => $"value{i} = VALUES(value{i})"));
        return $@"
            INSERT INTO character_stats (character_id, PageIndex, {columns}, ApplyNormalCount0, ApplySpecialCount0, ApplyNormalCount1, ApplySpecialCount1, ApplyNormalCount2, ApplySpecialCount2, PageCount, ApplyExtendCount, ExtendMaxStats)
            VALUES (@characterId, @pageIndex, {values}, @applyNormalCount0, @applySpecialCount0, @applyNormalCount1, @applySpecialCount1, @applyNormalCount2, @applySpecialCount2, @pageCount, @applyExtendCount, @extendMaxStats)
            ON DUPLICATE KEY UPDATE 
                character_id = VALUES(character_id),
                PageIndex = VALUES(PageIndex),
                {updates},
                ApplyNormalCount0 = VALUES(ApplyNormalCount0),
                ApplySpecialCount0 = VALUES(ApplySpecialCount0),
                ApplyNormalCount1 = VALUES(ApplyNormalCount1),
                ApplySpecialCount1 = VALUES(ApplySpecialCount1),
                ApplyNormalCount2 = VALUES(ApplyNormalCount2),
                ApplySpecialCount2 = VALUES(ApplySpecialCount2),
                PageCount = VALUES(PageCount),
                ApplyExtendCount = VALUES(ApplyExtendCount),
                ExtendMaxStats = VALUES(ExtendMaxStats)";
    }

    private void AddParameters(MySqlCommand command)
    {
        command.Parameters.AddWithValue("@characterId", Owner.Id);
        command.Parameters.AddWithValue("@pageIndex", PageIndex);
        for (uint i = 0; i < 15; i++)
        {
            command.Parameters.Add($"@value{i}", MySqlDbType.Int32).Value = _stats[i / 5].Stats[i % 5];
        }
        command.Parameters.AddWithValue("@applyNormalCount0", _stats[0].ApplyNormalCount);
        command.Parameters.AddWithValue("@applySpecialCount0", _stats[0].ApplySpecialCount);
        command.Parameters.AddWithValue("@applyNormalCount1", _stats[1].ApplyNormalCount);
        command.Parameters.AddWithValue("@applySpecialCount1", _stats[1].ApplySpecialCount);
        command.Parameters.AddWithValue("@applyNormalCount2", _stats[2].ApplyNormalCount);
        command.Parameters.AddWithValue("@applySpecialCount2", _stats[2].ApplySpecialCount);
        command.Parameters.AddWithValue("@pageCount", PageCount);
        command.Parameters.AddWithValue("@applyExtendCount", ApplyExtendCount);
        command.Parameters.AddWithValue("@extendMaxStats", ExtendMaxStats);
    }

    public void Load(MySqlConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = GenerateLoadCommandText();
        command.Parameters.AddWithValue("@characterId", Owner.Id);
        command.Prepare();

        using var reader = command.ExecuteReader();
        if (!reader.Read()) return;

        PageIndex = reader.GetUInt32("PageIndex");
        for (uint i = 0; i < 3; i++)
        {
            var stats = _stats[i];
            for (uint j = 0; j < 5; j++)
            {
                stats.Stats[j] = reader.GetInt32($"value{i * 5 + j}");
            }
            stats.ApplyNormalCount = reader.GetInt32($"ApplyNormalCount{i}");
            stats.ApplySpecialCount = reader.GetInt32($"ApplySpecialCount{i}");
            _stats[i] = stats; // Обновляем значение в словаре
        }

        ApplyNormalCount = _stats[PageIndex].ApplyNormalCount;
        ApplySpecialCount = _stats[PageIndex].ApplySpecialCount;
        PageCount = reader.GetInt32("PageCount") == 0 ? 1 : reader.GetInt32("PageCount");
        ApplyExtendCount = reader.GetInt32("ApplyExtendCount");
        ExtendMaxStats = reader.GetInt32("ExtendMaxStats");
    }

    private string GenerateLoadCommandText()
    {
        var columns = string.Join(", ", Enumerable.Range(0, 15).Select(i => $"value{i}"));
        return $@"
            SELECT PageIndex, {columns}, ApplyNormalCount0, ApplySpecialCount0, ApplyNormalCount1, ApplySpecialCount1, ApplyNormalCount2, ApplySpecialCount2, PageCount, ApplyExtendCount, ExtendMaxStats
            FROM character_stats WHERE character_id = @characterId";
    }

    public int GetApplyNormalCountByPageIndex(uint pageIndex)
    {
        if (pageIndex is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex), "Page index must be between 0 and 2.");
        }

        var stats = _stats[pageIndex];
        return stats.ApplyNormalCount;
    }

    public int GetApplySpecialCountByPageIndex(uint pageIndex)
    {
        if (pageIndex is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex), "Page index must be between 0 and 2.");
        }

        var stats = _stats[pageIndex];
        return stats.ApplySpecialCount;
    }

    public int[] GetStatsByPageIndex(uint pageIndex)
    {
        if (pageIndex is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex), "Page index must be between 0 and 2.");
        }

        return _stats.TryGetValue(pageIndex, out var stats) ? stats.Stats : new int[5];
    }

    public void UpdateStatsByPageIndex(uint pageIndex, int[] newStats, int applyNormalCount, int applySpecialCount)
    {
        if (pageIndex is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex), "Page index must be between 0 and 2.");
        }

        if (newStats.Length != 5)
        {
            throw new ArgumentException("New stats array must contain exactly 5 elements.", nameof(newStats));
        }

        _stats[pageIndex] = (newStats, applyNormalCount, applySpecialCount);
    }

    public void ResetStatsByPageIndex(uint pageIndex)
    {
        if (pageIndex is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex), "Page index must be between 0 and 2.");
        }

        _stats[pageIndex] = (new int[5], 0, 0);
    }
}
