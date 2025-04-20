using System.Collections;

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

namespace SteamGameServerMod.Managers
{
    internal class GameServerManager(GameServerSettings settings)
    {
        bool _serverInitialized;
        bool _lobbyGameCreated;

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
            Log.LogInfo($"Initializing server with AppID: {settings.AppID}");
            Log.LogInfo($"Game Port: {settings.GamePort}, Query Port: {settings.QueryPort}");
            Log.LogInfo($"Server Mode: {settings.ServerMode}");

            // var serverIp = Utils.IpToUInt32("192.168.178.70");
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

        void RegisterCallbacks()
        {
            // // Register server callbacks
            // Callback<SteamServersConnected_t>.Create(OnSteamServersConnected);
            // Callback<SteamServerConnectFailure_t>.Create(OnSteamServerConnectFailure);
            // Callback<SteamServersDisconnected_t>.Create(OnSteamServersDisconnected);
            //
            // // Register client callbacks
            // Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
            // Callback<GSClientApprove_t>.Create(OnClientApproved);
            // Callback<GSClientDeny_t>.Create(OnClientDenied);
            // Callback<GSClientKick_t>.Create(OnClientKicked);
            //
            // // Register debug callbacks
            // Callback<ValidateAuthTicketResponse_t>.Create(OnValidateAuthTicket);
            // Callback<P2PSessionConnectFail_t>.Create(OnP2PConnectFail);
            // Callback<GameServerChangeRequested_t>.Create(OnGameServerChangeRequested);

            Callback<LobbyChatUpdate_t>.Create(lobbyChatUpdate =>
            {
                var stateChange = (EChatMemberStateChange)lobbyChatUpdate.m_rgfChatMemberStateChange;
                Log.LogInfo($"LobbyChatUpdate: LobbyID: {lobbyChatUpdate.m_ulSteamIDLobby} {lobbyChatUpdate.m_ulSteamIDUserChanged} {stateChange}");
            });

            Callback<LobbyCreated_t>.Create(lobbyCreated =>
            {
                Log.LogInfo($"Lobby created: {lobbyCreated.m_eResult} {lobbyCreated.m_ulSteamIDLobby}");

                SteamMatchmaking.SetLobbyGameServer((CSteamID)lobbyCreated.m_ulSteamIDLobby, 0, settings.GamePort, CSteamID.Nil);

                SteamMatchmaking.SetLobbyData((CSteamID)lobbyCreated.m_ulSteamIDLobby, "version", settings.GameVersion);
                SteamMatchmaking.SetLobbyData((CSteamID)lobbyCreated.m_ulSteamIDLobby, "ready", "true");

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
