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
}
