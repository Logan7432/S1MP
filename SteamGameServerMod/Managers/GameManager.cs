using System;
using UnityEngine;
using ScheduleOne.PlayerScripts;
using SteamGameServerMod.Logging;

namespace SteamGameServerMod
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;

        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void OnPlayerJoined(Player player)
        {
            // Send notification to all players
            Log.LogInfo($"Player joined: {player.PlayerName}");

            // Broadcast message to all clients
            SendChatMessage($"<color=#5dd42f>{player.PlayerName} joined the server!</color>");
        }

        public void OnPlayerLeft(Player player)
        {
            // Send notification to all players
            Log.LogInfo($"Player left: {player.PlayerName}");

            // Broadcast message to all clients
            SendChatMessage($"<color=#bd1c1c>{player.PlayerName} left the server!</color>");
        }

        private void SendChatMessage(string message)
        {
            // Implementation to send message to all clients via RPC
            // This would depend on your networking setup
        }
    }
}