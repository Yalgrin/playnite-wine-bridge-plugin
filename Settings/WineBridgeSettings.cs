using System.Collections.Generic;
using System.Linq;

namespace WineBridgePlugin.Settings
{
    public static class WineBridgeSettings
    {
        public static string TrackingDirectoryLinux => WineBridgePlugin.Settings?.TrackingDirectoryLinux ?? "/tmp";

        public static bool SetScriptExecutePermissions =>
            WineBridgePlugin.Settings?.SetScriptExecutePermissions ?? true;

        public static bool RedirectExplorerCallsToLinux =>
            WineBridgePlugin.Settings?.RedirectExplorerCallsToLinux ?? true;

        public static bool RedirectProtocolCallsToLinux =>
            WineBridgePlugin.Settings?.RedirectProtocolCallsToLinux ?? true;

        public static bool ForceHighQualityIcons =>
            WineBridgePlugin.Settings?.ForceHighQualityIcons ?? false;

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


        public static bool AnyLutrisIntegrationEnabled => LutrisGogIntegrationEnabled ||
                                                          LutrisAmazonIntegrationEnabled ||
                                                          LutrisEpicIntegrationEnabled ||
                                                          LutrisEaIntegrationEnabled ||
                                                          LutrisBattleNetIntegrationEnabled ||
                                                          LutrisItchIoIntegrationEnabled;

        public static bool LutrisGogIntegrationEnabled =>
            WineBridgePlugin.Settings?.LutrisGogIntegrationEnabled ?? false;

        public static bool LutrisAmazonIntegrationEnabled =>
            WineBridgePlugin.Settings?.LutrisAmazonIntegrationEnabled ?? false;

        public static bool LutrisEpicIntegrationEnabled =>
            WineBridgePlugin.Settings?.LutrisEpicIntegrationEnabled ?? false;

        public static bool LutrisEaIntegrationEnabled =>
            WineBridgePlugin.Settings?.LutrisEaIntegrationEnabled ?? false;

        public static bool LutrisBattleNetIntegrationEnabled =>
            WineBridgePlugin.Settings?.LutrisBattleNetIntegrationEnabled ?? false;

        public static bool LutrisItchIoIntegrationEnabled =>
            WineBridgePlugin.Settings?.LutrisItchIoIntegrationEnabled ?? false;

        public static string LutrisDataPathLinux => WineBridgePlugin.Settings?.LutrisDataPathLinux ??
                                                    DefaultSettingFinder.LutrisConfiguration.DataPath;

        public static string LutrisExecutablePathLinux => WineBridgePlugin.Settings?.LutrisExecutablePathLinux ??
                                                          DefaultSettingFinder.LutrisConfiguration.ExecutablePath;

        public static List<WineBridgeEmulatorConfig> EmulatorConfigs =>
            WineBridgePlugin.Settings?.EmulatorConfigs?.ToList() ??
            new List<WineBridgeEmulatorConfig>();

        public static bool DebugLoggingEnabled => WineBridgePlugin.Settings?.DebugLoggingEnabled ?? false;

        public static bool AnyGogIntegrationEnabled =>
            LutrisGogIntegrationEnabled || HeroicGogIntegrationEnabled;

        public static bool AnyAmazonIntegrationEnabled =>
            LutrisAmazonIntegrationEnabled || HeroicAmazonIntegrationEnabled;

        public static bool AnyEpicIntegrationEnabled =>
            LutrisEpicIntegrationEnabled || HeroicEpicIntegrationEnabled;
    }
}