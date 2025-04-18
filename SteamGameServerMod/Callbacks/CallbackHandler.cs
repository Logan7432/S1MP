using SteamGameServerMod.Logging;
using SteamGameServerMod.Settings;
using SteamGameServerMod.Util;
using Steamworks;
using UnityEngine;

namespace SteamGameServerMod.Callbacks
{
    internal class CallbackHandler(GameServerSettings settings)
    {
        public static bool LobbyGameEntered;

        public void RegisterCallbacks()
        {
            // Register server callbacks
            Callback<SteamServersConnected_t>.Create(OnSteamServersConnected);
            Callback<SteamServerConnectFailure_t>.Create(OnSteamServerConnectFailure);
            Callback<SteamServersDisconnected_t>.Create(OnSteamServersDisconnected);

            // Register client callbacks
            Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
            Callback<GSClientApprove_t>.Create(OnClientApproved);
            Callback<GSClientDeny_t>.Create(OnClientDenied);
            Callback<GSClientKick_t>.Create(OnClientKicked);

            // Register debug callbacks
            Callback<ValidateAuthTicketResponse_t>.Create(OnValidateAuthTicket);
            Callback<P2PSessionConnectFail_t>.Create(OnP2PConnectFail);
            Callback<GameServerChangeRequested_t>.Create(OnGameServerChangeRequested);

            Callback<LobbyChatUpdate_t>.Create(lobbyChatUpdate =>
            {
                Log.LogInfo($"LobbyChatUpdate: LobbyID: {lobbyChatUpdate.m_ulSteamIDLobby} {lobbyChatUpdate.m_ulSteamIDUserChanged} {(EChatMemberStateChange)lobbyChatUpdate.m_rgfChatMemberStateChange}");
            });

            Callback<LobbyCreated_t>.Create(lobbyCreated =>
            {
                Log.LogInfo($"Lobby created: {lobbyCreated.m_eResult} {lobbyCreated.m_ulSteamIDLobby}");

                SteamMatchmaking.SetLobbyData((CSteamID)lobbyCreated.m_ulSteamIDLobby, "version", settings.GameVersion);
                SteamMatchmaking.SetLobbyData((CSteamID)lobbyCreated.m_ulSteamIDLobby, "ready", "true");

                // Communicate to Steam master server that this server is active and should be advertised on server browser
                SteamGameServer.SetAdvertiseServerActive(true);

                SteamMatchmaking.SetLobbyGameServer((CSteamID)lobbyCreated.m_ulSteamIDLobby, 0, settings.GamePort, (CSteamID)0ul);
            });

            Callback<LobbyGameCreated_t>.Create(_ =>
            {
                Log.LogInfo("Lobby game created");
                LobbyGameEntered = true;
            });

            Log.LogInfo("All Steam callbacks registered successfully");
        }

        private void OnSteamServersConnected(SteamServersConnected_t pCallback)
        {
            Log.LogInfo($"Connected to Steam servers. Server ID: {SteamGameServer.GetSteamID()}");
            Log.LogInfo($"Server is now visible in server browser: {SteamGameServer.BSecure()}");
        }

        private void OnSteamServerConnectFailure(SteamServerConnectFailure_t pCallback)
        {
            Log.LogError($"Failed to connect to Steam servers. Error: {pCallback.m_eResult} ({Utils.GetResultText(pCallback.m_eResult)})");
            Log.LogError($"Still retrying: {(pCallback.m_bStillRetrying ? "Yes" : "No")}");
        }

        private void OnSteamServersDisconnected(SteamServersDisconnected_t pCallback)
        {
            Log.LogWarning($"Disconnected from Steam servers. Error: {pCallback.m_eResult} ({Utils.GetResultText(pCallback.m_eResult)})");
        }

        private void OnP2PSessionRequest(P2PSessionRequest_t pCallback)
        {
            // Accept all session requests, this is THE place for filtering access
            var steamIDClient = pCallback.m_steamIDRemote;
            Log.LogInfo($"P2P Session request from: {steamIDClient}, Universe: {steamIDClient.GetEUniverse()}");
            var accepted = SteamNetworking.AcceptP2PSessionWithUser(steamIDClient);
            Log.LogInfo($"P2P Session accepted: {accepted}");
        }

        private void OnClientApproved(GSClientApprove_t pCallback)
        {
            var steamIDClient = pCallback.m_SteamID;
            Log.LogInfo($"Client approved: {steamIDClient}, Universe: {steamIDClient.GetEUniverse()}");

            // Auth status (NEED TO CHANGE, PROBABLY DOESN'T WORK)
            var authResult = SteamGameServer.UserHasLicenseForApp(steamIDClient, new AppId_t(settings.AppID));
            Log.LogInfo($"Client license check result: {authResult}");
        }

        private void OnClientDenied(GSClientDeny_t pCallback)
        {
            var steamIDClient = pCallback.m_SteamID;
            var reason = pCallback.m_eDenyReason;
            Log.LogWarning($"Client denied: {steamIDClient}, reason: {reason} ({Utils.GetDenyReasonText(reason)})");
        }

        private void OnClientKicked(GSClientKick_t pCallback)
        {
            var steamIDClient = pCallback.m_SteamID;
            var reason = pCallback.m_eDenyReason;
            Log.LogWarning($"Client kicked: {steamIDClient}, reason: {reason} ({Utils.GetDenyReasonText(reason)})");
        }

        // Debug callbacks
        private void OnValidateAuthTicket(ValidateAuthTicketResponse_t pCallback)
        {
            Log.LogInfo($"Auth ticket validation: SteamID={pCallback.m_SteamID}, AuthSessionResponse={pCallback.m_eAuthSessionResponse}, OwnerSteamID={pCallback.m_OwnerSteamID}");
        }

        private void OnP2PConnectFail(P2PSessionConnectFail_t pCallback)
        {
            Log.LogError($"P2P Connection failed with {pCallback.m_steamIDRemote}: Error={pCallback.m_eP2PSessionError}");
        }

        private void OnGameServerChangeRequested(GameServerChangeRequested_t pCallback)
        {
            Log.LogInfo($"Game server change requested: {pCallback.m_rgchServer}");
        }
    }
}
