using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK;
using WineBridgePlugin.Models;
using WineBridgePlugin.Settings;
using WineBridgePlugin.Utils;

namespace WineBridgePlugin.Processes
{
    public static class LinuxProcessStarter
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        public static LinuxProcess Start(Process originalProcess, string command, bool asyncTracking = false,
            string trackingExpression = "-")
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (directoryName == null)
            {
                throw new Exception("Could not determine plugin directory.");
            }

            var correlationId = DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid();
            var trackingDirectoryWindows = WineUtils.LinuxPathToWindows(WineBridgeSettings.TrackingDirectoryLinux);
            var processTrackingFile = $"{trackingDirectoryWindows}\\wine-bridge-{correlationId}";
            var outputTrackingFile = $"{processTrackingFile}-output";
            var errorTrackingFile = $"{processTrackingFile}-error";
            var inputTrackingFile = $"{processTrackingFile}-input";
            var linuxInputTrackingFile =
                $"{WineBridgeSettings.TrackingDirectoryLinux}/wine-bridge-{correlationId}-input";
            var statusTrackingFile = $"{processTrackingFile}-status";
            var pidTrackingFile = $"{processTrackingFile}-pid";
            var readyTrackingFile = $"{processTrackingFile}-ready";

            if (originalProcess?.StartInfo.RedirectStandardInput ?? false)
            {
                command = $"{command} < {linuxInputTrackingFile}";
            }

            var scriptPath = Path.Combine(directoryName, @"Resources\run-in-linux.bat");
            var encodedCommand = command.Base64Encode();
            var asyncTrackingStr = asyncTracking ? "1" : "0";
            var encodedTrackingExpression = asyncTracking ? trackingExpression.Base64Encode() : "-".Base64Encode();
            var linuxScript = WineUtils.ScriptPathLinux;
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
            process.StartInfo.RedirectStandardOutput = debugLogging;
            process.StartInfo.RedirectStandardError = debugLogging;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.Environment.Remove("LD_LIBRARY_PATH");
            process.StartInfo.Environment.Remove("OS");
            var list = process.StartInfo.Environment.Keys.Where(key => key.StartsWith("WINE")).ToList();
            list.ForEach(key => process.StartInfo.Environment.Remove(key));

            var runningLinuxProcess = new LinuxProcess
            {
                OriginalProcess = originalProcess,
                ScriptProcess = process,
                CorrelationId = correlationId,
                CancellationTokenSource = new CancellationTokenSource(),
                ProcessTrackingFile = processTrackingFile,
                OutputTrackingFile = outputTrackingFile,
                ErrorTrackingFile = errorTrackingFile,
                InputTrackingFile = inputTrackingFile,
                StatusTrackingFile = statusTrackingFile,
                PidTrackingFile = pidTrackingFile,
                ReadyTrackingFile = readyTrackingFile,
            };

            LinuxProcessMonitor.RegisterProcessToTrack(originalProcess, runningLinuxProcess);

            if (debugLogging)
            {
                Logger.Debug($"Running process: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
            }

            process.Start();

            if (debugLogging)
            {
                LogProcessOutputInBackground(process);
            }

            return runningLinuxProcess;
        }

        public static LinuxProcess Start(string command, bool asyncTracking = false,
            string trackingExpression = "-")
        {
            return Start(null, command, asyncTracking, trackingExpression);
        }

        public static Process StartRawCommand(string command)
        {
            var debugLogging = WineBridgeSettings.DebugLoggingEnabled;

            Logger.Info($"The following raw Linux command will be executed: {command}");

            var process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/c start /unix /bin/sh -c \"{command}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = debugLogging;
            process.StartInfo.RedirectStandardError = debugLogging;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.Environment.Remove("LD_LIBRARY_PATH");
            process.StartInfo.Environment.Remove("OS");
            var list = process.StartInfo.Environment.Keys.Where(key => key.StartsWith("WINE")).ToList();
            list.ForEach(key => process.StartInfo.Environment.Remove(key));

            if (debugLogging)
            {
                Logger.Debug($"Running process: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
            }

            process.Start();

            if (debugLogging)
            {
                LogProcessOutputInBackground(process);
            }

            return process;
        }

        private static void LogProcessOutputInBackground(Process process)
        {
            Task.Run(async () =>
            {
                try
                {
                    var stdoutTask = process.StandardOutput.ReadToEndAsync();
                    var stderrTask = process.StandardError.ReadToEndAsync();

                    await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);

                    process.WaitForExit();

                    var output = stdoutTask.Result;
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        Logger.Debug($"Process output: {output}");
                    }

                    var errorOutput = stderrTask.Result;
                    if (!string.IsNullOrWhiteSpace(errorOutput))
                    {
                        Logger.Debug($"Process error output: {errorOutput}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Debug(ex, "Error while reading process output!");
                }
            });
        }
    }
}