using System;
using System.Collections;
using System.Net;
using MelonLoader;
using SteamGameServerMod.Managers;
using SteamGameServerMod.Settings;
using Steamworks;
using UnityEngine;

[assembly: MelonInfo(typeof(SteamGameServerMod.Core), "SteamGameServerMod", "1.0.0", "Red", null)]
[assembly: MelonColor(1,255,0,0)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace SteamGameServerMod
{

    public class Core : MelonMod
    {
        private GameServerSettings _settings;
        private GameServerManager _gameServer;

        public override void OnApplicationStart()
        {
            LoggerInstance.Msg("Steam GameServer Mod Initializing...");

            // Initialize settings
            _settings = new SettingsManager(LoggerInstance).LoadSettings();

            // Initialize game server
            _gameServer = new GameServerManager(LoggerInstance, _settings);

            // Register exit handler
            Application.quitting += OnApplicationQuitHandler;

            // Start initialization process
            MelonCoroutines.Start(InitializeServer());
        }

        private IEnumerator InitializeServer()
        {
            yield return _gameServer.Initialize();
        }

        public override void OnUpdate()
        {
            _gameServer?.Update();
        }

        private void OnApplicationQuitHandler()
        {
            ShutdownServer();
        }

        public override void OnApplicationQuit()
        {
            ShutdownServer();
        }

        private void ShutdownServer()
        {
            _gameServer?.Shutdown();
        }
    }
}