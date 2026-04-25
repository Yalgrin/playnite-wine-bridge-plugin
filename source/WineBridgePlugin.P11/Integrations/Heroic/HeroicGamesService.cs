using Playnite;
using WineBridgePlugin.Utils;
using IdImportableProperty = Playnite.IdImportableProperty;
using ImportableExternalIdentifier = Playnite.ImportableExternalIdentifier;
using ImportableGame = Playnite.ImportableGame;
using InstallState = Playnite.InstallState;
using SpecImportableProperty = Playnite.SpecImportableProperty;

namespace WineBridgePlugin.Integrations.Heroic
{
    public static class HeroicGamesService
    {
        private static readonly IdImportableProperty GogSourceProperty = new IdImportableProperty("gog", "GOG");
        private static readonly SpecImportableProperty PcSpecProperty = new SpecImportableProperty("pc_windows");

        internal static Dictionary<string, ImportableGame> GetInstalledGames(HeroicPlatform platform)
        {
            var games = HeroicClient.GetInstalledGames(platform);

            var result = new Dictionary<string, ImportableGame>();
            games?.ForEach(game =>
            {
                var metadata =
                    new ImportableGame(game.Name, platform == HeroicPlatform.Gog ? "Crow.GOG" : "", game.AppId)
                    {
                        Source = GogSourceProperty,
                        InstallDirectory = WineUtils.LinuxPathToWindows(game.InstallPath),
                        InstallState = InstallState.Installed,
                        Platforms = [PcSpecProperty],
                        ExternalIdentifiers = platform == HeroicPlatform.Gog
                            ? [new ImportableExternalIdentifier("gog", "GOG", game.AppId)]
                            : []
                    };

                result.Add(metadata.GameId, metadata);
            });
            return result;
        }

        public static bool IsGameInstalled(Game game, HeroicPlatform platform)
        {
            var installedGames = HeroicClient.GetInstalledGames(platform);
            return installedGames.Any(e => e.AppId == game.LibraryGameId);
        }
    }
}