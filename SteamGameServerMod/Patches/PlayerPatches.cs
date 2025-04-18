using HarmonyLib;
using ScheduleOne.PlayerScripts;

namespace SteamGameServerMod.Patches;

[HarmonyPatch(typeof(Player))]
public class PlayerPatches
{
    [HarmonyPatch(nameof(Player.RpcLogic___SendPlayerNameData_586648380))]
    [HarmonyPrefix]
    static bool RpcLogic___SendPlayerNameData_586648380_Prefix(Player __instance, string playerName, ulong id)
    {
        __instance.ReceivePlayerNameData(null, playerName, id!.ToString());
        __instance.PlayerName = playerName;
        __instance.PlayerCode = id!.ToString();
        return false;
    }
}
