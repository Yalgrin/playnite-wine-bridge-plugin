using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Playnite.SDK;
using WineBridgePlugin.Integrations.Heroic;
using WineBridgePlugin.Integrations.Lutris;
using WineBridgePlugin.Integrations.Steam;
using WineBridgePlugin.Models;
using WineBridgePlugin.Processes;
using WineBridgePlugin.Settings;
using WineBridgePlugin.Utils;

namespace WineBridgePlugin.Patchers
{
    public static class SystemPatcher
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        public static PatchingState State { get; private set; } = PatchingState.Unpatched;

        internal static void Patch()
        {
            if (State == PatchingState.Patched)
            {
                return;
            }

            try
            {
                var processStartMethod = typeof(Process).GetMethod("Start", BindingFlags.Instance | BindingFlags.Public,
                    null,
                    Type.EmptyTypes, null);
                var processDisposeMethod = typeof(Process).GetMethod("Dispose",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(bool) }, null);
                var processGetIdMethod = typeof(Process).GetProperty("Id")?.GetGetMethod();
                var processWaitForExitMethod = typeof(Process).GetMethod("WaitForExit",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    new[] { typeof(int) }, null);
                var processGetExitCodeMethod = typeof(Process).GetProperty("ExitCode")?.GetGetMethod();
                var processHasExitedMethod = typeof(Process).GetProperty("HasExited")?.GetGetMethod();
                var processKillMethod = typeof(Process).GetMethod("Kill");
                var fileExistsMethod = typeof(File).GetMethod("Exists", BindingFlags.Static | BindingFlags.Public, null,
                    new[] { typeof(string) }, null);
                var fileInfoConstructors = AccessTools.GetDeclaredConstructors(typeof(FileInfo));

                if (processStartMethod == null || fileExistsMethod == null || (fileInfoConstructors?.Count ?? 0) == 0
                    || processGetIdMethod == null || processWaitForExitMethod == null || processDisposeMethod == null
                    || processKillMethod == null)
                {
                    Logger.Warn("Failed to find system methods to patch!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var processStartPrefix = AccessTools.Method(typeof(ProcessPatches), "StartPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(processStartMethod, prefix: new HarmonyMethod(processStartPrefix));
                var processDisposePrefix = AccessTools.Method(typeof(ProcessPatches), "DisposePrefix");
                HarmonyPatcher.HarmonyInstance.Patch(processDisposeMethod,
                    prefix: new HarmonyMethod(processDisposePrefix));
                var processGetIdPrefix = AccessTools.Method(typeof(ProcessPatches), "GetIdPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(processGetIdMethod, prefix: new HarmonyMethod(processGetIdPrefix));
                var processWaitForExitPrefix = AccessTools.Method(typeof(ProcessPatches), "WaitForExitPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(processWaitForExitMethod,
                    prefix: new HarmonyMethod(processWaitForExitPrefix));
                var processGetExitCodePrefix = AccessTools.Method(typeof(ProcessPatches), "GetExitCodePrefix");
                HarmonyPatcher.HarmonyInstance.Patch(processGetExitCodeMethod,
                    prefix: new HarmonyMethod(processGetExitCodePrefix));
                var processHasExitedPrefix = AccessTools.Method(typeof(ProcessPatches), "HasExitedPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(processHasExitedMethod,
                    prefix: new HarmonyMethod(processHasExitedPrefix));
                var processKillPrefix = AccessTools.Method(typeof(ProcessPatches), "KillPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(processKillMethod,
                    prefix: new HarmonyMethod(processKillPrefix));

                var fileExistsPrefix = AccessTools.Method(typeof(FilePatches), "Prefix");
                HarmonyPatcher.HarmonyInstance.Patch(fileExistsMethod, prefix: new HarmonyMethod(fileExistsPrefix));

                var fileInfoConstructorPrefix = AccessTools.Method(typeof(FileInfoPatches), "ConstructorPrefix");
                var fileInfoConstructor = fileInfoConstructors.FirstOrDefault(c =>
                {
                    var parameters = c.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType == typeof(string);
                });
                if (fileInfoConstructor != null)
                {
                    HarmonyPatcher.HarmonyInstance.Patch(fileInfoConstructor,
                        prefix: new HarmonyMethod(fileInfoConstructorPrefix));
                }

                Logger.Info("System methods patched successfully!");
                State = PatchingState.Patched;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while patching system methods!");
                State = PatchingState.Error;
            }
        }
    }

    internal static class FilePatches
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool Prefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref bool __result, string path)
        {
            if (path == Constants.DummySteamExe || path == Constants.DummyHeroicExe ||
                path == Constants.DummyLutrisExe || path == Constants.DummyItchIoExe ||
                path == Constants.DummyItchButlerExe)
            {
                __result = true;
                return false;
            }

            return true;
        }
    }

    internal static class FileInfoPatches
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool ConstructorPrefix(ref string fileName)
        {
            if (fileName != null && fileName.StartsWith(Constants.WineBridgePrefix))
            {
                fileName = Directory.GetCurrentDirectory();
            }

            return true;
        }
    }

    internal static class ProcessPatches
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool StartPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref Process __instance,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref bool __result
        )
        {
            if (WineBridgeSettings.DebugLoggingEnabled)
            {
                Logger.Debug(
                    $"Starting process: \"{__instance.StartInfo.FileName}\" with arguments \"{__instance.StartInfo.Arguments}\"");
            }

            var fileName = __instance.StartInfo.FileName;
            if (WineBridgeSettings.SteamIntegrationEnabled)
            {
                if (fileName.StartsWith("steam://"))
                {
                    var process = LinuxProcessStarter
                        .Start(__instance, $"{WineBridgeSettings.SteamExecutablePathLinux} " + fileName + " " +
                                           __instance.StartInfo.Arguments +
                                           " & disown").ScriptProcess;
                    __result = process != null;
                    return false;
                }

                if (fileName == Constants.DummySteamExe)
                {
                    var process = LinuxProcessStarter.Start(__instance,
                            $"{WineBridgeSettings.SteamExecutablePathLinux} " +
                            __instance.StartInfo.Arguments)
                        .ScriptProcess;
                    __result = process != null;
                    return false;
                }
            }

            if (WineBridgeSettings.AnyHeroicIntegrationEnabled)
            {
                if (fileName == Constants.DummyHeroicExe)
                {
                    var process = LinuxProcessStarter.Start(__instance,
                            $"{WineBridgeSettings.HeroicExecutablePathLinux} " +
                            __instance.StartInfo.Arguments)
                        .ScriptProcess;
                    __result = process != null;
                    return false;
                }
            }

            if (WineBridgeSettings.AnyLutrisIntegrationEnabled)
            {
                if (fileName == Constants.DummyLutrisExe)
                {
                    var process = LinuxProcessStarter.Start(__instance,
                            $"{WineBridgeSettings.LutrisExecutablePathLinux} " +
                            __instance.StartInfo.Arguments)
                        .ScriptProcess;
                    __result = process != null;
                    return false;
                }
            }

            if (WineBridgeSettings.AnyLutrisIntegrationEnabled)
            {
                if (fileName == Constants.DummyItchIoExe)
                {
                    var process = LinuxProcessStarter.Start(__instance,
                            $"XDG_SESSION_TYPE=wayland {WineBridgeSettings.ItchIoExecutablePathLinux} " +
                            __instance.StartInfo.Arguments)
                        .ScriptProcess;
                    __result = process != null;
                    return false;
                }
            }

            if (WineBridgeSettings.AnyLutrisIntegrationEnabled)
            {
                if (fileName == Constants.DummyItchButlerExe)
                {
                    var installationPath = WineBridgeSettings.ItchIoDataPathLinux;
                    if (installationPath != null)
                    {
                        var windowsPath = WineUtils.LinuxPathToWindows(installationPath);
                        if (!Directory.Exists(windowsPath))
                        {
                            return true;
                        }

                        var corePath = Path.Combine(windowsPath, "broth", "butler");
                        var versionPath = Path.Combine(corePath, ".chosen-version");
                        if (!File.Exists(versionPath))
                        {
                            return true;
                        }

                        var currentVer = File.ReadAllText(versionPath);
                        var exePath = Path.Combine(corePath, "versions", currentVer, "butler");
                        var butlerExePath = WineUtils.WindowsPathToLinux(exePath);
                        var process = LinuxProcessStarter.Start(__instance,
                                $"{butlerExePath} " +
                                __instance.StartInfo.Arguments)
                            .ScriptProcess;
                        __result = process != null;
                        return false;
                    }
                }
            }

            if (fileName.StartsWith(Constants.WineBridgePrefix))
            {
                var linuxProcess = LinuxProcessStarter
                    .Start(__instance,
                        $"{fileName.Substring(Constants.WineBridgePrefix.Length)} {__instance.StartInfo.Arguments}");
                __result = linuxProcess.ScriptProcess != null;
                return false;
            }

            if (fileName.StartsWith(Constants.WineBridgeAsyncPrefix))
            {
                var process = LinuxProcessStarter
                    .Start(__instance, fileName.Substring(Constants.WineBridgeAsyncPrefix.Length), true,
                        __instance.StartInfo.Arguments).ScriptProcess;
                __result = process != null;
                return false;
            }

            if (fileName.StartsWith(Constants.WineBridgeSteamPrefix))
            {
                var process = SteamProcessStarter
                    .Start(fileName.Substring(Constants.WineBridgeSteamPrefix.Length))
                    .ScriptProcess;
                __result = process != null;
                return false;
            }

            if (fileName.StartsWith(Constants.WineBridgeHeroicPrefix))
            {
                var process = HeroicProcessStarter
                    .Start(fileName.Substring(Constants.WineBridgeHeroicPrefix.Length))
                    .ScriptProcess;
                __result = process != null;
                return false;
            }

            if (fileName.StartsWith(Constants.WineBridgeLutrisPrefix))
            {
                var process = LutrisProcessStarter
                    .StartUsingId(Convert.ToInt64(fileName.Substring(Constants.WineBridgeLutrisPrefix.Length)))
                    .ScriptProcess;
                __result = process != null;
                return false;
            }

            if (fileName.StartsWith(Constants.ItchPrefix) && WineBridgeSettings.ItchIoIntegrationEnabled)
            {
                var process = LinuxProcessStarter.Start(__instance,
                        $"xdg-open \"{__instance.StartInfo.FileName} {__instance.StartInfo.Arguments}\"")
                    .ScriptProcess;
                __result = process != null;
                return false;
            }

            if (WineBridgeSettings.RedirectExplorerCallsToLinux)
            {
                if (fileName == "explorer.exe" || fileName.EndsWith(@"\explorer.exe"))
                {
                    try
                    {
                        var args = __instance.StartInfo.Arguments.Trim();
                        if (args.StartsWith("shell:"))
                        {
                            return true;
                        }

                        var parentFolder = false;
                        if (args.StartsWith("/select,"))
                        {
                            args = args.Substring("/select,".Length);
                            parentFolder = true;
                        }

                        Process process;
                        if (!string.IsNullOrEmpty(args))
                        {
                            var fullPath = CleanupAndTransformToLinuxFilePath(args, parentFolder);

                            process = LinuxProcessStarter.Start(__instance, $"xdg-open \"{fullPath}\"").ScriptProcess;
                        }
                        else
                        {
                            process = LinuxProcessStarter.Start(__instance, "xdg-open .").ScriptProcess;
                        }

                        __result = process != null;
                        return false;
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "Error occurred while redirecting explorer call!");
                    }
                }
                else if (Directory.Exists(fileName))
                {
                    var fullPath = CleanupAndTransformToLinuxFilePath(fileName);

                    var process = LinuxProcessStarter.Start(__instance, $"xdg-open \"{fullPath}\"").ScriptProcess;
                    __result = process != null;
                    return false;
                }
            }

            if (WineBridgeSettings.RedirectProtocolCallsToLinux &&
                Constants.RedirectedProtocols.Any(protocol => fileName.StartsWith(protocol)))
            {
                try
                {
                    var newFileName = fileName;
                    if (fileName.StartsWith(Constants.FilePrefix))
                    {
                        newFileName = CleanupAndTransformToLinuxFilePath(newFileName.Replace(Constants.FilePrefix, ""));
                    }
                    else
                    {
                        newFileName = fileName;
                    }

                    var process = LinuxProcessStarter.Start(__instance, $"xdg-open \"{newFileName}\"")
                        .ScriptProcess;
                    __result = process != null;
                    return false;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error occurred while redirecting protocol call!");
                }
            }

            return true;
        }

        private static string CleanupAndTransformToLinuxFilePath(string startingPath, bool parentFolder = false)
        {
            if (startingPath.StartsWith("\"") && startingPath.EndsWith("\""))
            {
                startingPath = startingPath.Substring(1, startingPath.Length - 2);
            }

            if (startingPath.StartsWith("/"))
            {
                startingPath = startingPath.Substring(1);
            }

            var fullPath = Path.GetFullPath(startingPath);
            if (parentFolder)
            {
                fullPath = Path.GetDirectoryName(fullPath);
            }

            return WineUtils.WindowsPathToLinux(fullPath);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool DisposePrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref Process __instance,
            bool disposing
        )
        {
            try
            {
                if (disposing)
                {
                    LinuxProcessMonitor.DisposeProcess(__instance);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while disposing process!");
            }

            return true;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool GetIdPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref Process __instance,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref int __result
        )
        {
            if (LinuxProcessMonitor.GetProcessId(__instance, out var result))
            {
                __result = result;
                return false;
            }

            return true;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool GetExitCodePrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref Process __instance,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref int __result
        )
        {
            if (LinuxProcessMonitor.GetExitCode(__instance, out var status))
            {
                __result = status;
                return false;
            }

            return true;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool HasExitedPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref Process __instance,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref bool __result
        )
        {
            if (LinuxProcessMonitor.HasExited(__instance, out var status))
            {
                __result = status;
                return false;
            }

            return true;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool WaitForExitPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref Process __instance,
            int milliseconds,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref bool __result
        )
        {
            if (LinuxProcessMonitor.WaitForExit(__instance, milliseconds, out var result))
            {
                __result = result;
                return false;
            }

            return true;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool KillPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref Process __instance
        )
        {
            if (LinuxProcessMonitor.Kill(__instance))
            {
                return false;
            }

            return true;
        }
    }
}