using System;
using System.Collections;
using System.Net;
using MelonLoader;
using Steamworks;
using UnityEngine;

[assembly: MelonInfo(typeof(SteamGameServerMod.Core), "SteamGameServerMod", "1.0.0", "Red", null)]
[assembly: MelonColor(1,255,0,0)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace SteamGameServerMod
{
    public class SteamGameServerSettings
    {
        public string ServerName = "[S1] OneM Testing";
        public string GameDescription = "Schedule 1";
        public string MapName = "default_map";
        public EServerMode ServerMode = EServerMode.eServerModeNoAuthentication;
        public int MaxPlayers = 16;
        public ushort GamePort = 27015;
        public ushort QueryPort = 27016;
        public bool PasswordProtected = false;
        public string ServerPassword = "";
        public string GameDirectory = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Schedule I";
        public uint AppID;
    }

    public class Core : MelonMod
    {
        private SteamGameServerSettings _settings;
        private bool _serverInitialized = false;
        private Callback<SteamServersConnected_t> _serverConnectedCallback;
        private Callback<SteamServerConnectFailure_t> _serverConnectFailureCallback;
        private Callback<SteamServersDisconnected_t> _serverDisconnectedCallback;

        private Callback<P2PSessionRequest_t> _p2pSessionRequestCallback;
        private Callback<GSClientApprove_t> _clientApproveCallback;
        private Callback<GSClientDeny_t> _clientDenyCallback;
        private Callback<GSClientKick_t> _clientKickCallback;

        // Debug callbacks for testing
        private Callback<ValidateAuthTicketResponse_t> _validateAuthCallback;
        private Callback<P2PSessionConnectFail_t> _p2pConnectFailCallback;
        private Callback<GameServerChangeRequested_t> _gameServerChangeCallback;

        public override void OnApplicationStart()
        {
            LoggerInstance.Msg("Steam GameServer Mod Initializing...");
            _settings = LoadSettings();

            // Exit handler
            Application.quitting += OnApplicationQuitHandler;


            MelonCoroutines.Start(InitializeSteamGameServer());
        }

        private SteamGameServerSettings LoadSettings()
        {
            // Add code to load settings from json or other config like melon preference
            return new SteamGameServerSettings
            {
                AppID = 3164500,
            };
        }

        private IEnumerator InitializeSteamGameServer()
        {
            LoggerInstance.Msg("Starting Steam GameServer initialization...");

            if (!SteamAPI.IsSteamRunning())
            {
                LoggerInstance.Error("Steam is not running. Cannot initialize GameServer.");
                yield break;
            }

            try
            {
                // Log Initial Configuration
                LoggerInstance.Msg($"Initializing server with AppID: {_settings.AppID}");
                LoggerInstance.Msg($"Game Port: {_settings.GamePort}, Query Port: {_settings.QueryPort}");
                LoggerInstance.Msg($"Server Mode: {_settings.ServerMode}");

                UInt32 serverIp = IpToUInt32("192.168.178.70");
                //UInt32 serverIp = 0;

                bool success = GameServer.Init(
                    serverIp,
                    _settings.QueryPort,
                    _settings.GamePort,
                    _settings.ServerMode,
                    $"1.0.0"
                );

                if (!success)
                {
                    LoggerInstance.Error("Failed to initialize Steam GameServer.");
                    yield break;
                }

                // CALLBACKS
                // register server callbacks
                _serverConnectedCallback = Callback<SteamServersConnected_t>.Create(OnSteamServersConnected);
                _serverConnectFailureCallback = Callback<SteamServerConnectFailure_t>.Create(OnSteamServerConnectFailure);
                _serverDisconnectedCallback = Callback<SteamServersDisconnected_t>.Create(OnSteamServersDisconnected);
                // register client callbacks
                _p2pSessionRequestCallback = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
                _clientApproveCallback = Callback<GSClientApprove_t>.Create(OnClientApproved);
                _clientDenyCallback = Callback<GSClientDeny_t>.Create(OnClientDenied);
                _clientKickCallback = Callback<GSClientKick_t>.Create(OnClientKicked);




                // DEBUGGING CALLBACKS
                _validateAuthCallback = Callback<ValidateAuthTicketResponse_t>.Create(OnValidateAuthTicket);
                _p2pConnectFailCallback = Callback<P2PSessionConnectFail_t>.Create(OnP2PConnectFail);
                _gameServerChangeCallback = Callback<GameServerChangeRequested_t>.Create(OnGameServerChangeRequested);



                // Apply game server configuration
                LoggerInstance.Msg("Configuring server settings...");
                SteamGameServer.SetModDir(_settings.GameDirectory);
                SteamGameServer.SetProduct($"{_settings.AppID}");
                SteamGameServer.SetGameDescription(_settings.GameDescription);
                SteamGameServer.SetDedicatedServer(true);
                SteamGameServer.SetMaxPlayerCount(_settings.MaxPlayers);
                SteamGameServer.SetPasswordProtected(_settings.PasswordProtected);
                SteamGameServer.SetServerName(_settings.ServerName);
                SteamGameServer.SetMapName(_settings.MapName);

                LoggerInstance.Msg("Logging on to Steam...");
                SteamGameServer.LogOn("GAMESERVER_TOKEN");

                // Communicate to Steam master server that this server is active and should be advertised on server browser
                SteamGameServer.SetAdvertiseServerActive(true);


                _serverInitialized = true;
                LoggerInstance.Msg("Steam GameServer initialization completed.");
                LoggerInstance.Msg($"Steam initialized: {success}, Steam running: {SteamAPI.IsSteamRunning()}, Server logged on: {SteamGameServer.BLoggedOn()}");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Exception during Steam GameServer initialization: {ex.Message}");
                LoggerInstance.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        public static uint IpToUInt32(string ipAddress)
        {
            byte[] bytes = IPAddress.Parse(ipAddress).GetAddressBytes();
            uint ipAsUint = ((uint)bytes[0]) | ((uint)bytes[1] << 8) | ((uint)bytes[2] << 16) | ((uint)bytes[3] << 24);
            return ipAsUint;
        }
        public override void OnUpdate()
        {
            if (_serverInitialized)
            {
                GameServer.RunCallbacks();
            }
        }

        private void OnSteamServersConnected(SteamServersConnected_t pCallback)
        {
            LoggerInstance.Msg($"Connected to Steam servers. Server ID: {SteamGameServer.GetSteamID()}");
            LoggerInstance.Msg($"Server is now visible in server browser: {SteamGameServer.BSecure()}");
        }

        private void OnSteamServerConnectFailure(SteamServerConnectFailure_t pCallback)
        {
            LoggerInstance.Error($"Failed to connect to Steam servers. Error: {pCallback.m_eResult} ({GetResultText(pCallback.m_eResult)})");
            LoggerInstance.Error($"Still retrying: {(pCallback.m_bStillRetrying ? "Yes" : "No")}");
        }

        private void OnSteamServersDisconnected(SteamServersDisconnected_t pCallback)
        {
            LoggerInstance.Warning($"Disconnected from Steam servers. Error: {pCallback.m_eResult} ({GetResultText(pCallback.m_eResult)})");
        }

        private void OnP2PSessionRequest(P2PSessionRequest_t pCallback)
        {
            // Accept all session requests, this is THE place for filtering access
            CSteamID steamIDClient = pCallback.m_steamIDRemote;
            LoggerInstance.Msg($"P2P Session request from: {steamIDClient}, Universe: {steamIDClient.GetEUniverse()}");
            bool accepted = SteamNetworking.AcceptP2PSessionWithUser(steamIDClient);
            LoggerInstance.Msg($"P2P Session accepted: {accepted}");
        }

        private void OnClientApproved(GSClientApprove_t pCallback)
        {
            CSteamID steamIDClient = pCallback.m_SteamID;
            LoggerInstance.Msg($"Client approved: {steamIDClient}, Universe: {steamIDClient.GetEUniverse()}");

            // Auth status (NEED TO CHANGE, PROBABLY DOESN'T WORK)
            EUserHasLicenseForAppResult authResult = SteamGameServer.UserHasLicenseForApp(steamIDClient, new AppId_t(_settings.AppID));
            LoggerInstance.Msg($"Client license check result: {authResult}");
        }

        private void OnClientDenied(GSClientDeny_t pCallback)
        {
            CSteamID steamIDClient = pCallback.m_SteamID;
            EDenyReason reason = pCallback.m_eDenyReason;
            LoggerInstance.Warning($"Client denied: {steamIDClient}, reason: {reason} ({GetDenyReasonText(reason)})");
        }

        private void OnClientKicked(GSClientKick_t pCallback)
        {
            CSteamID steamIDClient = pCallback.m_SteamID;
            EDenyReason reason = pCallback.m_eDenyReason;
            LoggerInstance.Warning($"Client kicked: {steamIDClient}, reason: {reason} ({GetDenyReasonText(reason)})");
        }

        // Debug callbacks
        private void OnValidateAuthTicket(ValidateAuthTicketResponse_t pCallback)
        {
            LoggerInstance.Msg($"Auth ticket validation: SteamID={pCallback.m_SteamID}, AuthSessionResponse={pCallback.m_eAuthSessionResponse}, OwnerSteamID={pCallback.m_OwnerSteamID}");
        }

        private void OnP2PConnectFail(P2PSessionConnectFail_t pCallback)
        {
            LoggerInstance.Error($"P2P Connection failed with {pCallback.m_steamIDRemote}: Error={pCallback.m_eP2PSessionError}");
        }

        private void OnGameServerChangeRequested(GameServerChangeRequested_t pCallback)
        {
            LoggerInstance.Msg($"Game server change requested: {pCallback.m_rgchServer}");
        }

        private void OnApplicationQuitHandler()
        {
            ShutdownGameServer();
        }

        public override void OnApplicationQuit()
        {
            ShutdownGameServer();
        }

        private void ShutdownGameServer()
        {
            if (_serverInitialized)
            {
                LoggerInstance.Msg("Shutting down Steam GameServer...");
                SteamGameServer.SetAdvertiseServerActive(false);
                SteamGameServer.LogOff();
                GameServer.Shutdown();
                _serverInitialized = false;
                LoggerInstance.Msg("Steam GameServer shutdown complete.");
            }
        }

        // EDenyReason TO Readable string
        private string GetDenyReasonText(EDenyReason reason)
        {
            switch (reason)
            {
                case EDenyReason.k_EDenyInvalid: return "Invalid";
                case EDenyReason.k_EDenyInvalidVersion: return "Invalid Version";
                case EDenyReason.k_EDenyGeneric: return "Generic Deny";
                case EDenyReason.k_EDenyNotLoggedOn: return "Not Logged On";
                case EDenyReason.k_EDenyNoLicense: return "No License";
                case EDenyReason.k_EDenyCheater: return "Cheater";
                case EDenyReason.k_EDenyLoggedInElseWhere: return "Logged In Elsewhere";
                case EDenyReason.k_EDenyUnknownText: return "Unknown Text";
                case EDenyReason.k_EDenyIncompatibleAnticheat: return "Incompatible Anticheat";
                case EDenyReason.k_EDenyMemoryCorruption: return "Memory Corruption";
                case EDenyReason.k_EDenyIncompatibleSoftware: return "Incompatible Software";
                case EDenyReason.k_EDenySteamConnectionLost: return "Steam Connection Lost";
                case EDenyReason.k_EDenySteamConnectionError: return "Steam Connection Error";
                case EDenyReason.k_EDenySteamResponseTimedOut: return "Steam Response Timed Out";
                case EDenyReason.k_EDenySteamValidationStalled: return "Steam Validation Stalled";
                default: return $"Unknown ({reason})";
            }
        }

        // EResult TO Readable string
        private string GetResultText(EResult result)
        {
            switch (result)
            {
                case EResult.k_EResultOK: return "Success";
                case EResult.k_EResultFail: return "Generic failure";
                case EResult.k_EResultNoConnection: return "No connection";
                case EResult.k_EResultInvalidPassword: return "Invalid password";
                case EResult.k_EResultLoggedInElsewhere: return "Logged in elsewhere";
                case EResult.k_EResultInvalidProtocolVer: return "Invalid protocol version";
                case EResult.k_EResultInvalidParam: return "Invalid parameter";
                case EResult.k_EResultFileNotFound: return "File not found";
                case EResult.k_EResultBusy: return "Busy";
                case EResult.k_EResultInvalidState: return "Invalid state";
                case EResult.k_EResultInvalidName: return "Invalid name";
                case EResult.k_EResultInvalidEmail: return "Invalid email";
                case EResult.k_EResultDuplicateName: return "Duplicate name";
                case EResult.k_EResultAccessDenied: return "Access denied";
                case EResult.k_EResultTimeout: return "Timeout";
                case EResult.k_EResultBanned: return "Banned";
                case EResult.k_EResultAccountNotFound: return "Account not found";
                case EResult.k_EResultInvalidSteamID: return "Invalid Steam ID";
                case EResult.k_EResultServiceUnavailable: return "Service unavailable";
                case EResult.k_EResultNotLoggedOn: return "Not logged on";
                case EResult.k_EResultPending: return "Pending";
                case EResult.k_EResultEncryptionFailure: return "Encryption failure";
                case EResult.k_EResultInsufficientPrivilege: return "Insufficient privilege";
                case EResult.k_EResultLimitExceeded: return "Limit exceeded";
                case EResult.k_EResultRevoked: return "Revoked";
                case EResult.k_EResultExpired: return "Expired";
                case EResult.k_EResultAlreadyRedeemed: return "Already redeemed";
                case EResult.k_EResultDuplicateRequest: return "Duplicate request";
                case EResult.k_EResultAlreadyOwned: return "Already owned";
                case EResult.k_EResultIPNotFound: return "IP not found";
                case EResult.k_EResultPersistFailed: return "Persist failed";
                case EResult.k_EResultLockingFailed: return "Locking failed";
                case EResult.k_EResultLogonSessionReplaced: return "Logon session replaced";
                case EResult.k_EResultConnectFailed: return "Connect failed";
                case EResult.k_EResultHandshakeFailed: return "Handshake failed";
                case EResult.k_EResultIOFailure: return "IO failure";
                case EResult.k_EResultRemoteDisconnect: return "Remote disconnect";
                case EResult.k_EResultShoppingCartNotFound: return "Shopping cart not found";
                case EResult.k_EResultBlocked: return "Blocked";
                case EResult.k_EResultIgnored: return "Ignored";
                case EResult.k_EResultNoMatch: return "No match";
                case EResult.k_EResultAccountDisabled: return "Account disabled";
                case EResult.k_EResultServiceReadOnly: return "Service read-only";
                case EResult.k_EResultAccountNotFeatured: return "Account not featured";
                case EResult.k_EResultAdministratorOK: return "Administrator OK";
                case EResult.k_EResultContentVersion: return "Content version";
                case EResult.k_EResultTryAnotherCM: return "Try another connection manager";
                case EResult.k_EResultPasswordRequiredToKickSession: return "Password required to kick session";
                case EResult.k_EResultAlreadyLoggedInElsewhere: return "Already logged in elsewhere";
                case EResult.k_EResultSuspended: return "Suspended";
                case EResult.k_EResultCancelled: return "Cancelled";
                case EResult.k_EResultDataCorruption: return "Data corruption";
                case EResult.k_EResultDiskFull: return "Disk full";
                case EResult.k_EResultRemoteCallFailed: return "Remote call failed";
                default: return $"Unknown ({result})";
            }
        }
    }
}