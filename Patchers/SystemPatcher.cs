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
                var fileExistsMethod = typeof(File).GetMethod("Exists", BindingFlags.Static | BindingFlags.Public, null,
                    new[] { typeof(string) }, null);

                if (processStartMethod == null || fileExistsMethod == null)
                {
                    Logger.Warn("Failed to find system methods to patch!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var processStartPrefix = AccessTools.Method(typeof(ProcessPatches), "Prefix");
                HarmonyPatcher.HarmonyInstance.Patch(processStartMethod, prefix: new HarmonyMethod(processStartPrefix));

                var fileExistsPrefix = AccessTools.Method(typeof(FilePatches), "Prefix");
                HarmonyPatcher.HarmonyInstance.Patch(fileExistsMethod, prefix: new HarmonyMethod(fileExistsPrefix));

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
            if (path == Constants.DummySteamExe || path == Constants.DummyHeroicExe || path == Constants.DummyLutrisExe)
            {
                __result = true;
                return false;
            }

            return true;
        }
    }

    internal static class ProcessPatches
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool Prefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref Process __instance,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref bool __result
        )
        {
            var fileName = __instance.StartInfo.FileName;
            if (WineBridgeSettings.SteamIntegrationEnabled)
            {
                if (fileName.StartsWith("steam://"))
                {
                    var process = LinuxProcessStarter
                        .Start($"{WineBridgeSettings.SteamExecutablePathLinux} " + fileName + " " +
                               __instance.StartInfo.Arguments +
                               " & disown").Process;
                    __result = process != null;
                    return false;
                }

                if (fileName == Constants.DummySteamExe)
                {
                    var process = LinuxProcessStarter.Start($"{WineBridgeSettings.SteamExecutablePathLinux} " +
                                                            __instance.StartInfo.Arguments)
                        .Process;
                    __result = process != null;
                    return false;
                }
            }

            if (WineBridgeSettings.AnyHeroicIntegrationEnabled)
            {
                if (fileName == Constants.DummyHeroicExe)
                {
                    var process = LinuxProcessStarter.Start($"{WineBridgeSettings.HeroicExecutablePathLinux} " +
                                                            __instance.StartInfo.Arguments)
                        .Process;
                    __result = process != null;
                    return false;
                }
            }

            if (WineBridgeSettings.AnyLutrisIntegrationEnabled)
            {
                if (fileName == Constants.DummyLutrisExe)
                {
                    var process = LinuxProcessStarter.Start($"{WineBridgeSettings.LutrisExecutablePathLinux} " +
                                                            __instance.StartInfo.Arguments)
                        .Process;
                    __result = process != null;
                    return false;
                }
            }

            if (fileName.StartsWith(Constants.WineBridgePrefix))
            {
                var process = LinuxProcessStarter
                    .Start(fileName.Substring(Constants.WineBridgePrefix.Length))
                    .Process;
                __result = process != null;
                return false;
            }

            if (fileName.StartsWith(Constants.WineBridgeAsyncPrefix))
            {
                var process = LinuxProcessStarter
                    .Start(fileName.Substring(Constants.WineBridgeAsyncPrefix.Length), true,
                        __instance.StartInfo.Arguments).Process;
                __result = process != null;
                return false;
            }

            if (fileName.StartsWith(Constants.WineBridgeSteamPrefix))
            {
                var process = SteamProcessStarter
                    .Start(fileName.Substring(Constants.WineBridgeSteamPrefix.Length))
                    .Process;
                __result = process != null;
                return false;
            }

            if (fileName.StartsWith(Constants.WineBridgeHeroicPrefix))
            {
                var process = HeroicProcessStarter
                    .Start(fileName.Substring(Constants.WineBridgeHeroicPrefix.Length))
                    .Process;
                __result = process != null;
                return false;
            }

            if (fileName.StartsWith(Constants.WineBridgeLutrisPrefix))
            {
                var process = LutrisProcessStarter
                    .StartUsingId(Convert.ToInt64(fileName.Substring(Constants.WineBridgeLutrisPrefix.Length)))
                    .Process;
                __result = process != null;
                return false;
            }

            if (WineBridgeSettings.RedirectExplorerCallsToLinux &&
                (fileName == "explorer.exe" || fileName.EndsWith(@"\explorer.exe")))
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

                        process = LinuxProcessStarter.Start($"xdg-open \"{fullPath}\"").Process;
                    }
                    else
                    {
                        process = LinuxProcessStarter.Start("xdg-open .").Process;
                    }

                    __result = process != null;
                    return false;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error occurred while redirecting explorer call!");
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

                    var process = LinuxProcessStarter.Start($"xdg-open \"{newFileName}\"")
                        .Process;
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
    }
}