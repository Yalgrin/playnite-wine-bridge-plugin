using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace WineBridgePlugin.Integrations.Steam
{
    public static class SteamGamesService
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        public static Dictionary<string, GameMetadata> GetInstalledGames(bool includeMods = true)
        {
            try
            {
                var serviceType = AccessTools.TypeByName("SteamLibrary.Services.SteamLocalService");
                if (serviceType == null)
                {
                    throw new Exception("Steam library classes not found!");
                }

                var method = AccessTools.Method(serviceType, "GetInstalledGames");
                if (method == null)
                {
                    throw new Exception("Steam library classes not found!");
                }

                if (method.Invoke(null, new object[] { includeMods }) is Dictionary<string, GameMetadata> result)
                {
                    return result;
                }

                return new Dictionary<string, GameMetadata>();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while getting installed Steam games!");
                throw;
            }
        }

        public static List<GameMetadata> GetNonSteamGames()
        {
            try
            {
                var result = new List<GameMetadata>();
                var userdataFolder = GetUserdataFolder();
                foreach (var userDirectory in Directory.GetDirectories(userdataFolder))
                {
                    var shortcutPath = Path.Combine(userDirectory, "config", "shortcuts.vdf");
                    if (!File.Exists(shortcutPath))
                    {
                        continue;
                    }

                    var vdfEntries = VDFParser.VDFParser.Parse(shortcutPath);
                    result.AddRange(vdfEntries.Select(vdfEntry => new GameMetadata
                    {
                        GameId = $"{(ulong)(uint)vdfEntry.appid}",
                        Name = vdfEntry.AppName,
                        IsInstalled = true,
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
            var steamType = AccessTools.TypeByName("SteamLibrary.Steam");
            if (steamType == null)
            {
                throw new Exception("Steam library classes not found!");
            }

            if (!(AccessTools.PropertyGetter(steamType, "InstallationPath").Invoke(null, new object[] { }) is string
                    installationPath))
            {
                throw new Exception("Steam installation path not found!");
            }

            return Path.Combine(installationPath, "userdata");
        }
    }
}