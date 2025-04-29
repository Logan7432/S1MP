using SteamGameServerMod.Logging;

namespace SteamGameServerMod.Managers
{
    public static class ClientConnectionManager
    {
        public static bool IsConnectingToDedicatedServer { get; private set; }

        public static void SetDedicatedServerConnectionState(bool isConnecting)
        {
            IsConnectingToDedicatedServer = isConnecting;
            Log.LogInfo($"Dedicated server connection state: {isConnecting}");
        }
    }
}