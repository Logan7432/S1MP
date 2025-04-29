using System.Collections;
using System.Net;
using FishNet;
using FishNet.Component.Scenes;
using FishNet.Transporting.Multipass;
using ScheduleOne.Audio;
using ScheduleOne.Persistence;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

using SteamGameServerMod.Logging;
using SteamGameServerMod.Settings;
using SteamGameServerMod.Util;

namespace SteamGameServerMod.Managers
{
    internal class GameServerManager
    {
        private readonly GameServerSettings _settings;
        private bool _serverInitialized;
        private bool _lobbyGameCreated;

        public GameServerManager(GameServerSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Initializes the <see cref="GameServerManager"/> instance.
        /// </summary>
        /// <returns></returns>
        public IEnumerator Initialize()
        {
            RegisterCallbacks();

            Log.LogInfo("Starting Steam GameServer initialization...");

            if (!SteamAPI.IsSteamRunning())
            {
                Log.LogError("Steam is not running. Cannot initialize GameServer.");
                yield break;
            }

            // Log Initial Configuration
            Log.LogInfo($"Initializing server with AppID: {_settings.AppID}");
            Log.LogInfo($"Game Port: {_settings.GamePort}, Query Port: {_settings.QueryPort}");
            Log.LogInfo($"Server Mode: {_settings.ServerMode}");

            uint serverIp = 0;
            if (_settings.ServerIP != "0.0.0.0")
            {
                serverIp = Utils.IpToUInt32(_settings.ServerIP);
                Log.LogInfo($"Using specific IP address: {_settings.ServerIP}");
            }

            var success = GameServer.Init(
                serverIp,
                _settings.QueryPort,
                _settings.GamePort,
                _settings.ServerMode,
                _settings.GameVersion
            );

            if (!success)
            {
                Log.LogError("Failed to initialize Steam GameServer.");
                yield break;
            }

            // Apply game server configuration
            Log.LogInfo("Configuring server settings...");
            SteamGameServer.SetModDir(_settings.GameDescription);
            SteamGameServer.SetGameDescription(_settings.GameDescription);
            SteamGameServer.SetProduct($"{_settings.AppID}");
            SteamGameServer.SetDedicatedServer(true);
            SteamGameServer.SetMaxPlayerCount(_settings.MaxPlayers);
            SteamGameServer.SetPasswordProtected(_settings.PasswordProtected);
            SteamGameServer.SetServerName(_settings.ServerName);
            SteamGameServer.SetMapName(_settings.MapName);

            Log.LogInfo("Logging on to Steam...");
            SteamGameServer.LogOn(_settings.Token);

            yield return new WaitUntil(SteamGameServer.BLoggedOn);

            _serverInitialized = true;
            Log.LogInfo("Steam GameServer initialization completed.");
            Log.LogInfo($"Steam initialized, Steam running: {SteamAPI.IsSteamRunning()}, Server logged on: {SteamGameServer.BLoggedOn()}");

            // Create Steam lobby
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, _settings.MaxPlayers);
            Log.LogInfo("SteamMatching::CreateLobby called");
        }

        /// <summary>
        /// Updates the <see cref="GameServerManager"/> instance.
        /// </summary>
        public void Update()
        {
            if (_serverInitialized)
            {
                GameServer.RunCallbacks();
                SteamAPI.RunCallbacks();
            }
        }

        /// <summary>
        /// Shut's down the <see cref="GameServerManager"/> instance.
        /// </summary>
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

        /// <summary>
        /// Initialize the spawning of the host <see cref="Player"/>
        /// </summary>
        /// <returns></returns>
        public IEnumerator InitializeServerSpawning()
        {
            yield return new WaitUntil(() => _lobbyGameCreated);

            Log.LogInfo("Spawn Server: Load scenes and host's player");

            var saveFile = SaveGameManager.GetSave(_settings.SaveGameName);
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

            Log.LogInfo("Spawn Server: Main scene loaded!");

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

            // Register heartbeat to master server if enabled
            if (_settings.RegisterToMasterServer)
            {
                StartHeartbeat();
            }
        }

        private void StartHeartbeat()
        {
            Log.LogInfo("Starting heartbeat to master server...");

            // Implement server heartbeat functionality
            // This could send regular updates to a master server
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

        void RegisterCallbacks()
        {
            Callback<LobbyChatUpdate_t>.Create(lobbyChatUpdate =>
            {
                var stateChange = (EChatMemberStateChange)lobbyChatUpdate.m_rgfChatMemberStateChange;
                Log.LogInfo($"LobbyChatUpdate: LobbyID: {lobbyChatUpdate.m_ulSteamIDLobby} {lobbyChatUpdate.m_ulSteamIDUserChanged} {stateChange}");
            });

            Callback<LobbyCreated_t>.Create(lobbyCreated =>
            {
                Log.LogInfo($"Lobby created: {lobbyCreated.m_eResult} {lobbyCreated.m_ulSteamIDLobby}");

                SteamMatchmaking.SetLobbyGameServer((CSteamID)lobbyCreated.m_ulSteamIDLobby, 0, _settings.GamePort, CSteamID.Nil);

                SteamMatchmaking.SetLobbyData((CSteamID)lobbyCreated.m_ulSteamIDLobby, "version", _settings.GameVersion);
                SteamMatchmaking.SetLobbyData((CSteamID)lobbyCreated.m_ulSteamIDLobby, "ready", "true");
                SteamMatchmaking.SetLobbyData((CSteamID)lobbyCreated.m_ulSteamIDLobby, "name", _settings.ServerName);
                SteamMatchmaking.SetLobbyData((CSteamID)lobbyCreated.m_ulSteamIDLobby, "maxplayers", _settings.MaxPlayers.ToString());

                // Communicate to Steam master server that this server is active and should be advertised on server browser
                SteamGameServer.SetAdvertiseServerActive(true);
            });

            Callback<LobbyGameCreated_t>.Create(_ =>
            {
                Log.LogInfo("Lobby game created");
                _lobbyGameCreated = true;
            });

            Log.LogInfo("All Steam callbacks registered successfully");
        }
    }
}