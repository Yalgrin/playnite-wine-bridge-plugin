using System.Collections.Generic;

namespace WineBridgePlugin.Integrations.Steam
{
    public static class SteamUtils
    {
        public static readonly IReadOnlyList<string> ExcludedSteamIds = new List<string>
        {
            "858280", //Proton 3.7
            "930400", //Proton 3.7 Beta
            "961940", //Proton 3.16
            "996510", //Proton 3.16 Beta
            "1054830", //Proton 4.2
            "1113280", //Proton 4.11
            "1161040", //Proton BattlEye Runtime
            "1245040", //Proton 5.0
            "1420170", //Proton 5.13
            "1493710", //Proton Experimental
            "1580130", //Proton 6.3
            "1826330", //Proton EasyAntiCheat Runtime
            "1887720", //Proton 7.0
            "2180100", //Proton Hotfix
            "2230260", //Proton Next
            "2348590", //Proton 8.0
            "2805730", //Proton 9.0
            "3086180", //Proton Voice Files
            "3658110", //Proton 10.0
            "1070560", //Steam Linux Runtime 1.0 (scout) 
            "1391110", //Steam Linux Runtime 2.0 (soldier) 
            "1628350", //Steam Linux Runtime 3.0 (sniper) 
            "3810310", //Steam Linux Runtime 3.0 - Arm64 (sniper) 
            "4183110", //Steam Linux Runtime 4.0 
            "4185400" //Steam Linux Runtime 4.0 - Arm64 
        }.AsReadOnly();

        //https://github.com/ValveSoftware/steam-for-linux/issues/9463#issuecomment-2558366504
        public static void ExtractAppIdAndTrackingId(ulong steamAppId, bool nonSteam, out ulong appId,
            out ulong trackingId)
        {
            appId = steamAppId;
            trackingId = steamAppId;
            if (!nonSteam)
            {
                return;
            }

            if (steamAppId >= (ulong)1 << 32)
            {
                appId = steamAppId;
                trackingId = steamAppId >> 32;
            }
            else
            {
                trackingId = steamAppId;
                appId = (steamAppId << 32) | (1L << 25);
            }
        }
    }
}