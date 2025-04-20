using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using ScheduleOne.Networking;
using ScheduleOne.Persistence;
using ScheduleOne.UI.MainMenu;
using Steamworks;
using UnityEngine;

namespace SteamGameServerMod.Patches;

[HarmonyPatch(typeof(Lobby))]
[SuppressMessage("Method Declaration", "Harmony003:Harmony non-ref patch parameters modified")]
public class LobbyPatches
{
    [HarmonyPatch(nameof(Lobby.OnLobbyCreated))]
    [HarmonyPrefix]
    public static bool OnLobbyCreated_Prefix(Lobby __instance) => false;

    [HarmonyPatch(nameof(Lobby.OnLobbyEntered))]
    [HarmonyPrefix]
    public static bool OnLobbyEntered_Prefix(Lobby __instance, LobbyEnter_t result)
    {
        if (Core.IsHost)
            return false;

        var enterResponse = (EChatRoomEnterResponse)result.m_EChatRoomEnterResponse;
        if (enterResponse == EChatRoomEnterResponse.k_EChatRoomEnterResponseFull)
        {
            LeaveLobbyWithStatusMessage(__instance, "Lobby full", "The lobby you have tried to enter is full!", isBad: true);
            return false;
        }

        var lobbyVersion = SteamMatchmaking.GetLobbyData((CSteamID)result.m_ulSteamIDLobby, "version");
        ScheduleOne.Console.Log($"Lobby version: {lobbyVersion}, client version: {Application.version}");
        if (lobbyVersion != Application.version)
        {
            LeaveLobbyWithStatusMessage(__instance, "Version Mismatch", $"Host version: {lobbyVersion}\nYour version: {Application.version}", isBad: true);
            return false;
        }

        ScheduleOne.Console.Log($"Entered lobby: {result.m_ulSteamIDLobby}");

        var isReady = SteamMatchmaking.GetLobbyData((CSteamID)result.m_ulSteamIDLobby, "ready") == "true";
        if (!isReady)
        {
            LeaveLobbyWithStatusMessage(__instance, "Lobby not ready", "Lobby is not ready yet, try again later.", isBad: true);
            return false;
        }

        var lobbyOwnerSteamId = SteamMatchmaking.GetLobbyOwner((CSteamID)result.m_ulSteamIDLobby).m_SteamID;
        LoadManager.Instance.LoadAsClient(lobbyOwnerSteamId.ToString());
        return false;
    }

    [HarmonyPatch(nameof(Lobby.IsHost), MethodType.Getter)]
    [HarmonyPrefix]
    public static void IsHost_Prefix(ref bool __result) => __result = Core.IsHost;

    static void LeaveLobbyWithStatusMessage(Lobby lobby, string title, string message, bool isBad = true)
    {
        ScheduleOne.Console.LogWarning(message);
        if (MainMenuPopup.InstanceExists)
            MainMenuPopup.Instance.Open(title, message, isBad);

        lobby.LeaveLobby();
    }
}
