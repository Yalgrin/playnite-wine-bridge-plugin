using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using Playnite;
using WineBridgePlugin.Integrations.Heroic;
using WineBridgePlugin.Integrations.Lutris;
using WineBridgePlugin.Integrations.Steam;
using WineBridgePlugin.Models;
using WineBridgePlugin.Processes;

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

                // var playniteApplicationType = assembly.GetType("Playnite.PlayniteApplication");
                var genericFilePlayControllerType = assembly.GetType("Playnite.GenericFilePlayController");
                var genericPlayControllerType = assembly.GetType("Playnite.GenericPlayController");
                // var gamesEditorType = assembly.GetType("Playnite.GamesEditor");
                // var emulationType = assembly.GetType("Playnite.Emulators.Emulation");
                // var bitmapExtensionsType = assembly.GetType("System.Drawing.Imaging.BitmapExtensions");
                // var systemDialogsType = assembly.GetType("Playnite.Common.SystemDialogs");
                if (genericPlayControllerType == null || genericFilePlayControllerType == null)
                {
                    Logger.Warn("Failed to find Playnite classes!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                // var crashMethod = playniteApplicationType.GetMethod("CurrentDomain_UnhandledException",
                //     BindingFlags.Instance | BindingFlags.NonPublic);
                var startMethod = genericFilePlayControllerType.GetMethod("PlayAsync",
                    BindingFlags.Instance | BindingFlags.Public, null,
                    new[] { typeof(PlayController.PlayActionArgs) }, null);
                var disposeMethod = genericPlayControllerType.GetMethod("DisposeAsync");
                // var powershellErrorField = AccessTools.Field(gamesEditorType, "showedPowerShellError");
                // var startEmulatorMethod = genericPlayControllerType.GetMethod("StartEmulatorProcess",
                //     BindingFlags.Instance | BindingFlags.NonPublic);
                // var getProfileMethod = emulationType.GetMethod("GetProfile",
                //     BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                // var getExecutableMethod = emulationType.GetMethod("GetExecutable",
                //     BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                // var bitmapFromStreamMethod = bitmapExtensionsType.GetMethod("BitmapFromStream",
                //     BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                // var saveFileMethod = systemDialogsType.GetMethod("SaveFile",
                //     BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                //     new[] { typeof(Window), typeof(string), typeof(bool), typeof(string) }, null);
                // var selectFolderMethod = systemDialogsType.GetMethod("SelectFolder",
                //     BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                //     new[] { typeof(Window), typeof(string) }, null);
                // var selectFileMethod = systemDialogsType.GetMethod("SelectFile",
                //     BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                //     new[] { typeof(Window), typeof(string), typeof(string) }, null);
                // var selectFilesMethod = systemDialogsType.GetMethod("SelectFiles",
                //     BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                //     new[] { typeof(Window), typeof(string), typeof(string) }, null);
                if (startMethod == null || disposeMethod == null)
                {
                    Logger.Warn("Failed to find Playnite methods!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                //
                // var crashPrefix = AccessTools.Method(typeof(ApplicationPatcher), "CrashPrefix");
                // HarmonyPatcher.HarmonyInstance.Patch(crashMethod, prefix: new HarmonyMethod(crashPrefix));
                //
                //
                var startControllerPlayPrefix = AccessTools.Method(typeof(GenericPlayGamePatcher), "PlayAsyncPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(startMethod, prefix: new HarmonyMethod(startControllerPlayPrefix));
                var startControllerDisposePostfix =
                    AccessTools.Method(typeof(GenericPlayGamePatcher), "DisposeAsyncPostfix");
                HarmonyPatcher.HarmonyInstance.Patch(disposeMethod,
                    postfix: new HarmonyMethod(startControllerDisposePostfix));
                // var startEmulatorProcessControllerPlayPrefix =
                //     AccessTools.Method(typeof(GenericPlayGamePatcher), "StartEmulatorProcessPrefix");
                // HarmonyPatcher.HarmonyInstance.Patch(startEmulatorMethod,
                //     prefix: new HarmonyMethod(startEmulatorProcessControllerPlayPrefix));
                // var getProfilePostfix = AccessTools.Method(typeof(EmulationPatches), "GetProfilePostfix");
                // HarmonyPatcher.HarmonyInstance.Patch(getProfileMethod,
                //     postfix: new HarmonyMethod(getProfilePostfix));
                // var getExecutablePrefix = AccessTools.Method(typeof(EmulationPatches), "GetExecutablePrefix");
                // HarmonyPatcher.HarmonyInstance.Patch(getExecutableMethod,
                //     prefix: new HarmonyMethod(getExecutablePrefix));
                // var bitmapFromStreamPrefix =
                //     AccessTools.Method(typeof(BitmapExtensionsPatches), "BitmapFromStreamPrefix");
                // HarmonyPatcher.HarmonyInstance.Patch(bitmapFromStreamMethod,
                //     prefix: new HarmonyMethod(bitmapFromStreamPrefix));
                //
                // var saveFilePrefix = AccessTools.Method(typeof(SystemDialogsPatches), "SaveFilePrefix");
                // HarmonyPatcher.HarmonyInstance.Patch(saveFileMethod, prefix: new HarmonyMethod(saveFilePrefix));
                // var selectFolderPrefix = AccessTools.Method(typeof(SystemDialogsPatches), "SelectFolderPrefix");
                // HarmonyPatcher.HarmonyInstance.Patch(selectFolderMethod, prefix: new HarmonyMethod(selectFolderPrefix));
                // var selectFilesPrefix = AccessTools.Method(typeof(SystemDialogsPatches), "SelectFilesPrefix");
                // HarmonyPatcher.HarmonyInstance.Patch(selectFilesMethod, prefix: new HarmonyMethod(selectFilesPrefix));
                // var selectFilePrefix = AccessTools.Method(typeof(SystemDialogsPatches), "SelectFilePrefix");
                // HarmonyPatcher.HarmonyInstance.Patch(selectFileMethod, prefix: new HarmonyMethod(selectFilePrefix));
                //
                // powershellErrorField.SetValue(null, true);

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

    // public static class ApplicationPatcher
    // {
    //     private static readonly ILogger Logger = LogManager.GetLogger();
    //
    //     [SuppressMessage("ReSharper", "UnusedMember.Local")]
    //     public static bool CrashPrefix(
    //         [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    //         object sender, UnhandledExceptionEventArgs e)
    //     {
    //         Logger.Error(e.ExceptionObject as Exception, "Playnite crashed!");
    //         return true;
    //     }
    // }
    //
    public static class GenericPlayGamePatcher
    {
        private static readonly Dictionary<PlayController, CancellationTokenSource> PlayCancelationTokenSources =
            new Dictionary<PlayController, CancellationTokenSource>();

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static void DisposeAsyncPostfix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            PlayController __instance)
        {
            if (PlayCancelationTokenSources.TryGetValue(__instance, out var token))
            {
                LocalDisposeAsync(__instance, token);
            }
        }

        private static void LocalDisposeAsync(PlayController __instance, CancellationTokenSource token)
        {
            token?.Cancel();
            token?.Dispose();
            PlayCancelationTokenSources.Remove(__instance);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private static bool PlayAsyncPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            PlayController __instance,
            PlayController.PlayActionArgs args,
            ref Task __result)
        {
            FileGameAction? playAction =
                AccessTools.Field(__instance.GetType(), "Action")?.GetValue(__instance) as FileGameAction;
            var playActionPath = playAction?.Path;
            if (playAction == null || playActionPath == null)
            {
                return true;
            }

            if (playActionPath.StartsWith(Constants.WineBridgePrefix))
            {
                var watcherToken = new CancellationTokenSource();
                PlayCancelationTokenSources[__instance] = watcherToken;
                var process =
                    LinuxProcessStarter.Start(playActionPath.Substring(Constants.WineBridgePrefix.Length));
                __result = LinuxProcessMonitor.TrackLinuxProcess(__instance, process, watcherToken);
                return false;
            }

            if (playActionPath.StartsWith(Constants.WineBridgeAsyncPrefix) && playAction.Arguments != null)
            {
                var watcherToken = new CancellationTokenSource();
                PlayCancelationTokenSources[__instance] = watcherToken;
                var process = LinuxProcessStarter.Start(
                    playActionPath.Substring(Constants.WineBridgeAsyncPrefix.Length),
                    true, playAction.Arguments);
                __result = LinuxProcessMonitor.TrackLinuxProcess(__instance, process, watcherToken);
                return false;
            }

            if (playActionPath.StartsWith(Constants.WineBridgeSteamPrefix) && playAction.Arguments != null)
            {
                var watcherToken = new CancellationTokenSource();
                PlayCancelationTokenSources[__instance] = watcherToken;
                var process = SteamProcessStarter.Start(
                    playActionPath.Substring(Constants.WineBridgeSteamPrefix.Length), playAction.Arguments);
                __result = LinuxProcessMonitor.TrackLinuxProcess(__instance, process, watcherToken);
                return false;
            }

            if (playActionPath.StartsWith(Constants.WineBridgeHeroicPrefix))
            {
                var watcherToken = new CancellationTokenSource();
                PlayCancelationTokenSources[__instance] = watcherToken;
                var process = HeroicProcessStarter.Start(
                    playActionPath.Substring(Constants.WineBridgeHeroicPrefix.Length));
                __result = LinuxProcessMonitor.TrackLinuxProcess(__instance, process, watcherToken);
                return false;
            }

            if (playActionPath.StartsWith(Constants.WineBridgeLutrisPrefix))
            {
                var watcherToken = new CancellationTokenSource();
                PlayCancelationTokenSources[__instance] = watcherToken;
                var process = LutrisProcessStarter.StartUsingId(
                    Convert.ToInt64(playActionPath.Substring(Constants.WineBridgeLutrisPrefix.Length)));
                __result = LinuxProcessMonitor.TrackLinuxProcess(__instance, process, watcherToken);
                return false;
            }

            return true;
        }
    }
    //
    // public static class EmulationPatches
    // {
    //     private static readonly ILogger Logger = LogManager.GetLogger();
    //
    //     [SuppressMessage("ReSharper", "UnusedMember.Local")]
    //     private static void GetProfilePostfix(
    //         [SuppressMessage("ReSharper", "InconsistentNaming")]
    //         ref EmulatorDefinitionProfile __result, string emulatorId,
    //         string profileName)
    //     {
    //         var emulatorConfigs = WineBridgeSettings.EmulatorConfigs;
    //         var matchingConfig = emulatorConfigs.FirstOrDefault(c => c.EmulatorId == emulatorId);
    //         if (matchingConfig == null)
    //         {
    //             return;
    //         }
    //
    //         Logger.Debug($"Replacing emulator profile for emulator {emulatorId} with {profileName}");
    //         var currentResult = __result;
    //
    //         __result = new EmulatorDefinitionProfile
    //         {
    //             Name = currentResult.Name,
    //             Platforms = currentResult.Platforms,
    //             ImageExtensions = currentResult.ImageExtensions,
    //             ProfileFiles = currentResult.ProfileFiles,
    //             InstallationFile = $"{Constants.WineBridgePrefix}{matchingConfig.LinuxPath}",
    //             StartupArguments = GetStartupArguments(currentResult, emulatorId),
    //             StartupExecutable = $"{Constants.WineBridgePrefix}{matchingConfig.LinuxPath}",
    //             ScriptStartup = false,
    //             ScriptGameImport = false
    //         };
    //     }
    //
    //     private static string GetStartupArguments(EmulatorDefinitionProfile oldResult, string emulatorId)
    //     {
    //         if (emulatorId != "retroarch")
    //         {
    //             return oldResult.StartupArguments;
    //         }
    //
    //         return Regex.Replace(oldResult.StartupArguments, "-L \"\\.\\\\cores\\\\(.+)_libretro\\.dll\"",
    //             "-L \"$1\"");
    //     }
    //
    //     [SuppressMessage("ReSharper", "UnusedMember.Local")]
    //     [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    //     private static bool GetExecutablePrefix(
    //         [SuppressMessage("ReSharper", "InconsistentNaming")]
    //         ref string __result, string directory,
    //         EmulatorDefinitionProfile profile, bool relative)
    //     {
    //         if (profile.StartupExecutable.StartsWith(Constants.WineBridgePrefix))
    //         {
    //             Logger.Debug($"Replacing executable prefix for profile {profile.Name} to {profile.InstallationFile}");
    //             __result = profile.StartupExecutable;
    //             return false;
    //         }
    //
    //         return true;
    //     }
    // }
    //
    // public static class BitmapExtensionsPatches
    // {
    //     private static readonly ILogger Logger = LogManager.GetLogger();
    //
    //     [SuppressMessage("ReSharper", "UnusedMember.Local")]
    //     private static bool BitmapFromStreamPrefix(
    //         ref Stream stream)
    //     {
    //         if (!WineBridgeSettings.ForceHighQualityIcons)
    //         {
    //             return true;
    //         }
    //
    //         try
    //         {
    //             stream.Seek(0, SeekOrigin.Begin);
    //             stream = IconExtractor.StripIconToTheHighestQualityFrame(stream);
    //         }
    //         catch (Exception e)
    //         {
    //             Logger.Error(e, "Failed to strip icon from stream!");
    //         }
    //
    //         return true;
    //     }
    // }
    //
    // public static class SystemDialogsPatches
    // {
    //     private static readonly ILogger Logger = LogManager.GetLogger();
    //
    //     [SuppressMessage("ReSharper", "UnusedMember.Local")]
    //     private static bool SaveFilePrefix(ref string __result,
    //         Window owner, string filter, bool promptOverwrite, string initialDir)
    //     {
    //         if (!WineBridgeSettings.RedirectFileDirectorySelectionCallsToLinux)
    //         {
    //             return true;
    //         }
    //
    //         try
    //         {
    //             if (filter != null && filter.EndsWith("*.*"))
    //             {
    //                 filter = null;
    //             }
    //
    //             var processWithCorrelationId = LinuxProcessStarter.Start(
    //                 $"{WineUtils.OpenFileScriptPathLinux} \"{WineBridgeSettings.FileDirectorySelectionProgram}\" \"save\" \"{filter ?? string.Empty}\" \"{initialDir ?? string.Empty}\"");
    //             __result = WineUtils.LinuxPathToWindows(
    //                 LinuxProcessMonitor.TrackLinuxProcessGetResult(processWithCorrelationId));
    //             return false;
    //         }
    //         catch (Exception e)
    //         {
    //             Logger.Error(e, "Failed to redirect file directory selection call to Linux!");
    //         }
    //
    //         return true;
    //     }
    //
    //     [SuppressMessage("ReSharper", "UnusedMember.Local")]
    //     private static bool SelectFolderPrefix(ref string __result,
    //         Window owner, string initialDir)
    //     {
    //         if (!WineBridgeSettings.RedirectFileDirectorySelectionCallsToLinux)
    //         {
    //             return true;
    //         }
    //
    //         try
    //         {
    //             var processWithCorrelationId = LinuxProcessStarter.Start(
    //                 $"{WineUtils.OpenFileScriptPathLinux} \"{WineBridgeSettings.FileDirectorySelectionProgram}\" \"directory\" \"\" \"{initialDir ?? string.Empty}\"");
    //             __result = WineUtils.LinuxPathToWindows(
    //                 LinuxProcessMonitor.TrackLinuxProcessGetResult(processWithCorrelationId));
    //             return false;
    //         }
    //         catch (Exception e)
    //         {
    //             Logger.Error(e, "Failed to redirect file directory selection call to Linux!");
    //         }
    //
    //         return true;
    //     }
    //
    //     [SuppressMessage("ReSharper", "UnusedMember.Local")]
    //     private static bool SelectFilesPrefix(ref List<string> __result,
    //         Window owner, string filter, string initialDir)
    //     {
    //         if (!WineBridgeSettings.RedirectFileDirectorySelectionCallsToLinux)
    //         {
    //             return true;
    //         }
    //
    //         try
    //         {
    //             if (filter != null && filter.EndsWith("*.*"))
    //             {
    //                 filter = null;
    //             }
    //
    //             var processWithCorrelationId = LinuxProcessStarter.Start(
    //                 $"{WineUtils.OpenFileScriptPathLinux} \"{WineBridgeSettings.FileDirectorySelectionProgram}\" \"file-multiple\" \"{filter ?? string.Empty}\" \"{initialDir ?? string.Empty}\"");
    //             var result = LinuxProcessMonitor.TrackLinuxProcessGetResultInLines(processWithCorrelationId);
    //             if (result == null)
    //             {
    //                 return true;
    //             }
    //
    //             __result = result.Select(WineUtils.LinuxPathToWindows).ToList();
    //             return false;
    //         }
    //         catch (Exception e)
    //         {
    //             Logger.Error(e, "Failed to redirect file directory selection call to Linux!");
    //         }
    //
    //         return true;
    //     }
    //
    //     [SuppressMessage("ReSharper", "UnusedMember.Local")]
    //     private static bool SelectFilePrefix(ref string __result,
    //         Window owner, string filter, string initialDir)
    //     {
    //         if (!WineBridgeSettings.RedirectFileDirectorySelectionCallsToLinux)
    //         {
    //             return true;
    //         }
    //
    //         try
    //         {
    //             if (filter != null && filter.EndsWith("*.*"))
    //             {
    //                 filter = null;
    //             }
    //
    //             var processWithCorrelationId = LinuxProcessStarter.Start(
    //                 $"{WineUtils.OpenFileScriptPathLinux} \"{WineBridgeSettings.FileDirectorySelectionProgram}\" \"file\" \"{filter ?? string.Empty}\" \"{initialDir ?? string.Empty}\"");
    //             __result = WineUtils.LinuxPathToWindows(
    //                 LinuxProcessMonitor.TrackLinuxProcessGetResult(processWithCorrelationId));
    //             return false;
    //         }
    //         catch (Exception e)
    //         {
    //             Logger.Error(e, "Failed to redirect file directory selection call to Linux!");
    //         }
    //
    //         return true;
    //     }
    // }
}