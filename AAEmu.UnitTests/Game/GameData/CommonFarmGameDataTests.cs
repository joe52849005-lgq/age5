using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.CommonFarm.Static;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AAEmu.UnitTests.Game.GameData;

public class CommonFarmGameDataTests
{
    private void SetupDatabase(SqliteConnection connection)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                    CREATE TABLE farm_groups (
                        id INTEGER PRIMARY KEY,
                        count INTEGER
                    );
                    CREATE TABLE farm_group_doodads (
                        id INTEGER PRIMARY KEY,
                        farm_group_id INTEGER,
                        doodad_id INTEGER,
                        item_id INTEGER
                    );
                    CREATE TABLE doodad_groups (
                        id INTEGER PRIMARY KEY,
                        guard_on_field_time INTEGER,
                        is_export BOOLEAN,
                        removed_by_house BOOLEAN
                    );
                    CREATE TABLE common_farms (
                        id INTEGER PRIMARY KEY,
                        comments TEXT,
                        farm_group_id INTEGER,
                        guard_time INTEGER,
                        name TEXT
                    );
                    INSERT INTO farm_groups (id, count) VALUES (1, 10);
                    INSERT INTO farm_group_doodads (id, farm_group_id, doodad_id, item_id) VALUES (1, 1, 1, 1);
                    INSERT INTO doodad_groups (id, guard_on_field_time, is_export, removed_by_house) VALUES (1, 100, 1, 0);
                    INSERT INTO common_farms (id, comments, farm_group_id, guard_time, name) VALUES (1, 'Test comments', 1, 100, 'Test Farm');
                ";
            command.ExecuteNonQuery();
        }
    }

    [Fact]
    public void GetCommonFarmById_ReturnsCorrectFarm()
    {
        // Arrange
        var commonFarmGameData = CommonFarmGameData.Instance;
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        SetupDatabase(connection);

        // Act
        commonFarmGameData.Load(connection, null);
        var farm = commonFarmGameData.GetCommonFarmById(1);

        // Assert
        Assert.NotNull(farm);
        Assert.Equal(1u, farm.Id);
        Assert.Equal("Test comments", farm.Comments);
        Assert.Equal(1u, farm.FarmGroupId);
        Assert.Equal(100, farm.GuardTime);
        Assert.Equal("Test Farm", farm.Name);

        connection.Close();
    }

    [Fact]
    public void GetFarmGroupMaxCount_ReturnsCorrectCount()
    {
        // Arrange
        var commonFarmGameData = CommonFarmGameData.Instance;
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        SetupDatabase(connection);

        // Act
        commonFarmGameData.Load(connection, null);
        var count = commonFarmGameData.GetFarmGroupMaxCount(FarmType.Farm);

        // Assert
        Assert.Equal(10u, count);

        connection.Close();
    }

    [Fact]
    public void GetDoodadGuardTime_ReturnsCorrectGuardTime()
    {
        // Arrange
        var commonFarmGameData = CommonFarmGameData.Instance;
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        SetupDatabase(connection);

        // Act
        commonFarmGameData.Load(connection, null);
        var guardTime = commonFarmGameData.GetDoodadGuardTime(1);

        // Assert
        Assert.Equal(100, guardTime);

        connection.Close();
    }

    [Fact]
    public void GetAllowedDoodads_ReturnsCorrectDoodads()
    {
        // Arrange
        var commonFarmGameData = CommonFarmGameData.Instance;
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        SetupDatabase(connection);

        // Act
        commonFarmGameData.Load(connection, null);
        var doodads = commonFarmGameData.GetAllowedDoodads0(FarmType.Farm);

        // Assert
        Assert.Single(doodads);
        Assert.Contains(1u, doodads);

        connection.Close();
    }

    [Fact]
    public void PostLoad_DoesNothing()
    {
        // Arrange
        var commonFarmGameData = CommonFarmGameData.Instance;

        // Act
        commonFarmGameData.PostLoad();

        // Assert
        // No assertions needed as PostLoad does nothing
    }
}
