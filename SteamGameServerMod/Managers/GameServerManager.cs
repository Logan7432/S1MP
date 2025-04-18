using System.Collections;
using SteamGameServerMod.Callbacks;
using SteamGameServerMod.Logging;
using SteamGameServerMod.Settings;
using Steamworks;

namespace SteamGameServerMod.Managers
{
    internal class GameServerManager(GameServerSettings settings)
    {
        private readonly CallbackHandler _callbackHandler = new(settings);
        private bool _serverInitialized;

        public IEnumerator Initialize()
        {
            Log.LogInfo("Starting Steam GameServer initialization...");

            if (!SteamAPI.IsSteamRunning())
            {
                Log.LogError("Steam is not running. Cannot initialize GameServer.");
                yield break;
            }

            try
            {
                // Log Initial Configuration
                Log.LogInfo($"Initializing server with AppID: {settings.AppID}");
                Log.LogInfo($"Game Port: {settings.GamePort}, Query Port: {settings.QueryPort}");
                Log.LogInfo($"Server Mode: {settings.ServerMode}");

                //UInt32 serverIp = Utils.IpToUInt32("192.168.178.70");
                var serverIp = 0u;

                var success = GameServer.Init(
                    serverIp,
                    settings.QueryPort,
                    settings.GamePort,
                    settings.ServerMode,
                    "1.0.0"
                );

                if (!success)
                {
                    Log.LogError("Failed to initialize Steam GameServer.");
                    yield break;
                }

                // Register callbacks
                _callbackHandler.RegisterCallbacks();

                // Apply game server configuration
                Log.LogInfo("Configuring server settings...");
                SteamGameServer.SetModDir(settings.GameDirectory);
                SteamGameServer.SetProduct($"{settings.AppID}");
                SteamGameServer.SetGameDescription(settings.GameDescription);
                SteamGameServer.SetDedicatedServer(true);
                SteamGameServer.SetMaxPlayerCount(settings.MaxPlayers);
                SteamGameServer.SetPasswordProtected(settings.PasswordProtected);
                SteamGameServer.SetServerName(settings.ServerName);
                SteamGameServer.SetMapName(settings.MapName);

                Log.LogInfo("Logging on to Steam...");
                SteamGameServer.LogOn("GAMESERVER_TOKEN");

                // Communicate to Steam master server that this server is active and should be advertised on server browser
                SteamGameServer.SetAdvertiseServerActive(true);

                _serverInitialized = true;
                Log.LogInfo("Steam GameServer initialization completed.");
                Log.LogInfo($"Steam initialized, Steam running: {SteamAPI.IsSteamRunning()}, Server logged on: {SteamGameServer.BLoggedOn()}");
            }
            catch (Exception ex)
            {
                Log.LogError($"Exception during Steam GameServer initialization: {ex.Message}");
                Log.LogError($"Stack trace: {ex.StackTrace}");
            }
        }

        public void Update()
        {
            if (_serverInitialized)
            {
                GameServer.RunCallbacks();
            }
        }

        public void Shutdown()
        {
            if (_serverInitialized)
            {
                Log.LogInfo("Shutting down Steam GameServer...");
                SteamGameServer.SetAdvertiseServerActive(false);
                SteamGameServer.LogOff();
                GameServer.Shutdown();
                _serverInitialized = false;
                Log.LogInfo("Steam GameServer shutdown complete.");
            }
        }
    }
}
