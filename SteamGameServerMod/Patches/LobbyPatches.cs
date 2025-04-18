using HarmonyLib;
using ScheduleOne.Networking;

namespace SteamGameServerMod.Patches;

[HarmonyPatch(typeof(Lobby))]
public class LobbyPatches
{
    [HarmonyPatch(nameof(Lobby.OnLobbyCreated))]
    [HarmonyPatch(nameof(Lobby.OnLobbyEntered))]
    [HarmonyPrefix]
    public static bool OnLobbyCreated_Prefix(Lobby __instance) => false;
}
