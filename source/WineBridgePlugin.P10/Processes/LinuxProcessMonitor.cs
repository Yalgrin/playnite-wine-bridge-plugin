using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using WineBridgePlugin.Models;
using WineBridgePlugin.Settings;

namespace WineBridgePlugin.Processes
{
    public static class LinuxProcessMonitor
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        private static readonly Dictionary<Process, LinuxProcess> TrackedProcesses =
            new Dictionary<Process, LinuxProcess>();

        private static readonly Dictionary<Process, RunningLinuxProcessData> TrackedProcessData =
            new Dictionary<Process, RunningLinuxProcessData>();

        public static void RegisterProcessToTrack(Process originalProcess, LinuxProcess linuxProcess)
        {
            if (originalProcess == null || linuxProcess == null || !WineBridgeSettings.AdvancedProcessIntegration)
            {
                return;
            }

            TrackedProcesses[originalProcess] = linuxProcess;
            var runningLinuxProcessData = new RunningLinuxProcessData();

            if (originalProcess.StartInfo.RedirectStandardInput)
            {
                var fieldInfo =
                    typeof(Process).GetField("standardInput", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfo != null)
                {
                    runningLinuxProcessData.InputPipe =
                        new LinuxProcessInputPipe(linuxProcess.InputTrackingFile);

                    var previousStream = fieldInfo.GetValue(originalProcess);
                    fieldInfo.SetValue(originalProcess,
                        new StreamWriter(runningLinuxProcessData.InputPipe.WriteStream, Console.InputEncoding, 4096)
                    );

                    runningLinuxProcessData.InputPipe.Token.Register(() =>
                    {
                        fieldInfo.SetValue(originalProcess, previousStream);
                    });
                    runningLinuxProcessData.InputPipe.Start(linuxProcess.CancellationTokenSource.Token);
                }
                else
                {
                    Logger.Warn("Standard input field not found! Redirection will not work!");
                }
            }

            if (originalProcess.StartInfo.RedirectStandardOutput)
            {
                var fieldInfo =
                    typeof(Process).GetField("standardOutput", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfo != null)
                {
                    var encoding = originalProcess.StartInfo.StandardOutputEncoding ?? Console.OutputEncoding;
                    var pipe = new LinuxProcessOutputPipe(linuxProcess.OutputTrackingFile, pollMs: 100);
                    runningLinuxProcessData.OutputPipe = pipe;

                    var previousStream = fieldInfo.GetValue(originalProcess);
                    fieldInfo.SetValue(originalProcess,
                        new StreamReader(
                            pipe.ReadStream, encoding, true,
                            4096)
                    );

                    pipe.Token.Register(() => { fieldInfo.SetValue(originalProcess, previousStream); });
                    pipe.Start(linuxProcess.CancellationTokenSource.Token);
                }
                else
                {
                    Logger.Warn("Standard output field not found! Redirection will not work!");
                }
            }

            if (originalProcess.StartInfo.RedirectStandardError)
            {
                var fieldInfo =
                    typeof(Process).GetField("standardError", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfo != null)
                {
                    var encoding = originalProcess.StartInfo.StandardErrorEncoding ?? Console.OutputEncoding;
                    var pipe = new LinuxProcessOutputPipe(linuxProcess.ErrorTrackingFile, pollMs: 100);
                    runningLinuxProcessData.ErrorPipe = pipe;

                    var previousStream = fieldInfo.GetValue(originalProcess);
                    fieldInfo.SetValue(originalProcess,
                        new StreamReader(
                            pipe.ReadStream, encoding, true,
                            4096)
                    );

                    pipe.Token.Register(() => { fieldInfo.SetValue(originalProcess, previousStream); });
                    pipe.Start(linuxProcess.CancellationTokenSource.Token);
                }
            }

            TrackedProcessData[originalProcess] = runningLinuxProcessData;

            Task.Run(async () =>
            {
                while (true)
                {
                    if (File.Exists(linuxProcess.StatusTrackingFile))
                    {
                        Logger.Debug("Cancelling for process: " + originalProcess.Id + " " +
                                     originalProcess.StartInfo.FileName +
                                     " " + originalProcess.StartInfo.Arguments);
                        linuxProcess.CancellationTokenSource.Cancel();
                        break;
                    }

                    await Task.Delay(500);
                }
            });
        }

