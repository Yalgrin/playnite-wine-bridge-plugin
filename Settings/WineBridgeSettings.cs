namespace WineBridgePlugin.Settings
{
    public static class WineBridgeSettings
    {
        public static string TrackingDirectoryLinux => WineBridgePlugin.Settings?.TrackingDirectoryLinux ?? "/tmp";

        public static bool SetScriptExecutePermissions =>
            WineBridgePlugin.Settings?.SetScriptExecutePermissions ?? true;

        public static bool SteamIntegrationEnabled => WineBridgePlugin.Settings?.SteamIntegrationEnabled ?? false;

        public static string SteamDataPathLinux => WineBridgePlugin.Settings?.SteamDataPathLinux ??
                                                   DefaultSettingFinder.SteamConfiguration.DataPath;

        public static string SteamExecutablePathLinux => WineBridgePlugin.Settings?.SteamExecutablePathLinux ??
                                                         DefaultSettingFinder.SteamConfiguration.ExecutablePath;

        public static bool AnyHeroicIntegrationEnabled => HeroicGogIntegrationEnabled ||
                                                          HeroicAmazonIntegrationEnabled ||
                                                          HeroicEpicIntegrationEnabled;

        public static bool HeroicGogIntegrationEnabled =>
            WineBridgePlugin.Settings?.HeroicGogIntegrationEnabled ?? false;

        public static bool HeroicAmazonIntegrationEnabled =>
            WineBridgePlugin.Settings?.HeroicAmazonIntegrationEnabled ?? false;

        public static bool HeroicEpicIntegrationEnabled =>
            WineBridgePlugin.Settings?.HeroicEpicIntegrationEnabled ?? false;

        public static string HeroicDataPathLinux => WineBridgePlugin.Settings?.HeroicDataPathLinux ??
                                                    DefaultSettingFinder.HeroicConfiguration.DataPath;

        public static string HeroicExecutablePathLinux => WineBridgePlugin.Settings?.HeroicExecutablePathLinux ??
                                                          DefaultSettingFinder.HeroicConfiguration.ExecutablePath;

        public static bool DebugLoggingEnabled => WineBridgePlugin.Settings?.DebugLoggingEnabled ?? false;
    }
}