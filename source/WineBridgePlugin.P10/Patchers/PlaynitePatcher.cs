using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
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

                var playniteApplicationType = assembly.GetType("Playnite.PlayniteApplication");
                var genericPlayControllerType = assembly.GetType("Playnite.Controllers.GenericPlayController");
                var gamesEditorType = assembly.GetType("Playnite.GamesEditor");
                var emulationType = assembly.GetType("Playnite.Emulators.Emulation");
                var bitmapExtensionsType = assembly.GetType("System.Drawing.Imaging.BitmapExtensions");
                var systemDialogsType = assembly.GetType("Playnite.Common.SystemDialogs");
                if (genericPlayControllerType == null || gamesEditorType == null || emulationType == null
                    || bitmapExtensionsType == null || systemDialogsType == null || playniteApplicationType == null)
                {
                    Logger.Warn("Failed to find Playnite classes!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var crashMethod = playniteApplicationType.GetMethod("CurrentDomain_UnhandledException",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var extensionLoadedMethod = playniteApplicationType.GetMethod("OnExtensionsLoaded",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
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
                var saveFileMethod = systemDialogsType.GetMethod("SaveFile",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                    new[] { typeof(Window), typeof(string), typeof(bool), typeof(string) }, null);
                var selectFolderMethod = systemDialogsType.GetMethod("SelectFolder",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                    new[] { typeof(Window), typeof(string) }, null);
                var selectFileMethod = systemDialogsType.GetMethod("SelectFile",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                    new[] { typeof(Window), typeof(string), typeof(string) }, null);
                var selectFilesMethod = systemDialogsType.GetMethod("SelectFiles",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                    new[] { typeof(Window), typeof(string), typeof(string) }, null);
                if (startMethod == null || disposeMethod == null || powershellErrorField == null ||
                    startEmulatorMethod == null || getProfileMethod == null || getExecutableMethod == null ||
                    bitmapFromStreamMethod == null || saveFileMethod == null || selectFolderMethod == null ||
                    selectFileMethod == null || selectFilesMethod == null || crashMethod == null ||
                    extensionLoadedMethod == null)
                {
                    Logger.Warn("Failed to find Playnite methods!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var crashPrefix = AccessTools.Method(typeof(ApplicationPatcher), "CrashPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(crashMethod, prefix: new HarmonyMethod(crashPrefix));
                var extensionLoadedPrefix = AccessTools.Method(typeof(ApplicationPatcher), "ExtensionLoadedPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(extensionLoadedMethod,
                    prefix: new HarmonyMethod(extensionLoadedPrefix));

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

                var saveFilePrefix = AccessTools.Method(typeof(SystemDialogsPatches), "SaveFilePrefix");
                HarmonyPatcher.HarmonyInstance.Patch(saveFileMethod, prefix: new HarmonyMethod(saveFilePrefix));
                var selectFolderPrefix = AccessTools.Method(typeof(SystemDialogsPatches), "SelectFolderPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(selectFolderMethod, prefix: new HarmonyMethod(selectFolderPrefix));
                var selectFilesPrefix = AccessTools.Method(typeof(SystemDialogsPatches), "SelectFilesPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(selectFilesMethod, prefix: new HarmonyMethod(selectFilesPrefix));
                var selectFilePrefix = AccessTools.Method(typeof(SystemDialogsPatches), "SelectFilePrefix");
                HarmonyPatcher.HarmonyInstance.Patch(selectFileMethod, prefix: new HarmonyMethod(selectFilePrefix));

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

    public static class ApplicationPatcher
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        public static bool CrashPrefix(
            [SuppressMessage("ReSharper", "UnusedParameter.Global")]
            object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Error(e.ExceptionObject as Exception, "Playnite crashed!");
            return true;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        public static bool ExtensionLoadedPrefix(
            [SuppressMessage("ReSharper", "UnusedParameter.Global")]
            object __instance)
        {
            if (!WineBridgeSettings.ForceLoadFailedPluginsEnabled)
            {
                return true;
            }

            try
            {
                LoadFailedPlugins(__instance);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error occurred while loading failed plugins!");
            }

            return true;
        }

        private static void LoadFailedPlugins(object instance)
        {
            var extensions = instance.GetType().GetProperty("Extensions")?.GetValue(instance);
            var failedExtensions = extensions?.GetType().GetProperty("FailedExtensions")?.GetValue(extensions);
            var plugins = extensions?.GetType().GetProperty("Plugins")?.GetValue(extensions) as IDictionary;
            var apiGenerator =
                extensions?.GetType().GetField("apiGenerator", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(extensions) as Delegate;

            if (extensions == null || failedExtensions == null || plugins == null || apiGenerator == null)
            {
                Logger.Warn("Failed to load failed plugins!");
                return;
            }

            var loadedPluginType = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(a => a.GetType("Playnite.Plugins.LoadedPlugin"))
                .FirstOrDefault(t => t != null);

            Logger.Info($"Failed extensions: {failedExtensions}");
            if (failedExtensions is IList failedExtensionList)
            {
                Logger.Info($"Failed extensions count: {failedExtensionList.Count}");
                var loadedExtensions = new List<object>();
                foreach (var failedExtension in failedExtensionList)
                {
                    DoLoadPlugin(failedExtension, extensions, apiGenerator, plugins, loadedPluginType,
                        loadedExtensions);
                }

                foreach (object loadedExtension in loadedExtensions)
                {
                    failedExtensionList.Remove(loadedExtension);
                }
            }
        }

        private static void DoLoadPlugin(object failedExtension, object extensions, Delegate apiGenerator,
            IDictionary plugins,
            Type loadedPluginType, List<object> loadedExtensions)
        {
            try
            {
                var tupleType = failedExtension.GetType();

                var item1Field = tupleType.GetField("Item1");
                var descriptor = item1Field.GetValue(failedExtension);
                Logger.Info($"Failed extension: {descriptor}");

                var descriptionPath =
                    descriptor.GetType().GetProperty("DescriptionPath")?.GetValue(descriptor) as string;
                var module = descriptor.GetType().GetProperty("Module")?.GetValue(descriptor) as string;

                if (descriptionPath == null || module == null)
                {
                    return;
                }

                var directoryName = Path.GetDirectoryName(descriptionPath);

                if (directoryName == null)
                {
                    return;
                }

                var asmPath = Path.Combine(directoryName, module);
                var asmName = AssemblyName.GetAssemblyName(asmPath);
                var assembly = Assembly.Load(asmName);

                var verifyMethod = extensions.GetType().GetMethod("VerifyAssemblyReferences",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                var verifyAssemblyReferencesResult = verifyMethod?.Invoke(extensions, new[] { assembly, descriptor });
                var pluginResult = new List<Plugin>();
                if (verifyAssemblyReferencesResult is bool result && result)
                {
                    var types = LoadTypesSafely(assembly);

                    foreach (var type in types)
                    {
                        if (type.IsInterface || type.IsAbstract)
                        {
                            continue;
                        }

                        if (!typeof(GenericPlugin).IsAssignableFrom(type) &&
                            !typeof(LibraryPlugin).IsAssignableFrom(type) &&
                            !typeof(MetadataPlugin).IsAssignableFrom(type))
                        {
                            continue;
                        }

                        var ignore = Attribute.IsDefined(type, typeof(IgnorePluginAttribute));
                        var load = Attribute.IsDefined(type, typeof(LoadPluginAttribute));
                        if ((ignore && load) || !ignore)
                        {
                            var api = apiGenerator.DynamicInvoke(descriptor);
                            pluginResult.Add(
                                (Plugin)Activator.CreateInstance(type, api));
                        }
                    }
                }
                else
                {
                    Logger.Error($"Plugin dependencies are not compatible: {descriptor}");
                }

                foreach (var plugin in pluginResult)
                {
                    if (plugin.Id == Guid.Empty)
                    {
                        Logger.Error($"Plugin {plugin.GetType()} doesn't have plugin ID specified.");
                        continue;
                    }

                    if (plugins.Contains(plugin.Id))
                    {
                        Logger.Warn($"Plugin {plugin.Id} is already loaded.");
                        continue;
                    }

                    var desc = AccessTools
                        .Constructor(loadedPluginType, new Type[] { typeof(Plugin), descriptor.GetType() })
                        .Invoke(new object[] { plugin, descriptor });
                    plugins.Add(plugin.Id, desc);
                    Logger.Info($"Loaded plugin: {desc}");
                }

                loadedExtensions.Add(failedExtension);
            }
            catch (TargetInvocationException ex)
            {
                Logger.Error(ex, $"Failed to load plugin: {failedExtension}");
                var eInnerException = ex.InnerException;
                if (eInnerException is ReflectionTypeLoadException reflectionTypeLoadException)
                {
                    Logger.Error(ex, $"Failed to load plugin: {failedExtension}");
                    reflectionTypeLoadException.LoaderExceptions.ToList()
                        .ForEach(e => Logger.Error(e, "Errored load type"));
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                Logger.Error(ex, $"Failed to load plugin: {failedExtension}");
                ex.LoaderExceptions.ToList().ForEach(e => Logger.Error(e, "Errored load type"));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to load plugin: {failedExtension}");
            }
        }

        private static Type[] LoadTypesSafely(Assembly assembly)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray();
            }

            return types;
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
            if (PlayCancelationTokenSources.TryGetValue(__instance, out var token))
            {
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
                    ProcessTrackingMode.Asynchronous, playAction.Arguments);
                LinuxProcessMonitor.TrackLinuxProcess(__instance, process, watcherToken);
                return false;
            }

            if (playAction.Path.StartsWith(Constants.WineBridgeAsyncContPrefix))
            {
                AccessTools.Property(__instance.GetType(), "StartingArgs").SetValue(__instance, startingArgs);
                var watcherToken = new CancellationTokenSource();
                PlayCancelationTokenSources[__instance] = watcherToken;
                var process = LinuxProcessStarter.Start(
                    playAction.Path.Substring(Constants.WineBridgeAsyncContPrefix.Length),
                    ProcessTrackingMode.AsynchronousContinuous, playAction.Arguments);
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
                var adjustedArgs = args;
                if (!string.IsNullOrEmpty(adjustedArgs) && !string.IsNullOrEmpty(romPath))
                {
                    adjustedArgs = adjustedArgs.Replace(romPath, WineUtils.WindowsPathToLinux(romPath));
                }

                LinuxProcess process;
                if (!string.IsNullOrEmpty(adjustedArgs))
                {
                    process = LinuxProcessStarter.Start(
                        $"{path.Replace(Constants.WineBridgePrefix, "")} {adjustedArgs}");
                }
                else
                {
                    process = LinuxProcessStarter.Start(path.Replace(Constants.WineBridgePrefix, ""));
                }

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

    public static class SystemDialogsPatches
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool SaveFilePrefix(ref string __result,
            Window owner, string filter, bool promptOverwrite, string initialDir)
        {
            if (!WineBridgeSettings.RedirectFileDirectorySelectionCallsToLinux)
            {
                return true;
            }

            try
            {
                if (filter != null && filter.EndsWith("*.*"))
                {
                    filter = null;
                }

                var processWithCorrelationId = LinuxProcessStarter.Start(
                    $"{WineUtils.OpenFileScriptPathLinux} \"{WineBridgeSettings.FileDirectorySelectionProgram}\" \"save\" \"{filter ?? string.Empty}\" \"{initialDir ?? string.Empty}\"");
                __result = WineUtils.LinuxPathToWindows(
                    LinuxProcessMonitor.TrackLinuxProcessGetResult(processWithCorrelationId));
                return false;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to redirect file directory selection call to Linux!");
            }

            return true;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool SelectFolderPrefix(ref string __result,
            Window owner, string initialDir)
        {
            if (!WineBridgeSettings.RedirectFileDirectorySelectionCallsToLinux)
            {
                return true;
            }

            try
            {
                var processWithCorrelationId = LinuxProcessStarter.Start(
                    $"{WineUtils.OpenFileScriptPathLinux} \"{WineBridgeSettings.FileDirectorySelectionProgram}\" \"directory\" \"\" \"{initialDir ?? string.Empty}\"");
                __result = WineUtils.LinuxPathToWindows(
                    LinuxProcessMonitor.TrackLinuxProcessGetResult(processWithCorrelationId));
                return false;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to redirect file directory selection call to Linux!");
            }

            return true;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool SelectFilesPrefix(ref List<string> __result,
            Window owner, string filter, string initialDir)
        {
            if (!WineBridgeSettings.RedirectFileDirectorySelectionCallsToLinux)
            {
                return true;
            }

            try
            {
                if (filter != null && filter.EndsWith("*.*"))
                {
                    filter = null;
                }

                var processWithCorrelationId = LinuxProcessStarter.Start(
                    $"{WineUtils.OpenFileScriptPathLinux} \"{WineBridgeSettings.FileDirectorySelectionProgram}\" \"file-multiple\" \"{filter ?? string.Empty}\" \"{initialDir ?? string.Empty}\"");
                var result = LinuxProcessMonitor.TrackLinuxProcessGetResultInLines(processWithCorrelationId);
                if (result == null)
                {
                    return true;
                }

                __result = result.Select(WineUtils.LinuxPathToWindows).ToList();
                return false;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to redirect file directory selection call to Linux!");
            }

            return true;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool SelectFilePrefix(ref string __result,
            Window owner, string filter, string initialDir)
        {
            if (!WineBridgeSettings.RedirectFileDirectorySelectionCallsToLinux)
            {
                return true;
            }

            try
            {
                if (filter != null && filter.EndsWith("*.*"))
                {
                    filter = null;
                }

                var processWithCorrelationId = LinuxProcessStarter.Start(
                    $"{WineUtils.OpenFileScriptPathLinux} \"{WineBridgeSettings.FileDirectorySelectionProgram}\" \"file\" \"{filter ?? string.Empty}\" \"{initialDir ?? string.Empty}\"");
                __result = WineUtils.LinuxPathToWindows(
                    LinuxProcessMonitor.TrackLinuxProcessGetResult(processWithCorrelationId));
                return false;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to redirect file directory selection call to Linux!");
            }

            return true;
        }
    }
}