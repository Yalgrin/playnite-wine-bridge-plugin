using System.IO;
using HarmonyLib;
using Playnite;

namespace WineBridgePlugin.Integrations.Steam
{
    public static class SteamGamesService
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        public static Dictionary<string, ImportableGame> GetInstalledGames(bool includeMods = true)
        {
            try
            {
                var serviceType = AccessTools.TypeByName("Steam.Games");
                if (serviceType == null)
                {
                    throw new Exception("Steam library classes not found!");
                }

                var method = AccessTools.Method(serviceType, "GetInstalledGames");
                if (method == null)
                {
                    throw new Exception("Steam library classes not found!");
                }

                if (method.Invoke(null, new object[] { includeMods }) is Dictionary<string, ImportableGame> result)
                {
                    return result;
                }

                return new Dictionary<string, ImportableGame>();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while getting installed Steam games!");
                throw;
            }
        }

        public static List<ImportableGame> GetNonSteamGames()
        {
            try
            {
                var result = new List<ImportableGame>();
                var userdataFolder = GetUserdataFolder();
                foreach (var userDirectory in Directory.GetDirectories(userdataFolder))
                {
                    var shortcutPath = Path.Combine(userDirectory, "config", "shortcuts.vdf");
                    if (!File.Exists(shortcutPath))
                    {
                        continue;
                    }

                    var vdfEntries = VDFParser.VDFParser.Parse(shortcutPath);
                    result.AddRange(vdfEntries.Select(vdfEntry =>
                        new ImportableGame(vdfEntry.AppName, "", $"{(ulong)(uint)vdfEntry.appid}")
                        {
                            InstallState = InstallState.Installed,
                            InstallDirectory = vdfEntry.StartDir
                        }));
                }

                return result;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while getting installed Non-Steam games!");
                throw;
            }
        }

        private static string GetUserdataFolder()
        {
            var steamType = AccessTools.TypeByName("Steam.SteamLauncher");
            if (steamType == null)
            {
                throw new Exception("Steam library classes not found!");
            }

            if (!(AccessTools.PropertyGetter(steamType, "InstallationDir").Invoke(null, new object[] { }) is string
                    installationPath))
            {
                throw new Exception("Steam installation path not found!");
            }

            return Path.Combine(installationPath, "userdata");
        }
    }
}