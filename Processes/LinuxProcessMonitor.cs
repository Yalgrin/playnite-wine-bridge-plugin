using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using WineBridgePlugin.Models;
using WineBridgePlugin.Settings;
using WineBridgePlugin.Utils;

namespace WineBridgePlugin.Processes
{
    public static class LinuxProcessMonitor
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        public static Task TrackLinuxProcess(
            PlayController instance,
            ProcessWithCorrelationId process,
            CancellationTokenSource watcherToken
        )
        {
            return Task.Run(async () => { await DoTrackLinuxProcess(instance, process, watcherToken); });
        }

        public static string TrackLinuxProcessGetResult(
            ProcessWithCorrelationId process,
            CancellationTokenSource watcherToken = null
        )
        {
            Task.Run(async () => { await DoTrackLinuxProcess(process, watcherToken, 200, 200); }).Wait();

            var trackingDirectory = WineUtils.LinuxPathToWindows(WineBridgeSettings.TrackingDirectoryLinux);
            var processTrackingFile = $"{trackingDirectory}\\wine-bridge-{process.CorrelationId}";
            var processStatus = GetProcessStatus($"{processTrackingFile}-status");
            switch (processStatus)
            {
                case 0:
                    return GetProcessOutput($"{processTrackingFile}-output");
                case 1:
                    return string.Empty;
                default:
                    throw new Exception(GetProcessOutput($"{processTrackingFile}-error"));
            }
        }

        public static string[] TrackLinuxProcessGetResultInLines(
            ProcessWithCorrelationId process,
            CancellationTokenSource watcherToken = null
        )
        {
            Task.Run(async () => { await DoTrackLinuxProcess(process, watcherToken, 200, 200); }).Wait();

            var trackingDirectory = WineUtils.LinuxPathToWindows(WineBridgeSettings.TrackingDirectoryLinux);
            var processTrackingFile = $"{trackingDirectory}\\wine-bridge-{process.CorrelationId}";
            var processStatus = GetProcessStatus($"{processTrackingFile}-status");
            switch (processStatus)
            {
                case 0:
                    return GetProcessOutputLines($"{processTrackingFile}-output");
                case 1:
                    return new string[] { };
                default:
                    throw new Exception(GetProcessOutput($"{processTrackingFile}-error"));
            }
        }

        private static int GetProcessStatus(string statusFile)
        {
            try
            {
                var allLines = File.ReadAllLines(statusFile);
                return allLines.Length > 0 ? int.Parse(allLines[0]) : 1;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while reading process status file!");
                return -1;
            }
        }

        private static string GetProcessOutput(string outputFile)
        {
            try
            {
                return File.ReadAllText(outputFile);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while reading process output file!");
                throw;
            }
        }

        private static string[] GetProcessOutputLines(string outputFile)
        {
            try
            {
                return File.ReadAllLines(outputFile);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while reading process output file!");
                throw;
            }
        }

        private static async Task DoTrackLinuxProcess(PlayController instance, ProcessWithCorrelationId process,
            CancellationTokenSource watcherToken)
        {
            Stopwatch stopwatch = null;
            try
            {
                var trackingDirectory = WineUtils.LinuxPathToWindows(WineBridgeSettings.TrackingDirectoryLinux);
                var debugLogging = WineBridgeSettings.DebugLoggingEnabled;
                var processTrackingFile = $"{trackingDirectory}\\wine-bridge-{process.CorrelationId}";
                var readyTrackingFile = $"{processTrackingFile}-ready";

                var ready = false;
                for (var i = 0; i <= 90; i++)
                {
                    if (watcherToken.IsCancellationRequested)
                    {
                        Logger.Info("Cancel signal received, stopping ready file monitoring...");
                        return;
                    }

                    if (File.Exists(readyTrackingFile))
                    {
                        ready = true;
                        Logger.Debug($"{readyTrackingFile} exists");
                        break;
                    }

                    if (debugLogging)
                    {
                        Logger.Debug($"{readyTrackingFile} does not exist");
                    }

                    await Task.Delay(2000);
                }

                if (!ready)
                {
                    Logger.Warn(
                        "Ready signal not received! Process will not be monitored and stop event will be sent.");
                    InvokeStopEvent(instance, null);
                    return;
                }

                stopwatch = InvokeStartEvent(instance, process.Process);

                var running = true;
                while (running)
                {
                    if (watcherToken.IsCancellationRequested)
                    {
                        Logger.Info("Cancel signal received, stopping process monitoring...");
                        stopwatch?.Stop();
                        return;
                    }

                    running = File.Exists(processTrackingFile);
                    if (running)
                    {
                        await Task.Delay(2000);
                    }
                    else if (debugLogging)
                    {
                        Logger.Debug($"{processTrackingFile} does not exist");
                    }
                }

                InvokeStopEvent(instance, stopwatch);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while monitoring Linux process!");
                stopwatch?.Stop();
                throw;
            }
        }

