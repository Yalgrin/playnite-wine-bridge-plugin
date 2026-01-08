using System;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using WineBridgePlugin.Processes;
using WineBridgePlugin.Settings;

namespace WineBridgePlugin.Integrations.Heroic
{
    public class HeroicInstallController : InstallController
    {
        private CancellationTokenSource _watcherToken;
        private readonly HeroicPlatform _platform;

        public HeroicInstallController(Game game, HeroicPlatform platform) : base(game)
        {
            _platform = platform;
            Name = "Install using Heroic";
        }

        public override void Dispose()
        {
            _watcherToken?.Dispose();
        }

        public override void Install(InstallActionArgs args)
        {
            var executablePath = WineBridgeSettings.HeroicExecutablePathLinux;
            if (executablePath == null)
            {
                throw new Exception("Heroic installation path not set.");
            }

            LinuxProcessStarter.Start($"{executablePath}");
            StartInstallWatcher();
        }

        public async void StartInstallWatcher()
        {
            _watcherToken = new CancellationTokenSource();
            await Task.Run(async () =>
            {
                while (true)
                {
                    if (_watcherToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var installedGames = HeroicGamesService.GetInstalledGames(_platform);
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

    public class HeroicUninstallController : UninstallController
    {
        private CancellationTokenSource _watcherToken;
        private readonly HeroicPlatform _platform;

        public HeroicUninstallController(Game game, HeroicPlatform platform) : base(game)
        {
            _platform = platform;
            Name = "Uninstall using Heroic";
        }

        public override void Dispose()
        {
            _watcherToken?.Dispose();
        }

        public override void Uninstall(UninstallActionArgs args)
        {
            var executablePath = WineBridgeSettings.HeroicExecutablePathLinux;
            if (executablePath == null)
            {
                throw new Exception("Heroic installation path not set.");
            }

            LinuxProcessStarter.Start($"{executablePath}");
            StartUninstallWatcher();
        }

        public async void StartUninstallWatcher()
        {
            _watcherToken = new CancellationTokenSource();
            await Task.Run(async () =>
            {
                while (true)
                {
                    if (_watcherToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var installedGames = HeroicGamesService.GetInstalledGames(_platform);
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

    public class HeroicPlayController : PlayController
    {
        private CancellationTokenSource _watcherToken;
        private readonly HeroicPlatform _platform;

        public HeroicPlayController(Game game, HeroicPlatform platform) : base(game)
        {
            _platform = platform;
            Name = "Play using Heroic";
        }

        public override void Dispose()
        {
            _watcherToken?.Dispose();
        }

        public override void Play(PlayActionArgs args)
        {
            var executablePath = WineBridgeSettings.HeroicExecutablePathLinux;
            if (executablePath == null)
            {
                throw new Exception("Heroic installation path not set.");
            }

            _watcherToken = new CancellationTokenSource();

            var process = HeroicProcessStarter.Start(Game, _platform);

            LinuxProcessMonitor.TrackLinuxProcess(this, process, _watcherToken);
        }
    }
}