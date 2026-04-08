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
                    $"{steamExecutable} -silent \"steam://launch/{steamAppId}/Dialog\"", true,
                    $"/reaper SteamLaunch AppId={trackingId}.*waitforexitandrun");
            }
            else
            {
                process = LinuxProcessStarter.Start(
                    $"{steamExecutable} -silent \"steam://rungameid/{steamAppId}\"", true,
                    $"/reaper SteamLaunch AppId={trackingId}.*waitforexitandrun");
            }

            return process;
        }
    }
}