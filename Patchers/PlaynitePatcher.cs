using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using HarmonyLib;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using WineBridgePlugin.Integrations.Heroic;
using WineBridgePlugin.Integrations.Lutris;
using WineBridgePlugin.Integrations.Steam;
using WineBridgePlugin.Models;
using WineBridgePlugin.Processes;
using WineBridgePlugin.Settings;
using WineBridgePlugin.Utils;

namespace WineBridgePlugin.Patchers
{
    public static class PlaynitePatcher
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
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Playnite");

                if (assembly == null)
                {
                    Logger.Warn("Failed to find Playnite assembly!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var genericPlayControllerType = assembly.GetType("Playnite.Controllers.GenericPlayController");
                var gamesEditorType = assembly.GetType("Playnite.GamesEditor");
                var emulationType = assembly.GetType("Playnite.Emulators.Emulation");
                var bitmapExtensionsType = assembly.GetType("System.Drawing.Imaging.BitmapExtensions");
                if (genericPlayControllerType == null || gamesEditorType == null || emulationType == null
                    || bitmapExtensionsType == null)
                {
                    Logger.Warn("Failed to find Playnite classes!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var startMethod = genericPlayControllerType.GetMethod("Start",
                    BindingFlags.Instance | BindingFlags.Public, null,
                    new[] { typeof(GameAction), typeof(bool), typeof(OnGameStartingEventArgs) }, null);
                var disposeMethod = genericPlayControllerType.GetMethod("Dispose");
                var powershellErrorField = AccessTools.Field(gamesEditorType, "showedPowerShellError");
                var startEmulatorMethod = genericPlayControllerType.GetMethod("StartEmulatorProcess",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var getProfileMethod = emulationType.GetMethod("GetProfile",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                var getExecutableMethod = emulationType.GetMethod("GetExecutable",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                var bitmapFromStreamMethod = bitmapExtensionsType.GetMethod("BitmapFromStream",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (startMethod == null || disposeMethod == null || powershellErrorField == null ||
                    startEmulatorMethod == null || getProfileMethod == null || getExecutableMethod == null ||
                    bitmapFromStreamMethod == null)
                {
                    Logger.Warn("Failed to find Playnite methods!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var startControllerPlayPrefix = AccessTools.Method(typeof(GenericPlayGamePatcher), "StartPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(startMethod, prefix: new HarmonyMethod(startControllerPlayPrefix));
                var startControllerDisposePrefix = AccessTools.Method(typeof(GenericPlayGamePatcher), "DisposePrefix");
                HarmonyPatcher.HarmonyInstance.Patch(disposeMethod,
                    prefix: new HarmonyMethod(startControllerDisposePrefix));
                var startEmulatorProcessControllerPlayPrefix =
                    AccessTools.Method(typeof(GenericPlayGamePatcher), "StartEmulatorProcessPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(startEmulatorMethod,
                    prefix: new HarmonyMethod(startEmulatorProcessControllerPlayPrefix));
                var getProfilePostfix = AccessTools.Method(typeof(EmulationPatches), "GetProfilePostfix");
                HarmonyPatcher.HarmonyInstance.Patch(getProfileMethod,
                    postfix: new HarmonyMethod(getProfilePostfix));
                var getExecutablePrefix = AccessTools.Method(typeof(EmulationPatches), "GetExecutablePrefix");
                HarmonyPatcher.HarmonyInstance.Patch(getExecutableMethod,
                    prefix: new HarmonyMethod(getExecutablePrefix));
                var bitmapFromStreamPrefix =
                    AccessTools.Method(typeof(BitmapExtensionsPatches), "BitmapFromStreamPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(bitmapFromStreamMethod,
                    prefix: new HarmonyMethod(bitmapFromStreamPrefix));
                powershellErrorField.SetValue(null, true);

                Logger.Info("Playnite methods patched successfully!");
                State = PatchingState.Patched;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while patching Playnite methods!");
                State = PatchingState.Error;
            }
        }
    }

    public static class GenericPlayGamePatcher
    {
        private static readonly Dictionary<PlayController, CancellationTokenSource> PlayCancelationTokenSources =
            new Dictionary<PlayController, CancellationTokenSource>();

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool DisposePrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            PlayController __instance)
        {
            if (PlayCancelationTokenSources.ContainsKey(__instance))
            {
                var token = PlayCancelationTokenSources[__instance];
                token?.Cancel();
                token?.Dispose();
                PlayCancelationTokenSources.Remove(__instance);
            }

            return true;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private static bool StartPrefix([SuppressMessage("ReSharper", "InconsistentNaming")] PlayController __instance,
            GameAction playAction, bool asyncExec,
            OnGameStartingEventArgs startingArgs)
        {
            if (playAction.Type != GameActionType.File)
            {
                return true;
            }

            if (playAction.Path.StartsWith(Constants.WineBridgePrefix))
            {
                AccessTools.Property(__instance.GetType(), "StartingArgs").SetValue(__instance, startingArgs);
                var watcherToken = new CancellationTokenSource();
                PlayCancelationTokenSources[__instance] = watcherToken;
                var process =
                    LinuxProcessStarter.Start(playAction.Path.Substring(Constants.WineBridgePrefix.Length));
                LinuxProcessMonitor.TrackLinuxProcess(__instance, process, watcherToken);
                return false;
            }

            if (playAction.Path.StartsWith(Constants.WineBridgeAsyncPrefix))
            {
                AccessTools.Property(__instance.GetType(), "StartingArgs").SetValue(__instance, startingArgs);
                var watcherToken = new CancellationTokenSource();
                PlayCancelationTokenSources[__instance] = watcherToken;
                var process = LinuxProcessStarter.Start(
                    playAction.Path.Substring(Constants.WineBridgeAsyncPrefix.Length),
                    true, playAction.Arguments);
                LinuxProcessMonitor.TrackLinuxProcess(__instance, process, watcherToken);
                return false;
            }

            if (playAction.Path.StartsWith(Constants.WineBridgeSteamPrefix))
            {
                AccessTools.Property(__instance.GetType(), "StartingArgs").SetValue(__instance, startingArgs);
                var watcherToken = new CancellationTokenSource();
                PlayCancelationTokenSources[__instance] = watcherToken;
                var process = SteamProcessStarter.Start(
                    playAction.Path.Substring(Constants.WineBridgeSteamPrefix.Length), playAction.Arguments);
                LinuxProcessMonitor.TrackLinuxProcess(__instance, process, watcherToken);
                return false;
            }

            if (playAction.Path.StartsWith(Constants.WineBridgeHeroicPrefix))
            {
                AccessTools.Property(__instance.GetType(), "StartingArgs").SetValue(__instance, startingArgs);
                var watcherToken = new CancellationTokenSource();
                PlayCancelationTokenSources[__instance] = watcherToken;
                var process = HeroicProcessStarter.Start(
                    playAction.Path.Substring(Constants.WineBridgeHeroicPrefix.Length));
                LinuxProcessMonitor.TrackLinuxProcess(__instance, process, watcherToken);
                return false;
            }

            if (playAction.Path.StartsWith(Constants.WineBridgeLutrisPrefix))
            {
                AccessTools.Property(__instance.GetType(), "StartingArgs").SetValue(__instance, startingArgs);
                var watcherToken = new CancellationTokenSource();
                PlayCancelationTokenSources[__instance] = watcherToken;
                var process = LutrisProcessStarter.StartUsingId(
                    Convert.ToInt64(playAction.Path.Substring(Constants.WineBridgeLutrisPrefix.Length)));
                LinuxProcessMonitor.TrackLinuxProcess(__instance, process, watcherToken);
                return false;
            }

            return true;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private static bool StartEmulatorProcessPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            PlayController __instance,
            string path,
            string args,
            string workDir,
            string emulatorDir,
            string romPath,
            bool asyncExec,
            Emulator emulator,
            EmulatorProfile emuProfile,
            TrackingMode trackingMode,
            string trackingPath)
        {
            if (path.StartsWith(Constants.WineBridgePrefix))
            {
                var watcherToken = new CancellationTokenSource();
                PlayCancelationTokenSources[__instance] = watcherToken;
                var process = LinuxProcessStarter.Start(
                    $"{path.Replace(Constants.WineBridgePrefix, "")} {args.Replace(romPath, WineUtils.WindowsPathToLinux(romPath))}");
                LinuxProcessMonitor.TrackLinuxProcess(__instance, process, watcherToken);

                return false;
            }

            return true;
        }
    }

    public static class EmulationPatches
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static void GetProfilePostfix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref EmulatorDefinitionProfile __result, string emulatorId,
            string profileName)
        {
            var emulatorConfigs = WineBridgeSettings.EmulatorConfigs;
            var matchingConfig = emulatorConfigs.FirstOrDefault(c => c.EmulatorId == emulatorId);
            if (matchingConfig == null)
            {
                return;
            }

            Logger.Debug($"Replacing emulator profile for emulator {emulatorId} with {profileName}");
            var currentResult = __result;

            __result = new EmulatorDefinitionProfile
            {
                Name = currentResult.Name,
                Platforms = currentResult.Platforms,
                ImageExtensions = currentResult.ImageExtensions,
                ProfileFiles = currentResult.ProfileFiles,
                InstallationFile = $"{Constants.WineBridgePrefix}{matchingConfig.LinuxPath}",
                StartupArguments = GetStartupArguments(currentResult, emulatorId),
                StartupExecutable = $"{Constants.WineBridgePrefix}{matchingConfig.LinuxPath}",
                ScriptStartup = false,
                ScriptGameImport = false
            };
        }

        private static string GetStartupArguments(EmulatorDefinitionProfile oldResult, string emulatorId)
        {
            if (emulatorId != "retroarch")
            {
                return oldResult.StartupArguments;
            }

            return Regex.Replace(oldResult.StartupArguments, "-L \"\\.\\\\cores\\\\(.+)_libretro\\.dll\"",
                "-L \"$1\"");
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private static bool GetExecutablePrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref string __result, string directory,
            EmulatorDefinitionProfile profile, bool relative)
        {
            if (profile.StartupExecutable.StartsWith(Constants.WineBridgePrefix))
            {
                Logger.Debug($"Replacing executable prefix for profile {profile.Name} to {profile.InstallationFile}");
                __result = profile.StartupExecutable;
                return false;
            }

            return true;
        }
    }

    public static class BitmapExtensionsPatches
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool BitmapFromStreamPrefix(
            ref Stream stream)
        {
            if (!WineBridgeSettings.ForceHighQualityIcons)
            {
                return true;
            }

            try
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream = IconExtractor.StripIconToTheHighestQualityFrame(stream);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to strip icon from stream!");
            }

            return true;
        }
    }
}