using System;
using System.IO;
using Newtonsoft.Json;
using SteamGameServerMod.Logging;

namespace SteamGameServerMod.Settings
{
    public class ConfigManager
    {
        private const string ConfigFileName = "SteamGameServerConfig.json";

        // Config model class
        public class ConfigData
        {
            public bool ServerEnabled { get; set; } = false;
            public string ServerListUrl { get; set; } = "https://scheduleoneservers.com";
        }

        public void LoadConfig()
        {
            try
            {
#if USEMELONLOADER
                var configPath = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, ConfigFileName);
#elif USEBEPINEX
                var configPath = Path.Combine("BepInEx", "config", ConfigFileName);
#endif
                Log.LogInfo($"Attempting to load config from: {configPath}");

                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var config = JsonConvert.DeserializeObject<ConfigData>(json);

                    // Apply settings using the static method
                    ServerSettings.UpdateFromConfig(config.ServerEnabled, config.ServerListUrl);

                    Log.LogInfo("Config loaded successfully");
                    return;
                }

                Log.LogInfo("Config file not found, creating default config");
                var defaultConfig = new ConfigData();
                SaveConfig(defaultConfig);

                // Apply default settings
                ServerSettings.UpdateFromConfig(defaultConfig.ServerEnabled, defaultConfig.ServerListUrl);
            }
            catch (Exception ex)
            {
                Log.LogError($"Error loading config: {ex.Message}");
                Log.LogInfo("Using default settings");

                // Apply default settings
                ServerSettings.UpdateFromConfig(false, "https://scheduleoneservers.com");
            }
        }

        private void SaveConfig(ConfigData config)
        {
            try
            {
#if USEMELONLOADER
                var configPath = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, ConfigFileName);
#elif USEBEPINEX
                var configPath = Path.Combine("BepInEx", "config", ConfigFileName);
#endif

                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configPath, json);
                Log.LogInfo($"Config saved to: {configPath}");
            }
            catch (Exception ex)
            {
                Log.LogError($"Error saving config: {ex.Message}");
            }
        }
    }
}