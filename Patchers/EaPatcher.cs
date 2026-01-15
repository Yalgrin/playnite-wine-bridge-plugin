using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using WineBridgePlugin.Integrations.Lutris;
using WineBridgePlugin.Models;
using WineBridgePlugin.Settings;

namespace WineBridgePlugin.Patchers
{
    public static class EaPatcher
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
                    .FirstOrDefault(a => a.GetName().Name == "EaLibrary");

                if (assembly == null)
                {
                    Logger.Warn("Failed to find EaLibrary assembly!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var mainType = assembly.GetType("EaLibrary.EaApp");
                var libraryType = assembly.GetType("EaLibrary.EaLibrary");
                var dataGathererType = assembly.GetType("EaLibrary.EaLibraryDataGatherer");

                if (mainType == null || libraryType == null || dataGathererType == null)
                {
                    Logger.Warn("Failed to find EaLibrary classes!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var isInstalledMethod = mainType.GetProperty("IsInstalled")?.GetGetMethod();
                var isRunningMethod = mainType.GetProperty("IsRunning")?.GetGetMethod();
                var installPathMethod = mainType.GetProperty("InstallationPath")?.GetGetMethod();
                var clientPathMethod = mainType.GetProperty("ClientExecPath")?.GetGetMethod();

                var getInstallActionsMethod = libraryType.GetMethod("GetInstallActions");
                var getUninstallActionsMethod = libraryType.GetMethod("GetUninstallActions");
                var getPlayActionsMethod = libraryType.GetMethod("GetPlayActions");

                var getGamesMethod = libraryType.GetMethod("GetGames",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (isInstalledMethod == null || installPathMethod == null || clientPathMethod == null
                    || getInstallActionsMethod == null || getUninstallActionsMethod == null ||
                    getPlayActionsMethod == null)
                {
                    Logger.Warn("Failed to find EaLibrary methods!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var isInstalledPrefix = AccessTools.Method(typeof(EaPatches), "IsInstalledPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(isInstalledMethod,
                    prefix: new HarmonyMethod(isInstalledPrefix));
                var isRunningPrefix = AccessTools.Method(typeof(EaPatches), "IsRunningPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(isRunningMethod,
                    prefix: new HarmonyMethod(isRunningPrefix));
                var installationPathPrefix = AccessTools.Method(typeof(EaPatches), "InstallationPathPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(installPathMethod,
                    prefix: new HarmonyMethod(installationPathPrefix));
                var clientPathPrefix = AccessTools.Method(typeof(EaPatches), "ClientExecPathPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(clientPathMethod,
                    prefix: new HarmonyMethod(clientPathPrefix));

                var getInstallActionsPrefix =
                    AccessTools.Method(typeof(EaLibraryPatches), "GetInstallActionsPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(getInstallActionsMethod,
                    prefix: new HarmonyMethod(getInstallActionsPrefix));
                var getUninstallActionsPrefix =
                    AccessTools.Method(typeof(EaLibraryPatches), "GetUninstallActionsPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(getUninstallActionsMethod,
                    prefix: new HarmonyMethod(getUninstallActionsPrefix));
                var getPlayActionsPrefix =
                    AccessTools.Method(typeof(EaLibraryPatches), "GetPlayActionsPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(getPlayActionsMethod,
                    prefix: new HarmonyMethod(getPlayActionsPrefix));
                var getGamesPostfixMethod =
                    AccessTools.Method(typeof(EaLibraryPatches), "GetGamesPostfix");
                var getGamesFinalizerMethod =
                    AccessTools.Method(typeof(EaLibraryPatches), "GetGamesFinalizer");
                HarmonyPatcher.HarmonyInstance.Patch(getGamesMethod, postfix: new HarmonyMethod(getGamesPostfixMethod),
                    finalizer: new HarmonyMethod(getGamesFinalizerMethod));

                Logger.Info("EA methods patched successfully!");
                State = PatchingState.Patched;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while patching EA methods!");
                State = PatchingState.Error;
            }
        }
    }

    internal static class EaPatches
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool IsInstalledPrefix([SuppressMessage("ReSharper", "InconsistentNaming")] ref bool __result)
        {
            if (!WineBridgeSettings.LutrisEaIntegrationEnabled)
            {
                return true;
            }

            __result = true;
            return false;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool IsRunningPrefix([SuppressMessage("ReSharper", "InconsistentNaming")] ref bool __result)
        {
            if (!WineBridgeSettings.LutrisEaIntegrationEnabled)
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
            if (!WineBridgeSettings.LutrisEaIntegrationEnabled)
            {
                return true;
            }

            var installationPath = WineBridgeSettings.LutrisDataPathLinux;
            if (installationPath != null)
            {
                __result = installationPath;
                return false;
            }

            return true;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool ClientExecPathPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref string __result)
        {
            if (!WineBridgeSettings.LutrisEaIntegrationEnabled)
            {
                return true;
            }

            __result = Constants.DummyLutrisExe;
            return false;
        }
    }

    internal static class EaLibraryPatches
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool GetInstallActionsPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            LibraryPlugin __instance,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref IEnumerable<InstallController> __result, GetInstallActionsArgs args)
        {
            if (!WineBridgeSettings.LutrisEaIntegrationEnabled)
            {
                return true;
            }

            __result = GetInstallActions(__instance, args);
            return false;
        }

        private static IEnumerable<InstallController> GetInstallActions(LibraryPlugin libraryPlugin,
            GetInstallActionsArgs args)
        {
            if (args.Game.PluginId != libraryPlugin.Id)
            {
                yield break;
            }

            yield return new LutrisInstallController(args.Game, LutrisPlatform.EaApp);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool GetUninstallActionsPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            LibraryPlugin __instance,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref IEnumerable<UninstallController> __result, GetUninstallActionsArgs args)
        {
            if (!WineBridgeSettings.LutrisEaIntegrationEnabled)
            {
                return true;
            }

            __result = GetUninstallActions(__instance, args);
            return false;
        }

        private static IEnumerable<UninstallController> GetUninstallActions(LibraryPlugin libraryPlugin,
            GetUninstallActionsArgs args)
        {
            if (args.Game.PluginId != libraryPlugin.Id)
            {
                yield break;
            }

            yield return new LutrisUninstallController(args.Game, LutrisPlatform.EaApp);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool GetPlayActionsPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            LibraryPlugin __instance,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref IEnumerable<PlayController> __result,
            GetPlayActionsArgs args)
        {
            if (!WineBridgeSettings.LutrisEaIntegrationEnabled)
            {
                return true;
            }

            __result = GetPlayActions(__instance, args);
            return false;
        }

        private static IEnumerable<PlayController> GetPlayActions(LibraryPlugin libraryPlugin, GetPlayActionsArgs args)
        {
            if (args.Game.PluginId != libraryPlugin.Id)
            {
                yield break;
            }

            yield return new LutrisPlayController(args.Game, LutrisPlatform.EaApp);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static void GetGamesPostfix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref IEnumerable<GameMetadata> __result)
        {
            if (!WineBridgeSettings.LutrisEaIntegrationEnabled)
            {
                return;
            }

            var gameMetadatas = __result.ToList();
            try
            {
                var lutrisInstalledGames = LutrisGamesService.GetInstalledGames(LutrisPlatform.EaApp);

                lutrisInstalledGames.ForEach(game =>
                {
                    var gameMetadata = gameMetadatas.FirstOrDefault(g => g.GameId == game.Key);
                    if (gameMetadata != null)
                    {
                        gameMetadata.IsInstalled = game.Value.IsInstalled;
                        gameMetadata.InstallDirectory = game.Value.InstallDirectory;
                    }
                    else
                    {
                        gameMetadatas.Add(game.Value);
                    }
                });
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to get installed games from Lutris database.");
            }

            __result = gameMetadatas;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static Exception GetGamesFinalizer(
            [SuppressMessage("ReSharper", "InconsistentNaming")] Exception __exception,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref IEnumerable<GameMetadata> __result)
        {
            if (__exception != null)
            {
                try
                {
                    Logger.Debug(__exception, "Original integration threw the following exception");
                    __result = LutrisGamesService.GetInstalledGames(LutrisPlatform.EaApp).Values;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to get installed games from Lutris database.");
                    return __exception;
                }
            }

            return null;
        }
    }
}