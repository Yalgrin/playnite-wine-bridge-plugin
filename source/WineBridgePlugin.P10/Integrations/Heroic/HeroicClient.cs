using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Playnite.SDK;
using WineBridgePlugin.Settings;

namespace WineBridgePlugin.Integrations.Heroic
{
    public static class HeroicClient
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        public static List<HeroicInstalledGame> GetInstalledGames()
        {
            var result = new List<HeroicInstalledGame>();
            foreach (var platform in Enum.GetValues(typeof(HeroicPlatform)))
            {
                GetInstalledGames((HeroicPlatform)platform).ForEach(game => result.Add(game));
            }

            return result;
        }

        public static List<HeroicInstalledGame> GetInstalledGames(HeroicPlatform platform)
        {
            switch (platform)
            {
                case HeroicPlatform.Gog:
                    return GetInstalledGogGames();
                case HeroicPlatform.Amazon:
                    return GetInstallAmazonGames();
                case HeroicPlatform.Epic:
                    return GetInstallEpicGames();
                case HeroicPlatform.Custom:
                    return GetInstallCustomGames();
                default:
                    return new List<HeroicInstalledGame>();
            }
        }

        private static List<HeroicInstalledGame> GetInstalledGogGames()
        {
            var path = WineBridgeSettings.HeroicDataPathLinux;
            if (path == null)
            {
                Logger.Warn("Heroic installation path not set.");
                return new List<HeroicInstalledGame>();
            }

            var installedPath = Path.Combine(path, @"gog_store\installed.json");
            var libraryPath = Path.Combine(path, @"store_cache\gog_library.json");
            if (!File.Exists(installedPath) || !File.Exists(libraryPath))
            {
                Logger.Warn("GOG data in Heroic not found.");
                return new List<HeroicInstalledGame>();
            }

            var installedGogGames =
                JsonConvert.DeserializeObject<HeroicInstalledGogGames>(File.ReadAllText(installedPath))
                    ?.InstalledGames ??
                new List<HeroicInstalledGogGame>();
            if (installedGogGames.Count == 0)
            {
                return new List<HeroicInstalledGame>();
            }

            var libraryGogGames =
                JsonConvert.DeserializeObject<HeroicLibraryGogGames>(File.ReadAllText(libraryPath))?.Games ??
                new List<HeroicLibraryGogGame>();
            var libraryGamesMap = libraryGogGames.ToDictionary(game => game.AppId, game => game);
            var result = new List<HeroicInstalledGame>();
            foreach (var game in installedGogGames)
            {
                if (game.AppId == null || game.InstallPath == null)
                {
                    continue;
                }

                var libraryGame = libraryGamesMap[game.AppId];
                if (libraryGame?.Name != null)
                {
                    result.Add(new HeroicInstalledGame
                    {
                        AppId = game.AppId,
                        InstallPath = game.InstallPath,
                        Name = libraryGame.Name,
                        Platform = HeroicPlatform.Gog
                    });
                }
            }

            return result;
        }

        private static List<HeroicInstalledGame> GetInstallAmazonGames()
        {
            var path = WineBridgeSettings.HeroicDataPathLinux;
            if (path == null)
            {
                Logger.Warn("Heroic installation path not set.");
                return new List<HeroicInstalledGame>();
            }

            var installedPath = Path.Combine(path, @"nile_config\nile\installed.json");
            var libraryPath = Path.Combine(path, @"nile_config\nile\library.json");
            if (!File.Exists(installedPath) || !File.Exists(libraryPath))
            {
                Logger.Warn("Amazon data in Heroic not found.");
                return new List<HeroicInstalledGame>();
            }

            var installedGogGames =
                JsonConvert.DeserializeObject<HeroicInstalledAmazonGames>(File.ReadAllText(installedPath)) ??
                new List<HeroicInstalledAmazonGame>();
            if (installedGogGames.Count == 0)
            {
                return new List<HeroicInstalledGame>();
            }

            var libraryGogGames =
                JsonConvert.DeserializeObject<HeroicLibraryAmazonGames>(File.ReadAllText(libraryPath)) ??
                new List<HeroicLibraryAmazonGame>();
            var libraryGamesMap = libraryGogGames.Where(g => g.Product?.AppId != null)
                .ToDictionary(game => game.Product.AppId, game => game);
            var result = new List<HeroicInstalledGame>();
            foreach (var game in installedGogGames)
            {
                if (game.AppId == null || game.InstallPath == null)
                {
                    continue;
                }

                var libraryGame = libraryGamesMap[game.AppId];
                if (libraryGame?.Product?.Name != null)
                {
                    result.Add(new HeroicInstalledGame
                    {
                        AppId = game.AppId,
                        InstallPath = game.InstallPath,
                        Name = libraryGame.Product.Name,
                        Platform = HeroicPlatform.Amazon
                    });
                }
            }

            return result;
        }

