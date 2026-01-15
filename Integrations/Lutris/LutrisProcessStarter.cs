using System;
using Playnite.SDK.Models;
using WineBridgePlugin.Models;
using WineBridgePlugin.Processes;
using WineBridgePlugin.Settings;

namespace WineBridgePlugin.Integrations.Lutris
{
    public static class LutrisProcessStarter
    {
        public static ProcessWithCorrelationId StartUsingId(long id)
        {
            var executablePath = WineBridgeSettings.LutrisExecutablePathLinux;
            if (executablePath == null)
            {
                throw new Exception("Lutris installation path not set.");
            }

            return LinuxProcessStarter.Start(
                $"LUTRIS_SKIP_INIT=1 {executablePath} lutris:rungameid/{id} & disown", true,
                "lutris-wrapper");
        }

        public static ProcessWithCorrelationId Start(Game game, LutrisPlatform platform)
        {
            var gameId = LutrisClient.GetGameId(platform, game.GameId);
            if (gameId == null)
            {
                throw new Exception("Failed to find game id!");
            }

            return StartUsingId(gameId.Value);
        }
    }
}