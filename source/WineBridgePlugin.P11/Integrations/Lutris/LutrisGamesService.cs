using Playnite;
using WineBridgePlugin.Utils;

namespace WineBridgePlugin.Integrations.Lutris
{
    public static class LutrisGamesService
    {
        private static readonly IdImportableProperty GogSourceProperty = new IdImportableProperty("gog", "GOG");
        private static readonly SpecImportableProperty PcSpecProperty = new SpecImportableProperty("pc_windows");

        internal static Dictionary<string, ImportableGame> GetInstalledGames(LutrisPlatform platform)
        {
            var games = LutrisClient.GetInstalledGames(platform);

            var result = new Dictionary<string, ImportableGame>();
            games.ForEach(game =>
            {
                var gameName = game.Name;
                var playniteGameId = game.PlayniteGameId;
                if (gameName == null || playniteGameId == null)
                {
                    return;
                }

                var metadata = new ImportableGame(gameName, platform == LutrisPlatform.Gog ? "Crow.GOG" : "",
                    playniteGameId)
                {
                    Source = GogSourceProperty,
                    InstallDirectory = WineUtils.LinuxPathToWindows(game.InstallPath),
                    InstallState = InstallState.Installed,
                    Platforms = [PcSpecProperty],
                    ExternalIdentifiers = platform == LutrisPlatform.Gog
                        ? [new ImportableExternalIdentifier("gog", "GOG", playniteGameId)]
                        : []
                };

                result.Add(metadata.GameId, metadata);
            });
            return result;
        }

        public static bool IsGameInstalled(Game game, LutrisPlatform platform)
        {
            return LutrisClient.IsGameInstalled(platform, game.LibraryGameId);
        }
    }
}