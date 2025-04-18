#if USEMELONLOADER
using MelonLoader;
#elif USEBEPINEX
using BepInEx;
using BepInEx.Unity.Mono;
using HarmonyLib;
#endif

using System.Collections;
using FishNet;
using FishNet.Component.Scenes;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using ScheduleOne.Audio;
using ScheduleOne.Persistence;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
using SteamGameServerMod.Callbacks;
using SteamGameServerMod.Logging;
using SteamGameServerMod.Managers;
using SteamGameServerMod.Settings;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            MelonCoroutines.Start(InitializeServer());
            MelonCoroutines.Start(InitializeServerSpawning(_settings));
#elif USEBEPINEX
            StartCoroutine(InitializeServer());
            StartCoroutine(InitializeServerSpawning(_settings));
#endif

#if USEBEPINEX
            new Harmony($"com.S1MP.{MyPluginInfo.PLUGIN_GUID}")
                .PatchAll();
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

        static IEnumerator InitializeServerSpawning(GameServerSettings settings)
        {
            yield return new WaitUntil(() => CallbackHandler.LobbyGameEntered);

            Log.LogInfo("Spawn Server: Load scenes etc");

            var saveFile = SaveGameManager.GetSave(settings.SaveGameName);
            LoadManager.Instance.ActiveSaveInfo = saveFile;
            LoadManager.Instance.IsLoading = true;
            LoadManager.Instance.TimeSinceGameLoaded = 0;
            LoadManager.instance.LoadedGameFolderPath = saveFile.SavePath;
            LoadManager.Instance.LoadStatus = LoadManager.ELoadStatus.LoadingScene;
            LoadManager.Instance.StoredSaveInfo = null;

            MusicPlayer.Instance.StopAndDisableTracks();

            LoadingScreen.Instance.Open(loadingTutorial: false);

            LoadManager.Instance.onPreSceneChange?.Invoke();
            LoadManager.Instance.CleanUp();

            InstanceFinder.NetworkManager.gameObject.GetComponent<DefaultScene>()
                .SetOnlineScene("Main");

            var sceneLoading = SceneManager.LoadSceneAsync("Main");
            yield return new WaitUntil(() => sceneLoading!.isDone);

            Log.LogInfo("Spawn Server: Main scene loaded!!!!!");

            LoadManager.Instance.LoadStatus = LoadManager.ELoadStatus.Initializing;
            LoadManager.Instance.onPreLoad?.Invoke();

            var fishySteamworks = InstanceFinder.TransportManager.GetTransport<Multipass>()
                .GetTransport<FishySteamworks.FishySteamworks>();
            fishySteamworks.OnServerConnectionState += _ =>
            {
                // Connect client
                InstanceFinder.TransportManager.GetTransport<Multipass>()
                    .SetClientTransport<FishySteamworks.FishySteamworks>();
                InstanceFinder.NetworkManager.ClientManager.StartConnection();
            };
            fishySteamworks.SetClientAddress("0.0.0.0");
            fishySteamworks.StartConnection(server: true);

            yield return new WaitUntil(() => InstanceFinder.IsClient && InstanceFinder.IsServer);

            Log.LogInfo("Waiting for client to initialize...");
            yield return new WaitUntil(() => InstanceFinder.IsClient && InstanceFinder.ClientManager.Connection.IsValid);
            Log.LogInfo($"Client network initialized (connected to {InstanceFinder.ClientManager.Connection} with client id {InstanceFinder.ClientManager.Connection.ClientId})");

            Log.LogInfo("Waiting for local player to spawn...");
            LoadManager.Instance.LoadStatus = LoadManager.ELoadStatus.SpawningPlayer;
            yield return new WaitUntil(() => Player.Local);
            Player.Local.gameObject.SetActive(false);
            Log.LogInfo("Local player spawned");

            LoadManager.Instance.LoadStatus = LoadManager.ELoadStatus.LoadingData;
            yield return LoadSave(LoadManager.Instance.ActiveSaveInfo);
            LoadManager.Instance.onLoadComplete?.Invoke();

            LoadManager.Instance.LoadStatus = LoadManager.ELoadStatus.None;
            LoadingScreen.Instance.Close();
            LoadManager.Instance.IsLoading = false;
            LoadManager.Instance.IsGameLoaded = true;
        }

        static IEnumerator LoadSave(SaveInfo saveInfo)
        {
            Log.LogInfo($"Loading save: {saveInfo.OrganisationName} ...");

            foreach (var saveable in SaveManager.Instance.BaseSaveables)
                _ = new LoadRequest(Path.Combine(LoadManager.Instance.LoadedGameFolderPath, saveable.SaveFolderName), saveable.Loader);

            while (LoadManager.Instance.loadRequests.Count > 0)
            {
                for (var i = 0; i < 50 && LoadManager.Instance.loadRequests.Count > 0; ++i)
                {
                    var loadRequest = LoadManager.Instance.loadRequests[0];
                    try
                    {
                        loadRequest.Complete();
                    }
                    catch (Exception ex)
                    {
                        Log.LogInfo($"LOAD ERROR FOR LOAD REQUEST: {loadRequest.Path}: {ex.Message} Site: {ex.TargetSite}");

                        if (LoadManager.Instance.loadRequests.FirstOrDefault() == loadRequest)
                            LoadManager.Instance.loadRequests.RemoveAt(0);
                    }
                }

                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            Log.LogInfo($"Save loaded: {saveInfo.OrganisationName} ...");
        }
    }
}
