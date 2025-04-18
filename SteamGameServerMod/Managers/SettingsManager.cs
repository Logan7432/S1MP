#if USEMELONLOADER
using MelonLoader.Utils;
using MelonLoader;
#endif
using Newtonsoft.Json;
using SteamGameServerMod.Logging;
using SteamGameServerMod.Settings;

namespace SteamGameServerMod.Managers
{
    internal class SettingsManager
    {
        const string SettingsFileName = "SteamGameServerSettings.json";

        public GameServerSettings LoadSettings()
        {
            try
            {
#if USEMELONLOADER
                var settingsPath = Path.Combine(MelonEnvironment.UserDataDirectory, SettingsFileName);
#elif USEBEPINEX
                var settingsPath = Path.Combine("BepInEx", "config", SettingsFileName);
#endif
                Log.LogInfo($"Attempting to load settings from: {settingsPath}");

                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var settings = JsonConvert.DeserializeObject<GameServerSettings>(json);
                    Log.LogInfo("Settings loaded successfully");
                    return settings;
                }

                Log.LogInfo("Settings file not found, creating default settings");
                var defaultSettings = CreateDefaultSettings();
                SaveSettings(defaultSettings);
                return defaultSettings;
            }
            catch (Exception ex)
            {
                Log.LogError($"Error loading settings: {ex.Message}");
                Log.LogInfo("Using default settings");
                return CreateDefaultSettings();
            }
        }

        void SaveSettings(GameServerSettings settings)
        {
            try
            {
#if USEMELONLOADER
                var settingsPath = Path.Combine(MelonEnvironment.UserDataDirectory, SettingsFileName);
#elif USEBEPINEX
                var settingsPath = Path.Combine("BepInEx", "config", SettingsFileName);
#endif

                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(settingsPath, json);
                Log.LogInfo($"Settings saved to: {settingsPath}");
            }
            catch (Exception ex)
            {
                Log.LogError($"Error saving settings: {ex.Message}");
            }
        }

        static GameServerSettings CreateDefaultSettings()
        {
            return new()
            {
                AppID = 3164500,
            };
        }
    }
}
