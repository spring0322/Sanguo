using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Collections.Concurrent;
using Zhsan.GameLogic.Infra; // 引用 Context 命名空间

namespace Zhsan.GameLogic.Config
{
    public static class ConfigManager
    {
        private const string AI_CONFIG_FILE = "ai_strategy_config.json";
        private const string BALANCE_CONFIG_FILE = "game_balance.json";
        private const string TROOP_CONFIG_FILE = "troop_config.json";
        private const string PERSON_CONFIG_FILE = "person_config.json";

        private static readonly string _dataDir;

        // Configurations with thread-safe access
        private static AIStrategicConfig _aiConfig;
        private static GameBalanceConfig _balanceConfig;
        private static TroopConfig _troopConfig;
        private static PersonConfig _personConfig;

        public static AIStrategicConfig AI => _aiConfig;
        public static GameBalanceConfig Balance => _balanceConfig;
        public static TroopConfig Troop => _troopConfig;
        public static PersonConfig Person => _personConfig;

        // File watchers
        private static ConcurrentDictionary<string, FileSystemWatcher> _watchers = new();

        static ConfigManager()
        {
            _dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "Data");
            if (!Directory.Exists(_dataDir))
            {
                Directory.CreateDirectory(_dataDir);
            }

            // Initial Load
            LoadAll();

            // Setup Watchers
            SetupWatchers();
        }

        private static void LoadAll()
        {
            LoadConfig(AI_CONFIG_FILE, ref _aiConfig, AIStrategicConfigContext.Default.AIStrategicConfig);
            LoadConfig(BALANCE_CONFIG_FILE, ref _balanceConfig, AIStrategicConfigContext.Default.GameBalanceConfig);
            LoadConfig(TROOP_CONFIG_FILE, ref _troopConfig, AIStrategicConfigContext.Default.TroopConfig);
            LoadConfig(PERSON_CONFIG_FILE, ref _personConfig, AIStrategicConfigContext.Default.PersonConfig);
        }

        private static void LoadConfig<T>(string filename, ref T configField, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo) where T : class, new()
        {
            string filePath = Path.Combine(_dataDir, filename);

            if (!File.Exists(filePath))
            {
                var defaultConfig = new T();
                SaveConfig(filePath, defaultConfig, typeInfo);
                Interlocked.Exchange(ref configField, defaultConfig);
                return;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var newConfig = JsonSerializer.Deserialize(json, typeInfo);
                Interlocked.Exchange(ref configField, newConfig ?? new T());
                System.Diagnostics.Debug.WriteLine($"[ConfigManager] Loaded {filename}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigManager] Failed to load {filename}: {ex.Message}");
                if (configField == null)
                {
                    Interlocked.Exchange(ref configField, new T());
                }
            }
        }

        private static void SaveConfig<T>(string filePath, T config, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
        {
            try
            {
                var json = JsonSerializer.Serialize(config, typeInfo);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigManager] Failed to save {filePath}: {ex.Message}");
            }
        }

        private static void SetupWatchers()
        {
            SetupWatcher(AI_CONFIG_FILE, () => LoadConfig(AI_CONFIG_FILE, ref _aiConfig, AIStrategicConfigContext.Default.AIStrategicConfig));
            SetupWatcher(BALANCE_CONFIG_FILE, () => LoadConfig(BALANCE_CONFIG_FILE, ref _balanceConfig, AIStrategicConfigContext.Default.GameBalanceConfig));
            SetupWatcher(TROOP_CONFIG_FILE, () => LoadConfig(TROOP_CONFIG_FILE, ref _troopConfig, AIStrategicConfigContext.Default.TroopConfig));
            SetupWatcher(PERSON_CONFIG_FILE, () => LoadConfig(PERSON_CONFIG_FILE, ref _personConfig, AIStrategicConfigContext.Default.PersonConfig));
        }

        private static void SetupWatcher(string filename, Action reloadAction)
        {
            try
            {
                var watcher = new FileSystemWatcher(_dataDir, filename);
                watcher.NotifyFilter = NotifyFilters.LastWrite;

                DateTime lastRead = DateTime.MinValue;

                watcher.Changed += (s, e) =>
                {
                    var now = DateTime.Now;
                    if ((now - lastRead).TotalMilliseconds < 500) return;
                    lastRead = now;

                    Thread.Sleep(50); // Allow file write to complete
                    reloadAction();
                };

                watcher.EnableRaisingEvents = true;
                _watchers[filename] = watcher;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigManager] Watcher setup failed for {filename}: {ex.Message}");
            }
        }
    }
}
