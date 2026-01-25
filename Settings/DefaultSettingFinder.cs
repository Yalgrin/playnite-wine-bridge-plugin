using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using Playnite.SDK;
using WineBridgePlugin.Utils;

namespace WineBridgePlugin.Settings
{
    public static class DefaultSettingFinder
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        public static string HomeFolderLinux
        {
            get
            {
                if (GetHomeFolderFromWinePrefix(out var home))
                {
                    return home;
                }

                return GetHomeFolderFromUsernameVariable(out var result) ? result : null;
            }
        }

        public static string HomeFolderWindows => WineUtils.LinuxPathToWindows(HomeFolderLinux);

        private static bool GetHomeFolderFromWinePrefix(out string result)
        {
            var winePrefix = Environment.GetEnvironmentVariable("WINEPREFIX");

            if (winePrefix != null && winePrefix.StartsWith("/home/"))
            {
                var strings = Regex.Split(winePrefix, "/");
                if (strings.Length >= 2)
                {
                    var homeFolder = "/home/" + strings[2];
                    Logger.Debug($"Found home folder from username environment variable: {homeFolder}");
                    if (Directory.Exists(WineUtils.LinuxPathToWindows(homeFolder)))
                    {
                        result = homeFolder;
                        return true;
                    }
                }
            }

            Logger.Debug($"Could not find home folder from WINEPREFIX environment variable: {winePrefix}");
            result = null;
            return false;
        }

        private static bool GetHomeFolderFromUsernameVariable(out string result)
        {
            var usernameFromEnv = new HashSet<string>
            {
                Environment.GetEnvironmentVariable("USER"),
                Environment.GetEnvironmentVariable("USERNAME"),
                Environment.GetEnvironmentVariable("WINEUSERNAME")
            };
            var uniqueUsernames = usernameFromEnv.Where(x => x != null).Distinct().ToList();
            if (uniqueUsernames.Count == 1)
            {
                var homeFolder = "/home/" + uniqueUsernames[0];
                Logger.Debug($"Found home folder from username environment variable: {homeFolder}");
                if (Directory.Exists(WineUtils.LinuxPathToWindows(homeFolder)))
                {
                    result = homeFolder;
                    return true;
                }
            }

            Logger.Debug($"Could not find home folder from username environment variables: {uniqueUsernames.Join()}");
            result = null;
            return false;
        }

        public static LinuxSteamConfiguration SteamConfiguration
        {
            get
            {
                var homeFolder = HomeFolderWindows;
                if (homeFolder == null)
                {
                    Logger.Debug("Could not find home folder. Going to use a placeholder Steam configuration.");
                    return new LinuxSteamConfiguration
                    {
                        Type = "Placeholder",
                        ExecutablePath = "steam",
                        DataPath = "/home/user/.local/share/Steam"
                    };
                }

                if (GetNativeSteamConfiguration(homeFolder, out var nativeConfig))
                {
                    return nativeConfig;
                }

                if (GetFlatpakSteamConfiguration(homeFolder, out var flatpakConfig))
                {
                    return flatpakConfig;
                }

                Logger.Debug("Did not manage to find Steam configuration. Going to use a placeholder configuration.");
                return new LinuxSteamConfiguration
                {
                    Type = "Placeholder",
                    ExecutablePath = "steam",
                    DataPath = $"{HomeFolderLinux ?? "/home/user"}/.local/share/Steam"
                };
            }
        }

        private static bool GetNativeSteamConfiguration(string homeFolder,
            out LinuxSteamConfiguration steamConfiguration)
        {
            var dataFolder = Path.Combine(homeFolder, @".local\share\Steam");
            if (Directory.Exists(dataFolder) && Directory.Exists(Path.Combine(dataFolder, "steamapps")) &&
                Directory.Exists(Path.Combine(dataFolder, "userdata")))
            {
                Logger.Debug($"Found native Steam data folder at {dataFolder}");
                steamConfiguration = new LinuxSteamConfiguration
                {
                    Type = "Native",
                    ExecutablePath = "steam",
                    DataPath = WineUtils.WindowsPathToLinux(dataFolder)
                };
                return true;
            }

            Logger.Debug($"Could not find native Steam data folder at: {dataFolder}");
            steamConfiguration = null;
            return false;
        }

        private static bool GetFlatpakSteamConfiguration(string homeFolder,
            out LinuxSteamConfiguration steamConfiguration)
        {
            var dataFolder = Path.Combine(homeFolder, @".var\app\com.valvesoftware.Steam\.local\share\Steam");
            if (Directory.Exists(dataFolder)
                && Directory.Exists(Path.Combine(dataFolder, "steamapps")) &&
                Directory.Exists(Path.Combine(dataFolder, "userdata")))
            {
                Logger.Debug($"Found flatpak Steam data folder at {dataFolder}");
                steamConfiguration = new LinuxSteamConfiguration
                {
                    Type = "Flatpak",
                    ExecutablePath = "flatpak run com.valvesoftware.Steam",
                    DataPath = WineUtils.WindowsPathToLinux(dataFolder)
                };
                return true;
            }

            Logger.Debug($"Could not find flatpak Steam data folder at: {dataFolder}");
            steamConfiguration = null;
            return false;
        }

