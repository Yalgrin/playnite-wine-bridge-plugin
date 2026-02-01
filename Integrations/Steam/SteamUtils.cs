namespace WineBridgePlugin.Integrations.Steam
{
    public static class SteamUtils
    {
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