        public static bool GetProcessId(Process originalProcess, out int processId)
        {
            if (!WineBridgeSettings.AdvancedProcessIntegration)
            {
                processId = -1;
                return false;
            }

            if (TrackedProcesses.TryGetValue(originalProcess, out var linuxProcess))
            {
                if (File.Exists(linuxProcess.PidTrackingFile))
                {
                    processId = -Convert.ToInt32(File
                        .ReadAllText(linuxProcess.PidTrackingFile)
                        .Trim());
                }
                else
                {
                    var task = Task.Run(async () =>
                    {
                        var stopwatch = Stopwatch.StartNew();

                        while (!File.Exists(linuxProcess.PidTrackingFile) &&
                               (stopwatch.ElapsedMilliseconds < 15_000))
                        {
                            await Task.Delay(100);
                        }

                        stopwatch.Stop();

                        if (File.Exists(linuxProcess.PidTrackingFile))
                        {
                            return -Convert.ToInt32(File
                                .ReadAllText(linuxProcess.PidTrackingFile)
                                .Trim());
                        }

                        return -1;
                    });
                    task.Wait();
                    processId = task.Result;
                }

                return true;
            }

            processId = -1;
            return false;
        }

        public static bool GetExitCode(Process originalProcess, out int status)
        {
            if (!WineBridgeSettings.AdvancedProcessIntegration)
            {
                status = -1;
                return false;
            }

            if (TrackedProcesses.TryGetValue(originalProcess, out var linuxProcess))
            {
                status = Convert.ToInt32(File
                    .ReadAllText(linuxProcess.StatusTrackingFile)
                    .Trim());
                return true;
            }

            status = -1;
            return false;
        }

        public static bool HasExited(Process originalProcess, out bool status)
        {
            if (!WineBridgeSettings.AdvancedProcessIntegration)
            {
                status = false;
                return false;
            }

            if (TrackedProcesses.TryGetValue(originalProcess, out var linuxProcess))
            {
                status = File.Exists(linuxProcess.StatusTrackingFile);
                return true;
            }

            status = false;
            return false;
        }

        public static bool WaitForExit(Process originalProcess, int milliseconds, out bool result)
        {
            if (!WineBridgeSettings.AdvancedProcessIntegration)
            {
                result = false;
                return false;
            }

            if (TrackedProcesses.TryGetValue(originalProcess, out var linuxProcess))
            {
                var task = Task.Run(async () =>
                {
                    var stopwatch = Stopwatch.StartNew();

                    while (!File.Exists(linuxProcess.StatusTrackingFile) &&
                           (milliseconds < 0 || stopwatch.ElapsedMilliseconds < milliseconds))
                    {
                        await Task.Delay(100);
                    }

                    if (milliseconds < 0 && TrackedProcessData.TryGetValue(originalProcess, out var processData))
                    {
                        if (processData.InputPipe != null)
                        {
                            await processData.InputPipe.WaitForDone();
                        }

                        if (processData.OutputPipe != null)
                        {
                            await processData.OutputPipe.WaitForDone();
                        }

                        if (processData.ErrorPipe != null)
                        {
                            await processData.ErrorPipe.WaitForDone();
                        }
                    }

                    stopwatch.Stop();

                    return File.Exists(linuxProcess.StatusTrackingFile);
                });
                task.Wait();
                result = task.Result;
                return true;
            }

            result = false;
            return false;
        }

        public static bool Kill(Process originalProcess)
        {
            if (!WineBridgeSettings.AdvancedProcessIntegration)
            {
                return false;
            }

            if (GetProcessId(originalProcess, out var processId) && processId < -1)
            {
                LinuxProcessStarter.Start(null, $"kill -9 {Math.Abs(processId)}");
                return true;
            }

            return false;
        }

