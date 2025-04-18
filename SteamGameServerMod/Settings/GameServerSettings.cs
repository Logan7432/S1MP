using Steamworks;

namespace SteamGameServerMod.Settings
{
    public class GameServerSettings
    {
        public string ServerName { get; set; } = "[S1] OneM Testing";
        public string GameDescription { get; set; }  = "Schedule 1";
        public string MapName { get; set; }  = "default_map";
        public EServerMode ServerMode { get; set; }  = EServerMode.eServerModeNoAuthentication;
        public int MaxPlayers { get; set; }  = 16;
        public ushort GamePort { get; set; }  = 27015;
        public ushort QueryPort { get; set; }  = 27016;
        public bool PasswordProtected { get; set; } = false;
        public string ServerPassword { get; set; } = "";
        public uint AppID { get; set; }
        public string Token { get; set; } = string.Empty;
        public string SaveGameName { get; set; } = "S1MP_Testing";
    }
}
