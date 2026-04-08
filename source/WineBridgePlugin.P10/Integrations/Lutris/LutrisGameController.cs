using System;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using WineBridgePlugin.Processes;
using WineBridgePlugin.Settings;

namespace WineBridgePlugin.Integrations.Lutris
{
    public class LutrisInstallController : InstallController
    {
        private CancellationTokenSource _watcherToken;
        private readonly LutrisPlatform _platform;

        public LutrisInstallController(Game game, LutrisPlatform platform) : base(game)
        {
            _platform = platform;
            Name = "Install using Lutris";
        }

        public override void Dispose()
        {
            _watcherToken?.Dispose();
        }

        public override void Install(InstallActionArgs args)
        {
            var executablePath = WineBridgeSettings.LutrisExecutablePathLinux;
            if (executablePath == null)
            {
                throw new Exception("Lutris installation path not set.");
            }

            var service = LutrisUtils.GetLutrisService(_platform);
            var id = LutrisClient.GetInstallId(_platform, Game.GameId);
            if (string.IsNullOrEmpty(id))
            {
                throw new Exception("Could not find game id");
            }

            LinuxProcessStarter.Start($"{executablePath} \"lutris:{service}:{id}\"");
            StartInstallWatcher();
        }

        private void StartInstallWatcher()
        {
            _watcherToken = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (true)
                {
                    if (_watcherToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var installedGames = LutrisGamesService.GetInstalledGames(_platform);
                    if (installedGames.TryGetValue(Game.GameId, out var installedGame))
                    {
                        var installInfo = new GameInstallationData
                        {
                            InstallDirectory = installedGame.InstallDirectory
                        };

                        InvokeOnInstalled(new GameInstalledEventArgs(installInfo));
                        return;
                    }

                    await Task.Delay(10000);
                }
            });
        }
    }

    public class LutrisUninstallController : UninstallController
    {
        private CancellationTokenSource _watcherToken;
        private readonly LutrisPlatform _platform;

        public LutrisUninstallController(Game game, LutrisPlatform platform) : base(game)
        {
            _platform = platform;
            Name = "Uninstall using Lutris";
        }

        public override void Dispose()
        {
            _watcherToken?.Dispose();
        }

        public override void Uninstall(UninstallActionArgs args)
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
            Task.Run(async () =>
            {
                while (true)
                {
                    if (_watcherToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var installedGames = LutrisGamesService.GetInstalledGames(_platform);
                    if (!installedGames.ContainsKey(Game.GameId))
                    {
                        InvokeOnUninstalled(new GameUninstalledEventArgs());
                        return;
                    }

                    await Task.Delay(5000);
                }
            });
        }
    }

    public class LutrisPlayController : PlayController
    {
        private CancellationTokenSource _watcherToken;
        private readonly LutrisPlatform _platform;

        public LutrisPlayController(Game game, LutrisPlatform platform) : base(game)
        {
            _platform = platform;
            Name = "Play using Lutris";
        }

        public override void Dispose()
        {
            _watcherToken?.Dispose();
        }

        public override void Play(PlayActionArgs args)
        {
            var executablePath = WineBridgeSettings.LutrisExecutablePathLinux;
            if (executablePath == null)
            {
                throw new Exception("Lutris installation path not set.");
            }

            _watcherToken = new CancellationTokenSource();

            var process = LutrisProcessStarter.Start(Game, _platform);

            LinuxProcessMonitor.TrackLinuxProcess(this, process, _watcherToken);
        }
    }
}