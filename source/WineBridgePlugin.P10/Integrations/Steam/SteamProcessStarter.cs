using WineBridgePlugin.Models;
using WineBridgePlugin.Processes;
using WineBridgePlugin.Settings;

namespace WineBridgePlugin.Integrations.Steam
{
    public static class SteamProcessStarter
    {
        public static LinuxProcess Start(string steamAppId, bool shouldShowLaunchDialog = false)
        {
            return Start(steamAppId, steamAppId, shouldShowLaunchDialog);
        }

        public static LinuxProcess Start(string steamAppId, string trackingId,
            bool shouldShowLaunchDialog = false)
        {
            var steamExecutable = WineBridgeSettings.SteamExecutablePathLinux;
            LinuxProcess process;
            if (shouldShowLaunchDialog)
            {
                process = LinuxProcessStarter.Start(
                    $"{steamExecutable} -silent \"steam://launch/{steamAppId}/Dialog\"",
                    ProcessTrackingMode.AsynchronousContinuous,
                    $"/reaper SteamLaunch AppId={trackingId} ");
            }
            else
            {
                process = LinuxProcessStarter.Start(
                    $"{steamExecutable} -silent \"steam://rungameid/{steamAppId}\"",
                    ProcessTrackingMode.AsynchronousContinuous,
                    $"/reaper SteamLaunch AppId={trackingId} ");
            }

            return process;
        }
    }
}