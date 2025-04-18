#if USEMELONLOADER
using MelonLoader;
#elif USEBEPINEX
using BepInEx;
using BepInEx.Unity.Mono;
#endif

using System.Collections;
using SteamGameServerMod.Logging;
using SteamGameServerMod.Managers;
using SteamGameServerMod.Settings;
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
        private GameServerSettings _settings;
        private GameServerManager _gameServer;

#if USEMELONLOADER
        public override void OnInitializeMelon()
#elif USEBEPINEX
        private void Awake()
#endif
        {
#if USEBEPINEX
            Log.Logger = Logger;
#endif

            Log.LogInfo("Steam GameServer Mod Initializing...");

            // Initialize settings
            _settings = new SettingsManager()
                .LoadSettings();

            // Initialize game server
            _gameServer = new(_settings);

            // Register exit handler
            Application.quitting += OnApplicationQuitHandler;

            // Start initialization process
#if USEMELONLOADER
            MelonCoroutines.Start(InitializeServer());
#elif USEBEPINEX
            StartCoroutine(InitializeServer());
#endif
        }

        private IEnumerator InitializeServer()
        {
            yield return _gameServer.Initialize();
        }

#if USEMELONLOADER
        public override void OnUpdate()
#elif USEBEPINEX
        private void Update()
#endif
        {
            _gameServer?.Update();
        }

        private void OnApplicationQuitHandler()
        {
            ShutdownServer();
        }

#if USEMELONLOADER
        public override void OnApplicationQuit()
#elif USEBEPINEX
        private void OnApplicationQuit()
#endif
        {
            ShutdownServer();
        }

        private void ShutdownServer()
        {
            _gameServer?.Shutdown();
        }
    }
}