        public static LinuxHeroicConfiguration HeroicConfiguration
        {
            get
            {
                var homeFolder = HomeFolderWindows;
                if (homeFolder == null)
                {
                    Logger.Debug("Could not find home folder. Going to use a placeholder Heroic configuration.");
                    return new LinuxHeroicConfiguration
                    {
                        Type = "Placeholder",
                        ExecutablePath = "heroic",
                        DataPath = "/home/user/.config/heroic"
                    };
                }

                if (GetNativeHeroicConfiguration(homeFolder, out var nativeConfig))
                {
                    return nativeConfig;
                }

                if (GetFlatpakHeroicConfiguration(homeFolder, out var flatpakConfig))
                {
                    return flatpakConfig;
                }

                Logger.Debug("Did not manage to find Heroic configuration. Going to use a placeholder configuration.");
                return new LinuxHeroicConfiguration
                {
                    Type = "Placeholder",
                    ExecutablePath = "heroic",
                    DataPath = $"{HomeFolderLinux ?? "/home/user"}/.config/heroic"
                };
            }
        }

        private static bool GetNativeHeroicConfiguration(string homeFolder,
            out LinuxHeroicConfiguration heroicConfiguration)
        {
            var dataFolder = Path.Combine(homeFolder, @".config\heroic");
            if (Directory.Exists(dataFolder)
                && (Directory.Exists(Path.Combine(dataFolder, "gog_store"))
                    || Directory.Exists(Path.Combine(dataFolder, "nile_store"))
                    || Directory.Exists(Path.Combine(dataFolder, "legendaryConfig"))))
            {
                Logger.Debug($"Found native Heroic data folder at {dataFolder}");
                heroicConfiguration = new LinuxHeroicConfiguration
                {
                    Type = "Native",
                    ExecutablePath = "heroic",
                    DataPath = WineUtils.WindowsPathToLinux(dataFolder)
                };
                return true;
            }

            Logger.Debug($"Could not find native Heroic data folder at: {dataFolder}");
            heroicConfiguration = null;
            return false;
        }

        private static bool GetFlatpakHeroicConfiguration(string homeFolder,
            out LinuxHeroicConfiguration heroicConfiguration)
        {
            var dataFolder = Path.Combine(homeFolder, @".var\app\com.heroicgameslauncher.hgl\config\heroic");
            if (Directory.Exists(dataFolder)
                && (Directory.Exists(Path.Combine(dataFolder, "gog_store"))
                    || Directory.Exists(Path.Combine(dataFolder, "nile_store"))
                    || Directory.Exists(Path.Combine(dataFolder, "legendaryConfig"))))
            {
                Logger.Debug($"Found flatpak Heroic data folder at {dataFolder}");
                heroicConfiguration = new LinuxHeroicConfiguration
                {
                    Type = "Flatpak",
                    ExecutablePath = "flatpak run com.heroicgameslauncher.hgl",
                    DataPath = WineUtils.WindowsPathToLinux(dataFolder)
                };
                return true;
            }

            Logger.Debug($"Could not find flatpak Heroic data folder at: {dataFolder}");
            heroicConfiguration = null;
            return false;
        }

        public static LinuxLutrisConfiguration LutrisConfiguration
        {
            get
            {
                var homeFolder = HomeFolderWindows;
                if (homeFolder == null)
                {
                    Logger.Debug("Could not find home folder. Going to use a placeholder Lutris configuration.");
                    return new LinuxLutrisConfiguration
                    {
                        Type = "Placeholder",
                        ExecutablePath = "lutris",
                        DataPath = "/home/user/.local/share/lutris/"
                    };
                }

                if (GetNativeLutrisConfiguration(homeFolder, out var nativeConfig))
                {
                    return nativeConfig;
                }

                if (GetFlatpakLutrisConfiguration(homeFolder, out var flatpakConfig))
                {
                    return flatpakConfig;
                }

                Logger.Debug("Did not manage to find Lutris configuration. Going to use a placeholder configuration.");
                return new LinuxLutrisConfiguration
                {
                    Type = "Placeholder",
                    ExecutablePath = "lutris",
                    DataPath = $"{HomeFolderLinux ?? "/home/user"}/.local/share/lutris/"
                };
            }
        }

        private static bool GetNativeLutrisConfiguration(string homeFolder,
            out LinuxLutrisConfiguration heroicConfiguration)
        {
            var dataFolder = Path.Combine(homeFolder, @".local\share\lutris");
            if (Directory.Exists(dataFolder) && File.Exists(Path.Combine(dataFolder, "pga.db")))
            {
                Logger.Debug($"Found native Lutris data folder at {dataFolder}");
                heroicConfiguration = new LinuxLutrisConfiguration
                {
                    Type = "Native",
                    ExecutablePath = "lutris",
                    DataPath = WineUtils.WindowsPathToLinux(dataFolder)
                };
                return true;
            }

            Logger.Debug($"Could not find native Lutris data folder at: {dataFolder}");
            heroicConfiguration = null;
            return false;
        }

        private static bool GetFlatpakLutrisConfiguration(string homeFolder,
            out LinuxLutrisConfiguration heroicConfiguration)
        {
            var dataFolder = Path.Combine(homeFolder, @".var\app\net.lutris.Lutris\data\lutris");
            if (Directory.Exists(dataFolder) && File.Exists(Path.Combine(dataFolder, "pga.db")))
            {
                Logger.Debug($"Found flatpak Lutris data folder at {dataFolder}");
                heroicConfiguration = new LinuxLutrisConfiguration
                {
                    Type = "Flatpak",
                    ExecutablePath = "flatpak run net.lutris.Lutris",
                    DataPath = WineUtils.WindowsPathToLinux(dataFolder)
                };
                return true;
            }

            Logger.Debug($"Could not find flatpak Lutris data folder at: {dataFolder}");
            heroicConfiguration = null;
            return false;
        }
    }
}