        public static void DisposeProcess(Process originalProcess)
        {
            if (!WineBridgeSettings.AdvancedProcessIntegration)
            {
                return;
            }

            if (TrackedProcessData.TryGetValue(originalProcess, out var processData))
            {
                if (processData.InputPipe != null)
                {
                    processData.InputPipe.Stop();

                    var fieldInfo =
                        typeof(Process).GetField("standardInput", BindingFlags.Instance | BindingFlags.NonPublic);
                    fieldInfo?.SetValue(originalProcess, null);
                }

                if (processData.OutputPipe != null)
                {
                    processData.OutputPipe.Stop();
                    var fieldInfo =
                        typeof(Process).GetField("standardOutput", BindingFlags.Instance | BindingFlags.NonPublic);
                    fieldInfo?.SetValue(originalProcess, null);
                }

                if (processData.ErrorPipe != null)
                {
                    processData.ErrorPipe.Stop();
                    var fieldInfo =
                        typeof(Process).GetField("standardError", BindingFlags.Instance | BindingFlags.NonPublic);
                    fieldInfo?.SetValue(originalProcess, null);
                }

                TrackedProcessData.Remove(originalProcess);
            }

            if (TrackedProcesses.TryGetValue(originalProcess, out var linuxProcess))
            {
                Logger.Debug($"Dispose called for process: " +
                             originalProcess.Id + " " +
                             originalProcess.StartInfo.FileName +
                             " " + originalProcess.StartInfo.Arguments);
                linuxProcess.ScriptProcess.Dispose();
                TrackedProcesses.Remove(originalProcess);
            }
        }

        public static Task TrackLinuxProcess(
            PlayController instance,
            LinuxProcess process,
            CancellationTokenSource watcherToken
        )
        {
            return Task.Run(async () => { await DoTrackLinuxProcess(instance, process, watcherToken); });
        }

        public static string TrackLinuxProcessGetResult(
            LinuxProcess process,
            CancellationTokenSource watcherToken = null
        )
        {
            Task.Run(async () => { await DoTrackLinuxProcess(process, watcherToken, 200, 200); }).Wait();

            var processStatus = GetProcessStatus(process.StatusTrackingFile);
            switch (processStatus)
            {
                case 0:
                    return GetProcessOutput(process.OutputTrackingFile);
                case 1:
                    return string.Empty;
                default:
                    throw new Exception(GetProcessOutput(process.ErrorTrackingFile));
            }
        }

        public static string[] TrackLinuxProcessGetResultInLines(
            LinuxProcess process,
            CancellationTokenSource watcherToken = null
        )
        {
            Task.Run(async () => { await DoTrackLinuxProcess(process, watcherToken, 200, 200); }).Wait();

            var processStatus = GetProcessStatus(process.StatusTrackingFile);
            switch (processStatus)
            {
                case 0:
                    return GetProcessOutputLines(process.OutputTrackingFile);
                case 1:
                    return new string[] { };
                default:
                    throw new Exception(GetProcessOutput(process.ErrorTrackingFile));
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

        private static async Task DoTrackLinuxProcess(PlayController instance, LinuxProcess process,
            CancellationTokenSource watcherToken)
        {
            Stopwatch stopwatch = null;
            try
            {
                var debugLogging = WineBridgeSettings.DebugLoggingEnabled;
                var processTrackingFile = process.ProcessTrackingFile;
                var readyTrackingFile = process.ReadyTrackingFile;

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

                stopwatch = InvokeStartEvent(instance, process);

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
            LinuxProcess process,
            CancellationTokenSource watcherToken,
            int millisecondsDelay = 2000,
            int readyIterations = 90)
        {
            try
            {
                var debugLogging = WineBridgeSettings.DebugLoggingEnabled;
                var processTrackingFile = process.ProcessTrackingFile;
                var readyTrackingFile = process.ReadyTrackingFile;

                var ready = false;
                for (var i = 0; i <= readyIterations; i++)
                {
                    if (watcherToken != null && watcherToken.IsCancellationRequested)
                    {
                        Logger.Info("Cancel signal received, stopping ready file monitoring...");
                        return;
                    }

                    if (File.Exists(process.ReadyTrackingFile))
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
            LinuxProcess process)
        {
            try
            {
                Logger.Debug("InvokeStartEvent");

                var stopwatch = Stopwatch.StartNew();

                AccessTools.Method(instance.GetType(), "InvokeOnStarted", new[] { typeof(GameStartedEventArgs) })
                    .Invoke(instance,
                        new object[]
                        {
                            new GameStartedEventArgs
                                { StartedProcessId = process?.OriginalProcess?.Id ?? process?.ScriptProcess?.Id ?? 0 }
                        });

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