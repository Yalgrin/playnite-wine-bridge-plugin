using Playnite;
using WineBridgePlugin.Processes;
using WineBridgePlugin.Settings;

namespace WineBridgePlugin.Integrations.Heroic
{
    public class HeroicInstallController(Game game, HeroicPlatform platform)
        : InstallController("heroic_install_primary", "Install using Heroic", game.Id)
    {
        private CancellationTokenSource? _watcherToken;

        public override async ValueTask DisposeAsync()
        {
            if (_watcherToken != null)
            {
                await _watcherToken.CancelAsync();
                _watcherToken.Dispose();
            }
        }

        public override async Task InstallAsync(InstallActionArgs args)
        {
            var executablePath = WineBridgeSettings.HeroicExecutablePathLinux;
            if (executablePath == null)
            {
                throw new Exception("Heroic installation path not set.");
            }

            LinuxProcessStarter.Start($"{executablePath}");
            StartInstallWatcher();
        }

        private void StartInstallWatcher()
        {
            _watcherToken = new CancellationTokenSource();
            Task.Run<Task>(async () =>
            {
                while (true)
                {
                    if (_watcherToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var installedGames = HeroicGamesService.GetInstalledGames(platform);
                    var gameLibraryGameId = game.LibraryGameId;
                    if (gameLibraryGameId != null &&
                        installedGames.TryGetValue(gameLibraryGameId, out var installedGame))
                    {
                        await GameInstalledAsync(new GameInstalledArgs
                        {
                            InstallDirectory = installedGame.InstallDirectory
                        });
                        return;
                    }

                    await Task.Delay(10000);
                }
            });
        }
    }

    public class HeroicUninstallController(Game game, HeroicPlatform platform)
        : UninstallController("heroic_uninstall_primary", "Uninstall using Heroic", game.Id)
    {
        private CancellationTokenSource? _watcherToken;

        public override async ValueTask DisposeAsync()
        {
            if (_watcherToken != null)
            {
                await _watcherToken.CancelAsync();
                _watcherToken.Dispose();
            }
        }

        public override async Task UninstallAsync(UninstallActionArgs args)
        {
            var executablePath = WineBridgeSettings.HeroicExecutablePathLinux;
            if (executablePath == null)
            {
                throw new Exception("Heroic installation path not set.");
            }

            LinuxProcessStarter.Start($"{executablePath}");
            StartUninstallWatcher();
        }

        private void StartUninstallWatcher()
        {
            _watcherToken = new CancellationTokenSource();
            Task.Run<Task>(async () =>
            {
                while (true)
                {
                    if (_watcherToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var installedGames = HeroicGamesService.GetInstalledGames(platform);
                    var gameLibraryGameId = game.LibraryGameId;
                    if (gameLibraryGameId != null && !installedGames.ContainsKey(gameLibraryGameId))
                    {
                        await GameUninstalledAsync(new GameUninstalledArgs());
                        return;
                    }

                    await Task.Delay(5000);
                }
            });
        }
    }

    public class HeroicPlayController(Game game, HeroicPlatform platform)
        : PlayController("heroic_play_primary", "Play using Heroic")
    {
        private CancellationTokenSource? _watcherToken;

        public override async ValueTask DisposeAsync()
        {
            if (_watcherToken != null)
            {
                await _watcherToken.CancelAsync();
                _watcherToken.Dispose();
            }
        }

        public override async Task PlayAsync(PlayActionArgs args)
        {
            var executablePath = WineBridgeSettings.HeroicExecutablePathLinux;
            if (executablePath == null)
            {
                throw new Exception("Heroic installation path not set.");
            }

            _watcherToken = new CancellationTokenSource();

            var process = HeroicProcessStarter.Start(game, platform);

            _ = LinuxProcessMonitor.TrackLinuxProcess(this, process, _watcherToken);
        }
    }
}