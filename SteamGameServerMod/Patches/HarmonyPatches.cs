using HarmonyLib;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using FishNet.Managing;
using FishNet;
using SteamGameServerMod.Settings;
using SteamGameServerMod.Logging;
using SteamGameServerMod.Client; // Added this import
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
using System.Collections.Generic;
using Steamworks;
using SteamGameServerMod.Managers;

namespace SteamGameServerMod.Patches
{
    [HarmonyPatch]
    public static class HarmonyPatches
    {
        // Patch to add Tugboat transport to Multipass on initialization
        [HarmonyPatch(typeof(Multipass), "Initialize")]
        public static class MultipassAddTugboat
        {
            [HarmonyPrefix]
            private static void Prefix(Multipass __instance, ref List<Transport> ____transports)
            {
                // Check if Tugboat is already present
                bool hasTugboat = false;
                foreach (var transport in ____transports)
                {
                    if (transport is Tugboat)
                    {
                        hasTugboat = true;
                        break;
                    }
                }

                if (!hasTugboat)
                {
                    Tugboat tugboat = __instance.gameObject.AddComponent<Tugboat>();
                    ____transports.Add(tugboat);
                    Log.LogInfo("Added Tugboat transport to Multipass");
                }
            }
        }

        // Patch to force Tugboat as client transport when connecting to dedicated servers
        [HarmonyPatch(typeof(Multipass))]
        [HarmonyPatch("SetClientTransport")]
        [HarmonyPatch(new System.Type[] { typeof(System.Type) })]
        public static class ForceClientTransport
        {
            [HarmonyPrefix]
            private static bool Prefix(Multipass __instance, System.Type transportType, ref Transport ____clientTransport)
            {
                // Only intercept if we're trying to connect to a dedicated server
                if (ClientConnectionManager.IsConnectingToDedicatedServer)
                {
                    ____clientTransport = __instance.GetComponent<Tugboat>();
                    Log.LogInfo("Forced Tugboat as client transport for dedicated server connection");
                    return false;
                }

                return true;
            }
        }

        // Patch to modify SteamMatchmaking.CreateLobby for dedicated servers
        [HarmonyPatch(typeof(SteamMatchmaking), "CreateLobby")]
        public static class SteamMatchmaking_CreateLobby
        {
            [HarmonyPrefix]
            private static void Prefix(ref ELobbyType eLobbyType, ref int cMaxMembers)
            {
                if (ServerSettings.ServerEnabled)
                {
                    eLobbyType = ELobbyType.k_ELobbyTypePublic;
                    Log.LogInfo($"Modified CreateLobby to use Public lobby type for dedicated server");
                }
            }
        }

        // Patch to handle player joining
        [HarmonyPatch(typeof(Player), "OnSpawned")]
        public static class PlayerJoinPatch
        {
            [HarmonyPostfix]
            public static void Postfix(Player __instance)
            {
                if (InstanceFinder.IsServer && !__instance.IsOwner)
                {
                    Log.LogInfo($"Player joined: {__instance.PlayerName} ({__instance.PlayerCode})");

                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.OnPlayerJoined(__instance);
                    }
                }
            }
        }

        // Patch to handle player leaving
        [HarmonyPatch(typeof(Player), "OnDespawned")]
        public static class PlayerLeavePatch
        {
            [HarmonyPostfix]
            public static void Postfix(Player __instance)
            {
                if (InstanceFinder.IsServer)
                {
                    Log.LogInfo($"Player left: {__instance.PlayerName} ({__instance.PlayerCode})");

                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.OnPlayerLeft(__instance);
                    }
                }
            }
        }
    }
}