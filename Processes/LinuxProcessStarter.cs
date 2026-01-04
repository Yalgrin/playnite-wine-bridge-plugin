using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Playnite.SDK;
using WineBridgePlugin.Models;
using WineBridgePlugin.Utils;

namespace WineBridgePlugin.Processes
{
    public static class LinuxProcessStarter
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        public static ProcessWithCorrelationId StartSteamApp(string steamAppId)
        {
            return StartSteamApp(steamAppId, steamAppId);
        }

        public static ProcessWithCorrelationId StartSteamApp(string steamAppId, string trackingId)
        {
            if (!(WineBridgePlugin.Settings?.SteamIntegrationEnabled ?? false))
            {
                throw new Exception("Wine Bridge Steam integration is not enabled.");
            }

            return Start($"{GetSteamExecutable()} -silent steam://rungameid/{steamAppId}", true,
                $"/reaper SteamLaunch AppId={trackingId}.*waitforexitandrun");
        }

        private static string GetSteamExecutable()
        {
            return WineBridgePlugin.Settings?.SteamExecutablePathLinux ?? "steam";
        }

        public static ProcessWithCorrelationId Start(string command, bool asyncTracking = false,
            string trackingExpression = "-")
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (directoryName == null)
            {
                throw new Exception("Could not determine plugin directory.");
            }

            var scriptPath = Path.Combine(directoryName, @"Resources\run-in-linux.bat");
            var encodedCommand = command.Base64Encode();
            var asyncTrackingStr = asyncTracking ? "1" : "0";
            var encodedTrackingExpression = asyncTracking ? trackingExpression.Base64Encode() : "-".Base64Encode();
            var linuxScript = $"{directoryName.WindowsPathToLinuxPath()}/Resources/run-in-linux.sh";
            var correlationId = DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid();
            var trackingDirectory = (WineBridgePlugin.Settings?.TrackingDirectoryLinux ?? "/tmp").Base64Encode();

            var debugLogging = WineBridgePlugin.Settings?.DebugLoggingEnabled ?? false;
            Logger.Info($"The following Linux command will be executed: {command}");
            if (asyncTracking && debugLogging)
            {
                Logger.Debug($"The command will use the following tracking expression: {trackingExpression}");
            }

            var process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments =
                $"/c {scriptPath} \"{encodedCommand}\" \"{linuxScript}\" \"{correlationId}\" \"{asyncTrackingStr}\" \"{encodedTrackingExpression}\" \"{trackingDirectory}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.Environment.Remove("LD_LIBRARY_PATH");

            if (debugLogging)
            {
                Logger.Debug($"Running process: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
            }

            process.Start();

            if (debugLogging)
            {
                var output = process.StandardOutput.ReadToEnd();
                if (output.Length > 0)
                {
                    Logger.Debug($"Process output: {output}");
                }

                var errorOutput = process.StandardError.ReadToEnd();
                if (errorOutput.Length > 0)
                {
                    Logger.Debug($"Process error output: {errorOutput}");
                }
            }

            return new ProcessWithCorrelationId
            {
                Process = process,
                CorrelationId = correlationId
            };
        }
    }
}