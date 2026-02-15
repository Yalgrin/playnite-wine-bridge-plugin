using System;
using System.Linq;
using Playnite.SDK.Models;
using WineBridgePlugin.Models;
using WineBridgePlugin.Processes;
using WineBridgePlugin.Settings;
using WineBridgePlugin.Utils;

namespace WineBridgePlugin.Integrations.Heroic
{
    public static class HeroicProcessStarter
    {
        public static ProcessWithCorrelationId Start(string command)
        {
            var strings = command?.Split('/');
            if (strings == null || strings.Length != 2)
            {
                throw new Exception("Heroic command is not in the expected format.");
            }

            return Start(strings[0], strings[1]);
        }

        private static ProcessWithCorrelationId Start(string runner, string id)
        {
            var heroicPlatform = runner.ToHeroicPlatform();
            var installedGames = HeroicClient.GetInstalledGames(heroicPlatform);
            var installPath = installedGames.FirstOrDefault(g => g.AppId == id)?.InstallPath;

            if (installPath == null)
            {
                throw new Exception("Could not find game installation directory");
            }

            return Start(runner, id, installPath);
        }

        public static ProcessWithCorrelationId Start(Game game, HeroicPlatform platform)
        {
            var platformId = game.GameId;
            var gameInstallDirectory = WineUtils.WindowsPathToLinux(game.InstallDirectory);
            if (gameInstallDirectory == null)
            {
                var games = HeroicClient.GetInstalledGames(platform);
                var matchingGame = games.FirstOrDefault(g => g.AppId == platformId);
                if (matchingGame != null)
                {
                    gameInstallDirectory = matchingGame.InstallPath;
                }
            }

            if (gameInstallDirectory == null)
            {
                throw new Exception("Could not find game installation directory");
            }

            var process = Start(platform.ToHeroicRunner(), platformId, gameInstallDirectory);
            return process;
        }

        private static ProcessWithCorrelationId Start(string runner, string id, string installPath)
        {
            var executablePath = WineBridgeSettings.HeroicExecutablePathLinux;
            if (executablePath == null)
            {
                throw new Exception("Heroic installation path not set.");
            }

            return LinuxProcessStarter.Start(
                $"{executablePath} --no-gui \"heroic://launch?appName={id}&runner={runner}\"", true,
                installPath.EscapeRegex());
        }
    }
}