        private static List<HeroicInstalledGame> GetInstallEpicGames()
        {
            var path = WineBridgeSettings.HeroicDataPathLinux;
            if (path == null)
            {
                Logger.Warn("Heroic installation path not set.");
                return new List<HeroicInstalledGame>();
            }

            var installedPath = Path.Combine(path, @"legendaryConfig\legendary\installed.json");
            if (!File.Exists(installedPath))
            {
                Logger.Warn("Epic data in Heroic not found.");
                return new List<HeroicInstalledGame>();
            }

            var installedGogGames =
                JsonConvert.DeserializeObject<HeroicInstalledEpicGames>(File.ReadAllText(installedPath))?.Values
                    .ToList() ??
                new List<HeroicInstalledEpicGame>();
            if (installedGogGames.Count == 0)
            {
                return new List<HeroicInstalledGame>();
            }

            var result = new List<HeroicInstalledGame>();
            foreach (var game in installedGogGames)
            {
                if (game.AppId == null || game.InstallPath == null || game.Name == null)
                {
                    continue;
                }

                result.Add(new HeroicInstalledGame
                {
                    AppId = game.AppId,
                    InstallPath = game.InstallPath,
                    Name = game.Name,
                    Platform = HeroicPlatform.Epic
                });
            }

            return result;
        }

        private static List<HeroicInstalledGame> GetInstallCustomGames()
        {
            var path = WineBridgeSettings.HeroicDataPathLinux;
            if (path == null)
            {
                Logger.Warn("Heroic installation path not set.");
                return new List<HeroicInstalledGame>();
            }

            var installedPath = Path.Combine(path, @"sideload_apps\library.json");
            if (!File.Exists(installedPath))
            {
                Logger.Warn("Custom game data in Heroic not found.");
                return new List<HeroicInstalledGame>();
            }

            var installedGogGames =
                JsonConvert.DeserializeObject<HeroicInstalledCustomGames>(File.ReadAllText(installedPath))
                    ?.InstalledGames ??
                new List<HeroicInstalledCustomGame>();
            if (installedGogGames.Count == 0)
            {
                return new List<HeroicInstalledGame>();
            }

            var result = new List<HeroicInstalledGame>();
            foreach (var game in installedGogGames)
            {
                if (game.AppId == null || game.InstallPath == null || game.Name == null)
                {
                    continue;
                }

                result.Add(new HeroicInstalledGame
                {
                    AppId = game.AppId,
                    InstallPath = game.InstallPath,
                    Name = game.Name,
                    Platform = HeroicPlatform.Custom
                });
            }

            return result;
        }
    }

    public class HeroicInstalledGame
    {
        public string AppId { get; set; }
        public string InstallPath { get; set; }
        public string Name { get; set; }
        public HeroicPlatform Platform { get; set; }
    }


    public class HeroicInstalledGogGames
    {
        [JsonProperty(PropertyName = "installed")]
        public List<HeroicInstalledGogGame> InstalledGames { get; set; }
    }

    public class HeroicInstalledGogGame
    {
        [JsonProperty(PropertyName = "appName")]
        public string AppId { get; set; }

        [JsonProperty(PropertyName = "install_path")]
        public string InstallPath { get; set; }
    }

    public class HeroicLibraryGogGames
    {
        [JsonProperty(PropertyName = "games")] public List<HeroicLibraryGogGame> Games { get; set; }
    }

    public class HeroicLibraryGogGame
    {
        [JsonProperty(PropertyName = "app_name")]
        public string AppId { get; set; }

        [JsonProperty(PropertyName = "title")] public string Name { get; set; }
    }


    public class HeroicInstalledAmazonGames : List<HeroicInstalledAmazonGame>
    {
    }

    public class HeroicInstalledAmazonGame
    {
        [JsonProperty(PropertyName = "id")] public string AppId { get; set; }

        [JsonProperty(PropertyName = "path")] public string InstallPath { get; set; }
    }

    public class HeroicLibraryAmazonGames : List<HeroicLibraryAmazonGame>
    {
    }

    public class HeroicLibraryAmazonGame
    {
        [JsonProperty(PropertyName = "product")]
        public HeroicLibraryAmazonProduct Product { get; set; }
    }

    public class HeroicLibraryAmazonProduct
    {
        [JsonProperty(PropertyName = "id")] public string AppId { get; set; }

        [JsonProperty(PropertyName = "title")] public string Name { get; set; }
    }

    public class HeroicInstalledEpicGames : Dictionary<string, HeroicInstalledEpicGame>
    {
    }

    public class HeroicInstalledEpicGame
    {
        [JsonProperty(PropertyName = "app_name")]
        public string AppId { get; set; }

        [JsonProperty(PropertyName = "title")] public string Name { get; set; }

        [JsonProperty(PropertyName = "install_path")]
        public string InstallPath { get; set; }
    }

    public class HeroicInstalledCustomGames
    {
        [JsonProperty(PropertyName = "games")] public List<HeroicInstalledCustomGame> InstalledGames { get; set; }
    }

    public class HeroicInstalledCustomGame
    {
        [JsonProperty(PropertyName = "app_name")]
        public string AppId { get; set; }

        [JsonProperty(PropertyName = "title")] public string Name { get; set; }

        [JsonProperty(PropertyName = "folder_name")]
        public string InstallPath { get; set; }
    }

    public enum HeroicPlatform
    {
        Gog,
        Amazon,
        Epic,
        Custom
    }
}