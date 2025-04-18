using System;
using System.Collections.Generic;
using System.Text;
using MelonLoader.Utils;
using MelonLoader;
using Newtonsoft.Json;
using SteamGameServerMod.Settings;

namespace SteamGameServerMod.Managers
{
    internal class SettingsManager
    {
        private readonly MelonLogger.Instance _logger;
        private const string SettingsFileName = "SteamGameServerSettings.json";

        public SettingsManager(MelonLogger.Instance logger)
        {
            _logger = logger;
        }

        public GameServerSettings LoadSettings()
        {
            try
            {
                string settingsPath = Path.Combine(MelonEnvironment.UserDataDirectory, SettingsFileName);
                _logger.Msg($"Attempting to load settings from: {settingsPath}");

                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    var settings = JsonConvert.DeserializeObject<GameServerSettings>(json);
                    _logger.Msg("Settings loaded successfully");
                    return settings;
                }
                else
                {
                    _logger.Msg("Settings file not found, creating default settings");
                    var defaultSettings = CreateDefaultSettings();
                    SaveSettings(defaultSettings);
                    return defaultSettings;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error loading settings: {ex.Message}");
                _logger.Msg("Using default settings");
                return CreateDefaultSettings();
            }
        }

        private void SaveSettings(GameServerSettings settings)
        {
            try
            {
                string settingsPath = Path.Combine(MelonEnvironment.UserDataDirectory, SettingsFileName);
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(settingsPath, json);
                _logger.Msg($"Settings saved to: {settingsPath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error saving settings: {ex.Message}");
            }
        }

        private GameServerSettings CreateDefaultSettings()
        {
            return new GameServerSettings
            {
                AppID = 3164500,
            };
        }
    }
}
