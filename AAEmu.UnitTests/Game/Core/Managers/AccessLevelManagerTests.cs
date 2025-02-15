﻿using System.Collections.Generic;
using System.Reflection;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models;
using Xunit;

namespace AAEmu.UnitTests.Game.Core.Managers
{
    public class AccessLevelManagerTests
    {
        private readonly AccessLevelManager _manager;

        public AccessLevelManagerTests()
        {
            _manager = AccessLevelManager.Instance;
            ResetManagerState();
            ResetAppConfiguration();
        }

        private void ResetManagerState()
        {
            var cmdField = typeof(AccessLevelManager).GetField("CMD", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            cmdField.SetValue(_manager, new List<Command>());
        }

        private void ResetAppConfiguration()
        {
            // Исправляем тип на Dictionary<string, int>
            var config = AppConfiguration.Instance;
            var accessLevelProperty = config.GetType().GetProperty("AccessLevel");
            accessLevelProperty.SetValue(config, new Dictionary<string, int>());
        }

        [Fact]
        public void GetLevel_WhenCommandNotExists_ShouldReturnDefaultLevel()
        {
            _manager.Load();
            var result = _manager.GetLevel("non_existent_command");
            Assert.Equal(100, result);
        }

        [Fact]
        public void GetLevel_WhenCommandExists_ShouldReturnCorrectLevel()
        {
            var config = AppConfiguration.Instance;
            var accessLevel = config.AccessLevel as Dictionary<string, int>;
            
            // Используем добавление через словарь
            accessLevel.Add("test_command", 5);
            
            _manager.Load();
            var result = _manager.GetLevel("test_command");
            Assert.Equal(5, result);
        }

        [Fact]
        public void Load_ShouldLoadMultipleCommandsCorrectly()
        {
            var config = AppConfiguration.Instance;
            var accessLevel = config.AccessLevel as Dictionary<string, int>;
            
            // Добавляем элементы в словарь
            accessLevel["cmd1"] = 1;
            accessLevel["cmd2"] = 2;
            accessLevel["cmd3"] = 3;

            _manager.Load();
            Assert.Equal(1, _manager.GetLevel("cmd1"));
            Assert.Equal(2, _manager.GetLevel("cmd2"));
            Assert.Equal(3, _manager.GetLevel("cmd3"));
        }

        [Fact]
        public void Load_WhenDuplicateCommands_ShouldOverwriteLevel()
        {
            var config = AppConfiguration.Instance;
            var accessLevel = config.AccessLevel as Dictionary<string, int>;
            
            // В словаре дубликаты перезаписывают значение
            accessLevel["duplicate"] = 5;
            accessLevel["duplicate"] = 10; // Перезапись

            _manager.Load();
            Assert.Equal(10, _manager.GetLevel("duplicate")); // Ожидаем последнее значение
        }

        [Fact]
        public void Load_WhenEmptyConfig_ShouldNotLoadCommands()
        {
            _manager.Load();
            Assert.Equal(100, _manager.GetLevel("any_command"));
        }
    }
}
