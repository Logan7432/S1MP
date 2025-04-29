using System;

namespace SteamGameServerMod.Settings
{
    public static class ServerSettings
    {
        public static bool ServerEnabled { get; set; } = false;
        public static string ServerListUrl { get; set; } = "https://scheduleoneservers.com";

        // Method to update settings from config
        public static void UpdateFromConfig(bool serverEnabled, string serverListUrl)
        {
            ServerEnabled = serverEnabled;
            ServerListUrl = serverListUrl;
        }
    }
}