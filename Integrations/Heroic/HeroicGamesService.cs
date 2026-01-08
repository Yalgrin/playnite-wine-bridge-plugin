using System.Collections.Generic;
using Playnite.SDK.Models;
using WineBridgePlugin.Utils;

namespace WineBridgePlugin.Integrations.Heroic
{
    public static class HeroicGamesService
    {
        internal static Dictionary<string, GameMetadata> GetInstalledGames(HeroicPlatform platform)
        {
            var games = HeroicClient.GetInstalledGames(platform);

            var result = new Dictionary<string, GameMetadata>();
            games?.ForEach(game =>
            {
                var source = GetSource(platform);
                var metadata = new GameMetadata
                {
                    Source = source != null ? new MetadataNameProperty(source) : null,
                    InstallDirectory = WineUtils.LinuxPathToWindows(game.InstallPath),
                    GameId = game.AppId,
                    Name = game.Name,
                    IsInstalled = true,
                    Platforms = new HashSet<MetadataProperty> { new MetadataSpecProperty("pc_windows") }
                };

                result.Add(metadata.GameId, metadata);
            });
            return result;
        }

        private static string GetSource(HeroicPlatform platform)
        {
            switch (platform)
            {
                case HeroicPlatform.Gog:
                    return "GOG";
                case HeroicPlatform.Amazon:
                    return "Amazon";
                case HeroicPlatform.Epic:
                    return "Epic";
                default:
                    return null;
            }
        }
    }
}