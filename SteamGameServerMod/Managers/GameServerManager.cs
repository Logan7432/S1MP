using System.Collections;
using FishNet;
using SteamGameServerMod.Callbacks;
using SteamGameServerMod.Logging;
using SteamGameServerMod.Settings;
using Steamworks;
using UnityEngine;

namespace SteamGameServerMod.Managers
{
    internal class GameServerManager(GameServerSettings settings)
    {
        readonly CallbackHandler _callbackHandler = new(settings);
        bool _serverInitialized;

        public IEnumerator Initialize()
        {
            // Register callbacks
            _callbackHandler.RegisterCallbacks();

            Log.LogInfo("Starting Steam GameServer initialization...");

            if (!SteamAPI.IsSteamRunning())
            {
                Log.LogError("Steam is not running. Cannot initialize GameServer.");
                yield break;
            }

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

            // Apply game server configuration
            Log.LogInfo("Configuring server settings...");
            SteamGameServer.SetModDir(settings.GameDescription);
            SteamGameServer.SetGameDescription(settings.GameDescription);
            SteamGameServer.SetProduct($"{settings.AppID}");
            SteamGameServer.SetDedicatedServer(true);
            SteamGameServer.SetMaxPlayerCount(settings.MaxPlayers);
            SteamGameServer.SetPasswordProtected(settings.PasswordProtected);
            SteamGameServer.SetServerName(settings.ServerName);
            SteamGameServer.SetMapName(settings.MapName);

            Log.LogInfo("Logging on to Steam...");
            SteamGameServer.LogOn(settings.Token);

            yield return new WaitUntil(SteamGameServer.BLoggedOn);

            _serverInitialized = true;
            Log.LogInfo("Steam GameServer initialization completed.");
            Log.LogInfo($"Steam initialized, Steam running: {SteamAPI.IsSteamRunning()}, Server logged on: {SteamGameServer.BLoggedOn()}");

            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, settings.MaxPlayers);
            Log.LogInfo("SteamMatching::CreateLobby called");
        }

        public void Update()
        {
            if (_serverInitialized)
            {
                GameServer.RunCallbacks();
                SteamAPI.RunCallbacks();
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
