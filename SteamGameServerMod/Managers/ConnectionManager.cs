using System;
using UnityEngine;
using FishNet;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using SteamGameServerMod.Logging;

namespace SteamGameServerMod.Client
{
    public class ConnectionManager
    {
        private static bool _connecting = false;
        private static Action<bool> _connectionCallback;

        public static void ConnectToServer(string ip, int port, Action<bool> callback = null)
        {
            if (_connecting)
            {
                Log.LogWarning("Already connecting to a server. Cancel current connection first.");
                return;
            }

            _connecting = true;
            _connectionCallback = callback;

            try
            {
                // Configure connection
                Log.LogInfo($"Connecting to server: {ip}:{port}");

                // Set up Tugboat transport
                var networkManager = InstanceFinder.NetworkManager;
                var multipass = networkManager.TransportManager.Transport as Multipass;

                if (multipass == null)
                {
                    Log.LogError("Failed to find Multipass transport");
                    ConnectionFailed();
                    return;
                }

                // Find or add Tugboat transport
                Tugboat tugboat = multipass.GetComponent<Tugboat>();
                if (tugboat == null)
                {
                    tugboat = multipass.gameObject.AddComponent<Tugboat>();
                    Log.LogInfo("Added Tugboat transport component");
                }

                // Configure transport
                tugboat.SetServerBindAddress(ip, 0);
                tugboat.SetClientAddress(ip);
                tugboat.SetPort((ushort)port);

                // Set as client transport
                multipass.SetClientTransport<Tugboat>();

                // Register connection callbacks
                networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;

                // Start connection
                bool connectionStarted = networkManager.ClientManager.StartConnection();

                if (!connectionStarted)
                {
                    Log.LogError("Failed to start connection");
                    ConnectionFailed();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Error connecting to server: {ex.Message}");
                ConnectionFailed();
            }
        }

        public static void CancelConnection()
        {
            if (!_connecting)
                return;

            try
            {
                var networkManager = InstanceFinder.NetworkManager;
                if (networkManager != null)
                {
                    networkManager.ClientManager.StopConnection();
                    networkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
                }

                _connecting = false;
                _connectionCallback?.Invoke(false);
                _connectionCallback = null;

                Log.LogInfo("Connection cancelled");
            }
            catch (Exception ex)
            {
                Log.LogError($"Error cancelling connection: {ex.Message}");
            }
        }

        private static void ClientManager_OnClientConnectionState(ClientConnectionStateArgs args)
        {
            switch (args.ConnectionState)
            {
                case LocalConnectionState.Started:
                    Log.LogInfo("Connected to server");
                    ConnectionSucceeded();
                    break;

                case LocalConnectionState.Stopped:
                    Log.LogInfo("Disconnected from server");
                    if (_connecting)
                    {
                        ConnectionFailed();
                    }
                    break;

                case LocalConnectionState.Stopping:
                    Log.LogInfo("Connection stopping...");
                    break;
            }
        }

        private static void ConnectionSucceeded()
        {
            // Unregister event
            InstanceFinder.NetworkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;

            _connecting = false;
            _connectionCallback?.Invoke(true);
            _connectionCallback = null;

            Log.LogInfo("Connection successful");
        }

        private static void ConnectionFailed()
        {
            // Unregister event
            if (InstanceFinder.NetworkManager != null)
            {
                InstanceFinder.NetworkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
            }

            _connecting = false;
            _connectionCallback?.Invoke(false);
            _connectionCallback = null;

            Log.LogInfo("Connection failed");
        }
    }
}