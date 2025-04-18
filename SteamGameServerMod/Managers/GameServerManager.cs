using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MelonLoader;
using SteamGameServerMod.Callbacks;
using SteamGameServerMod.Settings;
using SteamGameServerMod.Util;
using Steamworks;

namespace SteamGameServerMod.Managers
{
    internal class GameServerManager
    {
        private readonly MelonLogger.Instance _logger;
        private readonly GameServerSettings _settings;
        private readonly CallbackHandler _callbackHandler;
        private bool _serverInitialized = false;

        public GameServerManager(MelonLogger.Instance logger, GameServerSettings settings)
        {
            _logger = logger;
            _settings = settings;
            _callbackHandler = new CallbackHandler(logger, settings);
        }

        public IEnumerator Initialize()
        {
            _logger.Msg("Starting Steam GameServer initialization...");

            if (!SteamAPI.IsSteamRunning())
            {
                _logger.Error("Steam is not running. Cannot initialize GameServer.");
                yield break;
            }

            try
            {
                // Log Initial Configuration
                _logger.Msg($"Initializing server with AppID: {_settings.AppID}");
                _logger.Msg($"Game Port: {_settings.GamePort}, Query Port: {_settings.QueryPort}");
                _logger.Msg($"Server Mode: {_settings.ServerMode}");

                //UInt32 serverIp = Utils.IpToUInt32("192.168.178.70");
                UInt32 serverIp = 0;

                bool success = GameServer.Init(
                    serverIp,
                    _settings.QueryPort,
                    _settings.GamePort,
                    _settings.ServerMode,
                    $"1.0.0"
                );

                if (!success)
                {
                    _logger.Error("Failed to initialize Steam GameServer.");
                    yield break;
                }

                // Register callbacks
                _callbackHandler.RegisterCallbacks();

                // Apply game server configuration
                _logger.Msg("Configuring server settings...");
                SteamGameServer.SetModDir(_settings.GameDirectory);
                SteamGameServer.SetProduct($"{_settings.AppID}");
                SteamGameServer.SetGameDescription(_settings.GameDescription);
                SteamGameServer.SetDedicatedServer(true);
                SteamGameServer.SetMaxPlayerCount(_settings.MaxPlayers);
                SteamGameServer.SetPasswordProtected(_settings.PasswordProtected);
                SteamGameServer.SetServerName(_settings.ServerName);
                SteamGameServer.SetMapName(_settings.MapName);

                _logger.Msg("Logging on to Steam...");
                SteamGameServer.LogOn("GAMESERVER_TOKEN");

                // Communicate to Steam master server that this server is active and should be advertised on server browser
                SteamGameServer.SetAdvertiseServerActive(true);

                _serverInitialized = true;
                _logger.Msg("Steam GameServer initialization completed.");
                _logger.Msg($"Steam initialized: {success}, Steam running: {SteamAPI.IsSteamRunning()}, Server logged on: {SteamGameServer.BLoggedOn()}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Exception during Steam GameServer initialization: {ex.Message}");
                _logger.Error($"Stack trace: {ex.StackTrace}");
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
                _logger.Msg("Shutting down Steam GameServer...");
                SteamGameServer.SetAdvertiseServerActive(false);
                SteamGameServer.LogOff();
                GameServer.Shutdown();
                _serverInitialized = false;
                _logger.Msg("Steam GameServer shutdown complete.");
            }
        }
    }
}
