using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Steamworks;

namespace SteamGameServerMod.Util
{
    internal class Utils
    {
        public static uint IpToUInt32(string ipAddress)
        {
            byte[] bytes = IPAddress.Parse(ipAddress).GetAddressBytes();
            uint ipAsUint = ((uint)bytes[0]) | ((uint)bytes[1] << 8) | ((uint)bytes[2] << 16) | ((uint)bytes[3] << 24);
            return ipAsUint;
        }

        // EDenyReason TO Readable string
        public static string GetDenyReasonText(EDenyReason reason)
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
        public static string GetResultText(EResult result)
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
