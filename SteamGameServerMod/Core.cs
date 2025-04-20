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
using Steamworks;
using UnityEngine;

#if USEMELONLOADER
[assembly: MelonInfo(typeof(SteamGameServerMod.Core), "SteamGameServerMod", "1.0.0", "Red")]
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

        GameServerSettings _settings;
        GameServerManager _gameServer;

#if USEMELONLOADER
        public override void OnInitializeMelon()
#elif USEBEPINEX
        void Awake()
#endif
        {
#if USEBEPINEX
            Log.Logger = Logger;
#endif

            var startArguments = Environment.GetCommandLineArgs()
                .ToList();
            IsHost = startArguments.Contains("--host");
            if (IsHost)
            {
                Log.LogInfo("Steam GameServer Mod Initializing...");

                if (!SteamAPI.Init())
                {
                    Log.LogFatal("Failed to initialize SteamAPI!!!!!!");
                    return;
                }

                // Initialize settings
                _settings = new SettingsManager()
                    .LoadSettings();

                // Initialize game server
                _gameServer = new(_settings);

                // Register exit handler
                Application.quitting += OnApplicationQuitHandler;

                // Start initialization process
#if USEMELONLOADER
                MelonCoroutines.Start(_gameServer.Initialize());
                MelonCoroutines.Start(_gameServer.InitializeServerSpawning());
#elif USEBEPINEX
                StartCoroutine(_gameServer.Initialize());
                StartCoroutine(_gameServer.InitializeServerSpawning());
#endif
            }

#if USEBEPINEX
            new Harmony($"com.S1MP.{MyPluginInfo.PLUGIN_GUID}")
                .PatchAll();
#endif
        }

#if USEMELONLOADER
        public override void OnUpdate()
#elif USEBEPINEX
        void Update()
#endif
        {
            _gameServer?.Update();
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
            _gameServer?.Shutdown();
        }
    }
}
