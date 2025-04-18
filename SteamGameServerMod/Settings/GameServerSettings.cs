using System;
using System.Collections.Generic;
using System.Text;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using Steamworks;

namespace SteamGameServerMod.Settings
{
    public class GameServerSettings
    {
        public string ServerName = "[S1] OneM Testing";
        public string GameDescription = "Schedule 1";
        public string MapName = "default_map";
        public EServerMode ServerMode = EServerMode.eServerModeNoAuthentication;
        public int MaxPlayers = 16;
        public ushort GamePort = 27015;
        public ushort QueryPort = 27016;
        public bool PasswordProtected = false;
        public string ServerPassword = "";
        public string GameDirectory = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Schedule I";
        public uint AppID;
    }

    public class SettingsLoader
    {
        private readonly MelonLogger.Instance _logger;
        private const string SettingsFileName = "SteamGameServerSettings.json";

        public SettingsLoader(MelonLogger.Instance logger)
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
