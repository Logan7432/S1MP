#if USEMELONLOADER
using MelonLoader;
#elif USEBEPINEX
using BepInEx;
using BepInEx.Unity.Mono;
using HarmonyLib;
#endif

using SteamGameServerMod.Logging;
using SteamGameServerMod.Managers;
using SteamGameServerMod.Settings;
using SteamGameServerMod.Client;
using Steamworks;
using UnityEngine;
using System;
using System.Linq;

#if USEMELONLOADER
[assembly: MelonInfo(typeof(SteamGameServerMod.Core), "SteamGameServerMod", "1.0.0", "YourName")]
[assembly: MelonColor(1,255,0,0)]
[assembly: MelonGame("TVGS", "Schedule I")]
#endif
namespace SteamGameServerMod
{
#if USEMELONLOADER
    public class Core : MelonMod
#elif USEBEPINEX
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Core : BaseUnityPlugin
#endif
    {
        public static bool IsHost;
        public static bool IsRunningAsServer => ServerSettings.ServerEnabled;

        private GameServerSettings _settings;
        private GameServerManager _gameServer;
        private Client.ClientManager _clientManager; // Keep original name, but use full namespace
        private ConfigManager _configManager;

#if USEMELONLOADER
        public override void OnInitializeMelon()
#elif USEBEPINEX
        void Awake()
#endif
        {
#if USEBEPINEX
            Log.Logger = Logger;
#endif

            var startArguments = Environment.GetCommandLineArgs().ToList();
            IsHost = startArguments.Contains("--host") || IsRunningAsServer;

            Log.LogInfo("Steam Game Server Mod Initializing...");

            // Initialize configuration
            _configManager = new ConfigManager();
            _configManager.LoadConfig();

            if (!SteamAPI.Init())
            {
                Log.LogFatal("Failed to initialize SteamAPI!!!!!!");
                return;
            }

            if (IsRunningAsServer)
            {
                // Initialize server mode
                InitializeServerMode();
            }
            else
            {
                // Initialize client mode
                InitializeClientMode();
            }

            // Register exit handler
            Application.quitting += OnApplicationQuitHandler;

#if USEBEPINEX
            new Harmony($"com.S1MP.{MyPluginInfo.PLUGIN_GUID}")
                .PatchAll();
#endif
        }

        private void InitializeServerMode()
        {
            Log.LogInfo("Initializing in Server Mode");

            // Initialize settings
            _settings = new SettingsManager()
                .LoadSettings();

            // Initialize game server
            _gameServer = new(_settings);

            // Start initialization process
#if USEMELONLOADER
            MelonCoroutines.Start(_gameServer.Initialize());
            MelonCoroutines.Start(_gameServer.InitializeServerSpawning());
#elif USEBEPINEX
            StartCoroutine(_gameServer.Initialize());
            StartCoroutine(_gameServer.InitializeServerSpawning());
#endif
        }

        private void InitializeClientMode()
        {
            Log.LogInfo("Initializing in Client Mode");

            // Initialize client manager
            _clientManager = new Client.ClientManager();

#if USEMELONLOADER
            MelonCoroutines.Start(_clientManager.Initialize());
#elif USEBEPINEX
            StartCoroutine(_clientManager.Initialize());
#endif
        }

#if USEMELONLOADER
        public override void OnUpdate()
#elif USEBEPINEX
        void Update()
#endif
        {
            if (IsRunningAsServer)
            {
                _gameServer?.Update();
            }
            else
            {
                _clientManager?.Update();
            }
        }

        void OnApplicationQuitHandler()
        {
            ShutdownServer();
        }

#if USEMELONLOADER
        public override void OnApplicationQuit()
#elif USEBEPINEX
        void OnApplicationQuit()
#endif
        {
            ShutdownServer();
        }

        void ShutdownServer()
        {
            if (IsRunningAsServer)
            {
                _gameServer?.Shutdown();
            }
            else
            {
                _clientManager?.Shutdown();
            }
        }
    }
}