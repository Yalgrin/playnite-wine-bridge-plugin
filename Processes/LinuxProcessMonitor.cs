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
                for (var i = 0; i <= 31; i++)
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