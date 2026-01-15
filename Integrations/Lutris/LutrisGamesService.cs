using System.Collections.Generic;
using Playnite.SDK.Models;
using WineBridgePlugin.Utils;

namespace WineBridgePlugin.Integrations.Lutris
{
    public static class LutrisGamesService
    {
        internal static Dictionary<string, GameMetadata> GetInstalledGames(LutrisPlatform platform)
        {
            var games = LutrisClient.GetInstalledGames(platform);

            var result = new Dictionary<string, GameMetadata>();
            games?.ForEach(game =>
            {
                var source = GetSource(platform);
                var metadata = new GameMetadata
                {
                    Source = source != null ? new MetadataNameProperty(source) : null,
                    InstallDirectory = WineUtils.LinuxPathToWindows(game.InstallPath),
                    GameId = game.PlayniteGameId,
                    Name = game.ServiceName,
                    IsInstalled = true,
                    Platforms = new HashSet<MetadataProperty> { new MetadataSpecProperty("pc_windows") }
                };

                result.Add(metadata.GameId, metadata);
            });
            return result;
        }

        private static string GetSource(LutrisPlatform platform)
        {
            switch (platform)
            {
                case LutrisPlatform.Gog:
                    return "GOG";
                case LutrisPlatform.Amazon:
                    return "Amazon";
                case LutrisPlatform.Epic:
                    return "Epic";
                case LutrisPlatform.EaApp:
                    return "EA app";
                case LutrisPlatform.BattleNet:
                    return "Battle.net";
                case LutrisPlatform.ItchIo:
                    return "itch.io";
                default:
                    return null;
            }
        }

        public static bool IsGameInstalled(Game game, LutrisPlatform platform)
        {
            return LutrisClient.IsGameInstalled(platform, game.GameId);
        }
    }
}