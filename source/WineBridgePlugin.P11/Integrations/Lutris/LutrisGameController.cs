using Playnite;
using WineBridgePlugin.Processes;
using WineBridgePlugin.Settings;

namespace WineBridgePlugin.Integrations.Lutris;

public class LutrisInstallController(Game game, LutrisPlatform platform)
    : InstallController("lutris_install_primary", "Install using Lutris", game.Id)
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

    public override Task InstallAsync(InstallActionArgs args)
    {
        try
        {
            var executablePath = WineBridgeSettings.LutrisExecutablePathLinux;
            if (string.IsNullOrEmpty(executablePath))
            {
                throw new Exception("Lutris installation path not set.");
            }

            var service = LutrisUtils.GetLutrisService(platform);
            var id = LutrisClient.GetInstallId(platform, game.LibraryGameId);
            if (string.IsNullOrEmpty(id))
            {
                throw new Exception("Could not find game id");
            }

            LinuxProcessStarter.Start($"{executablePath} \"lutris:{service}:{id}\"");
            StartInstallWatcher();
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
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

                var installedGames = LutrisGamesService.GetInstalledGames(platform);
                var gameLibraryGameId = game.LibraryGameId;
                if (gameLibraryGameId != null && installedGames.TryGetValue(gameLibraryGameId, out var installedGame))
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

public class LutrisUninstallController(Game game, LutrisPlatform platform)
    : UninstallController("lutris_uninstall_primary", "Uninstall using Lutris", game.Id)
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
        var executablePath = WineBridgeSettings.LutrisExecutablePathLinux;
        if (executablePath == null)
        {
            throw new Exception("Lutris installation path not set.");
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

                var installedGames = LutrisGamesService.GetInstalledGames(platform);
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

public class LutrisPlayController(Game game, LutrisPlatform platform)
    : PlayController("lutris_play_primary", "Play using Lutris")
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
        var executablePath = WineBridgeSettings.LutrisExecutablePathLinux;
        if (executablePath == null)
        {
            throw new Exception("Lutris installation path not set.");
        }

        _watcherToken = new CancellationTokenSource();

        var process = LutrisProcessStarter.Start(game, platform);

        _ = LinuxProcessMonitor.TrackLinuxProcess(this, process, _watcherToken);

        await Task.CompletedTask;
    }
}