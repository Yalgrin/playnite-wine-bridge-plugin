using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Playnite.SDK;
using WineBridgePlugin.Models;
using WineBridgePlugin.Processes;

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
            if (path == Constants.DummySteamExe)
            {
                __result = true;
                return false;
            }

            return true;
        }
    }

    internal static class ProcessPatches
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool Prefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref Process __instance,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref bool __result
        )
        {
            var fileName = __instance.StartInfo.FileName;
            if (WineBridgePlugin.Settings?.SteamIntegrationEnabled ?? false)
            {
                if (fileName.StartsWith("steam://"))
                {
                    var process = LinuxProcessStarter
                        .Start($"{GetSteamExecutable()} " + fileName + " " + __instance.StartInfo.Arguments +
                               " & disown").Process;
                    __result = process != null;
                    return false;
                }

                if (fileName == Constants.DummySteamExe)
                {
                    var process = LinuxProcessStarter.Start($"{GetSteamExecutable()} " + __instance.StartInfo.Arguments)
                        .Process;
                    __result = process != null;
                    return false;
                }
            }

            if (fileName.StartsWith("wine-bridge://"))
            {
                var process = LinuxProcessStarter
                    .Start(fileName.Substring("wine-bridge://".Length))
                    .Process;
                __result = process != null;
                return false;
            }

            if (fileName.StartsWith("wine-bridge-async://"))
            {
                var process = LinuxProcessStarter
                    .Start(fileName.Substring("wine-bridge-async://".Length), true,
                        __instance.StartInfo.Arguments).Process;
                __result = process != null;
                return false;
            }

            return true;
        }

        private static string GetSteamExecutable()
        {
            return WineBridgePlugin.Settings?.SteamExecutablePathLinux ?? "steam";
        }
    }
}