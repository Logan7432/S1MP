using System;
using System.Collections.Generic;
using System.Text;
using MelonLoader;
using SteamGameServerMod.Settings;
using SteamGameServerMod.Util;
using Steamworks;

namespace SteamGameServerMod.Callbacks
{
    internal class CallbackHandler
    {
        private readonly MelonLogger.Instance _logger;
        private readonly GameServerSettings _settings;

        // Server callbacks
        private Callback<SteamServersConnected_t> _serverConnectedCallback;
        private Callback<SteamServerConnectFailure_t> _serverConnectFailureCallback;
        private Callback<SteamServersDisconnected_t> _serverDisconnectedCallback;

        // Client callbacks
        private Callback<P2PSessionRequest_t> _p2pSessionRequestCallback;
        private Callback<GSClientApprove_t> _clientApproveCallback;
        private Callback<GSClientDeny_t> _clientDenyCallback;
        private Callback<GSClientKick_t> _clientKickCallback;

        // Debug callbacks for testing
        private Callback<ValidateAuthTicketResponse_t> _validateAuthCallback;
        private Callback<P2PSessionConnectFail_t> _p2pConnectFailCallback;
        private Callback<GameServerChangeRequested_t> _gameServerChangeCallback;

        public CallbackHandler(MelonLogger.Instance logger, GameServerSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public void RegisterCallbacks()
        {
            // Register server callbacks
            _serverConnectedCallback = Callback<SteamServersConnected_t>.Create(OnSteamServersConnected);
            _serverConnectFailureCallback = Callback<SteamServerConnectFailure_t>.Create(OnSteamServerConnectFailure);
            _serverDisconnectedCallback = Callback<SteamServersDisconnected_t>.Create(OnSteamServersDisconnected);

            // Register client callbacks
            _p2pSessionRequestCallback = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
            _clientApproveCallback = Callback<GSClientApprove_t>.Create(OnClientApproved);
            _clientDenyCallback = Callback<GSClientDeny_t>.Create(OnClientDenied);
            _clientKickCallback = Callback<GSClientKick_t>.Create(OnClientKicked);

            // Register debug callbacks
            _validateAuthCallback = Callback<ValidateAuthTicketResponse_t>.Create(OnValidateAuthTicket);
            _p2pConnectFailCallback = Callback<P2PSessionConnectFail_t>.Create(OnP2PConnectFail);
            _gameServerChangeCallback = Callback<GameServerChangeRequested_t>.Create(OnGameServerChangeRequested);

            _logger.Msg("All Steam callbacks registered successfully");
        }

        private void OnSteamServersConnected(SteamServersConnected_t pCallback)
        {
            _logger.Msg($"Connected to Steam servers. Server ID: {SteamGameServer.GetSteamID()}");
            _logger.Msg($"Server is now visible in server browser: {SteamGameServer.BSecure()}");
        }

        private void OnSteamServerConnectFailure(SteamServerConnectFailure_t pCallback)
        {
            _logger.Error($"Failed to connect to Steam servers. Error: {pCallback.m_eResult} ({Utils.GetResultText(pCallback.m_eResult)})");
            _logger.Error($"Still retrying: {(pCallback.m_bStillRetrying ? "Yes" : "No")}");
        }

        private void OnSteamServersDisconnected(SteamServersDisconnected_t pCallback)
        {
            _logger.Warning($"Disconnected from Steam servers. Error: {pCallback.m_eResult} ({Utils.GetResultText(pCallback.m_eResult)})");
        }

        private void OnP2PSessionRequest(P2PSessionRequest_t pCallback)
        {
            // Accept all session requests, this is THE place for filtering access
            CSteamID steamIDClient = pCallback.m_steamIDRemote;
            _logger.Msg($"P2P Session request from: {steamIDClient}, Universe: {steamIDClient.GetEUniverse()}");
            bool accepted = SteamNetworking.AcceptP2PSessionWithUser(steamIDClient);
            _logger.Msg($"P2P Session accepted: {accepted}");
        }

        private void OnClientApproved(GSClientApprove_t pCallback)
        {
            CSteamID steamIDClient = pCallback.m_SteamID;
            _logger.Msg($"Client approved: {steamIDClient}, Universe: {steamIDClient.GetEUniverse()}");

            // Auth status (NEED TO CHANGE, PROBABLY DOESN'T WORK)
            EUserHasLicenseForAppResult authResult = SteamGameServer.UserHasLicenseForApp(steamIDClient, new AppId_t(_settings.AppID));
            _logger.Msg($"Client license check result: {authResult}");
        }

        private void OnClientDenied(GSClientDeny_t pCallback)
        {
            CSteamID steamIDClient = pCallback.m_SteamID;
            EDenyReason reason = pCallback.m_eDenyReason;
            _logger.Warning($"Client denied: {steamIDClient}, reason: {reason} ({Utils.GetDenyReasonText(reason)})");
        }

        private void OnClientKicked(GSClientKick_t pCallback)
        {
            CSteamID steamIDClient = pCallback.m_SteamID;
            EDenyReason reason = pCallback.m_eDenyReason;
            _logger.Warning($"Client kicked: {steamIDClient}, reason: {reason} ({Utils.GetDenyReasonText(reason)})");
        }

        // Debug callbacks
        private void OnValidateAuthTicket(ValidateAuthTicketResponse_t pCallback)
        {
            _logger.Msg($"Auth ticket validation: SteamID={pCallback.m_SteamID}, AuthSessionResponse={pCallback.m_eAuthSessionResponse}, OwnerSteamID={pCallback.m_OwnerSteamID}");
        }

        private void OnP2PConnectFail(P2PSessionConnectFail_t pCallback)
        {
            _logger.Error($"P2P Connection failed with {pCallback.m_steamIDRemote}: Error={pCallback.m_eP2PSessionError}");
        }

        private void OnGameServerChangeRequested(GameServerChangeRequested_t pCallback)
        {
            _logger.Msg($"Game server change requested: {pCallback.m_rgchServer}");
        }
    }
}