        private static async Task DoTrackLinuxProcess(
            ProcessWithCorrelationId process,
            CancellationTokenSource watcherToken,
            int millisecondsDelay = 2000,
            int readyIterations = 90)
        {
            try
            {
                var trackingDirectory = WineUtils.LinuxPathToWindows(WineBridgeSettings.TrackingDirectoryLinux);
                var debugLogging = WineBridgeSettings.DebugLoggingEnabled;
                var processTrackingFile = $"{trackingDirectory}\\wine-bridge-{process.CorrelationId}";
                var readyTrackingFile = $"{processTrackingFile}-ready";

                var ready = false;
                for (var i = 0; i <= readyIterations; i++)
                {
                    if (watcherToken != null && watcherToken.IsCancellationRequested)
                    {
                        Logger.Info("Cancel signal received, stopping ready file monitoring...");
                        return;
                    }

                    if (File.Exists(readyTrackingFile))
                    {
                        ready = true;
                        Logger.Debug($"{readyTrackingFile} exists");
                        break;
                    }

                    if (debugLogging)
                    {
                        Logger.Debug($"{readyTrackingFile} does not exist");
                    }

                    await Task.Delay(millisecondsDelay);
                }

                if (!ready)
                {
                    Logger.Warn(
                        "Ready signal not received! Process will not be monitored and stop event will be sent.");
                    return;
                }

                var running = true;
                while (running)
                {
                    if (watcherToken != null && watcherToken.IsCancellationRequested)
                    {
                        Logger.Info("Cancel signal received, stopping process monitoring...");
                        return;
                    }

                    running = File.Exists(processTrackingFile);
                    if (running)
                    {
                        await Task.Delay(millisecondsDelay);
                    }
                    else if (debugLogging)
                    {
                        Logger.Debug($"{processTrackingFile} does not exist");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while monitoring Linux process!");
                throw;
            }
        }

        private static Stopwatch InvokeStartEvent(
            PlayController instance,
            Process process)
        {
            try
            {
                Logger.Debug("InvokeStartEvent");

                var stopwatch = Stopwatch.StartNew();

                AccessTools.Method(instance.GetType(), "InvokeOnStarted", new[] { typeof(GameStartedEventArgs) })
                    .Invoke(instance, new object[] { new GameStartedEventArgs { StartedProcessId = process.Id } });

                return stopwatch;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while invoking start event!");
                throw;
            }
        }

        private static void InvokeStopEvent(
            PlayController instance,
            Stopwatch stopwatch)
        {
            try
            {
                Logger.Debug("InvokeStopEvent");

                stopwatch?.Stop();
                AccessTools.Method(instance.GetType(), "InvokeOnStopped")
                    .Invoke(instance,
                        new object[]
                            { new GameStoppedEventArgs(Convert.ToUInt64(stopwatch?.Elapsed.TotalSeconds ?? 0)) });
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while invoking stop event!");
                throw;
            }
        }
    }
}