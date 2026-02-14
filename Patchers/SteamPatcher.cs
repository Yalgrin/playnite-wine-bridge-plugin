using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using HarmonyLib;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using SteamKit2;
using WineBridgePlugin.Integrations.Steam;
using WineBridgePlugin.Models;
using WineBridgePlugin.Processes;
using WineBridgePlugin.Settings;
using WineBridgePlugin.Utils;

namespace WineBridgePlugin.Patchers
{
    public static class SteamPatcher
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
                    .FirstOrDefault(a => a.GetName().Name == "SteamLibrary");

                if (assembly == null)
                {
                    Logger.Warn("Failed to find SteamLibrary assembly!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var steamType = assembly.GetType("SteamLibrary.Steam");
                var steamPlayControllerType = assembly.GetType("SteamLibrary.SteamPlayController");
                var localServiceType = AccessTools.TypeByName("SteamLibrary.Services.SteamLocalService");

                if (steamType == null || steamPlayControllerType == null || localServiceType == null)
                {
                    Logger.Warn("Failed to find SteamLibrary classes!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var isInstalledMethod = steamType.GetProperty("IsInstalled")?.GetGetMethod();
                var installPathMethod = steamType.GetProperty("InstallationPath")?.GetGetMethod();
                var clientPathMethod = steamType.GetProperty("ClientExecPath")?.GetGetMethod();
                var playMethod = steamPlayControllerType.GetMethod("Play");
                var disposeMethod = steamPlayControllerType.GetMethod("Dispose");
                var getInstalledGamesMethod = AccessTools.Method(localServiceType, "GetInstalledGamesFromFolder");

                if (isInstalledMethod == null || installPathMethod == null || clientPathMethod == null ||
                    playMethod == null || disposeMethod == null || getInstalledGamesMethod == null)
                {
                    Logger.Warn("Failed to find SteamLibrary methods!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var steamIsInstalledPrefix = AccessTools.Method(typeof(SteamPatches), "IsInstalledPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(isInstalledMethod,
                    prefix: new HarmonyMethod(steamIsInstalledPrefix));
                var steamInstallationPathPrefix = AccessTools.Method(typeof(SteamPatches), "InstallationPathPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(installPathMethod,
                    prefix: new HarmonyMethod(steamInstallationPathPrefix));
                var steamClientPathPrefix = AccessTools.Method(typeof(SteamPatches), "ClientExecPathPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(clientPathMethod,
                    prefix: new HarmonyMethod(steamClientPathPrefix));

                var playControllerPlayPrefix = AccessTools.Method(typeof(SteamPlayControllerPatches), "PlayPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(playMethod, prefix: new HarmonyMethod(playControllerPlayPrefix));
                var playControllerDisposePrefix =
                    AccessTools.Method(typeof(SteamPlayControllerPatches), "DisposePrefix");
                HarmonyPatcher.HarmonyInstance.Patch(disposeMethod,
                    prefix: new HarmonyMethod(playControllerDisposePrefix));

                var localServiceInstalledGamesPostfix =
                    AccessTools.Method(typeof(SteamLocalServicePatches), "GetInstalledGamesFromFolderPostfix");
                HarmonyPatcher.HarmonyInstance.Patch(getInstalledGamesMethod,
                    postfix: new HarmonyMethod(localServiceInstalledGamesPostfix));

                Logger.Info("Steam methods patched successfully!");
                State = PatchingState.Patched;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while patching Steam methods!");
                State = PatchingState.Error;
            }
        }
    }

    internal static class SteamPatches
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool IsInstalledPrefix([SuppressMessage("ReSharper", "InconsistentNaming")] ref bool __result)
        {
            if (!WineBridgeSettings.SteamIntegrationEnabled)
            {
                return true;
            }

            __result = true;
            return false;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool InstallationPathPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref string __result)
        {
            if (!WineBridgeSettings.SteamIntegrationEnabled)
            {
                return true;
            }

            var installationPath = WineBridgeSettings.SteamDataPathLinux;
            if (installationPath != null)
            {
                __result = WineUtils.LinuxPathToWindows(installationPath);
                return false;
            }

            return true;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool ClientExecPathPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref string __result)
        {
            if (!WineBridgeSettings.SteamIntegrationEnabled)
            {
                return true;
            }

            __result = Constants.DummySteamExe;
            return false;
        }
    }

    internal static class SteamPlayControllerPatches
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
        private static bool PlayPrefix([SuppressMessage("ReSharper", "InconsistentNaming")] PlayController __instance)
        {
            if (!WineBridgeSettings.SteamIntegrationEnabled)
            {
                return true;
            }

            var gameId = AccessTools.Field(__instance.GetType(), "gameId").GetValue(__instance) as GameID;
            var api = AccessTools.Field(__instance.GetType(), "playniteAPI").GetValue(__instance) as IPlayniteAPI;

            __instance.Dispose();

            var gameInstance = __instance.Game;
            var watcherToken = new CancellationTokenSource();
            PlayCancelationTokenSources[__instance] = watcherToken;

            var shouldShowLaunchDialog = ShouldShowLaunchDialog(__instance, gameId, api);
            var process = SteamProcessStarter.Start(gameInstance.GameId, shouldShowLaunchDialog);

            LinuxProcessMonitor.TrackLinuxProcess(__instance, process, watcherToken);

            return false;
        }

        private static bool ShouldShowLaunchDialog(PlayController controller, GameID gameId, IPlayniteAPI api)
        {
            return !(gameId?.IsMod ?? false) && !(gameId?.IsShortcut ?? false) &&
                   ShouldShowLaunchDialog(controller, api);
        }

        private static bool ShouldShowLaunchDialog(PlayController controller, IPlayniteAPI api)
        {
            var settingsObject = AccessTools.Field(controller.GetType(), "settings")?.GetValue(controller);
            if (settingsObject == null)
            {
                return false;
            }

            switch (api?.ApplicationInfo?.Mode)
            {
                case ApplicationMode.Fullscreen:
                {
                    var value = AccessTools
                        .Property(settingsObject.GetType(), "ShowSteamLaunchMenuInFullscreenMode")
                        ?.GetValue(settingsObject);
                    return value is bool b && b;
                }
                case ApplicationMode.Desktop:
                {
                    var value = AccessTools
                        .Property(settingsObject.GetType(), "ShowSteamLaunchMenuInDesktopMode")
                        ?.GetValue(settingsObject);
                    return value is bool b && b;
                }
                default:
                    return false;
            }
        }
    }

    internal static class SteamLocalServicePatches
    {
        private static void GetInstalledGamesFromFolderPostfix(ref List<GameMetadata> __result)
        {
            if (__result == null)
            {
                return;
            }

            var resultCopy = new List<GameMetadata>(__result);
            resultCopy.RemoveAll(metadata => SteamUtils.ExcludedSteamIds.Contains(metadata.GameId));
            __result = resultCopy;
        }
    }
}