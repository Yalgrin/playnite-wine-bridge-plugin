using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Playnite.SDK;
using WineBridgePlugin.Models;
using WineBridgePlugin.Settings;
using WineBridgePlugin.Utils;

namespace WineBridgePlugin.Processes
{
    public static class LinuxProcessStarter
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

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
            var linuxScript = WineUtils.WindowsPathToLinux(Path.Combine(directoryName, @"Resources\run-in-linux.sh"));
            var correlationId = DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid();
            var trackingDirectory = WineBridgeSettings.TrackingDirectoryLinux.Base64Encode();

            var debugLogging = WineBridgeSettings.DebugLoggingEnabled;
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

        public static Process StartRawCommand(string command)
        {
            var debugLogging = WineBridgeSettings.DebugLoggingEnabled;

            Logger.Info($"The following raw Linux command will be executed: {command}");

            var process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/c start /unix /bin/sh -c \"{command}\"";
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

            return process;
        }
    }
}