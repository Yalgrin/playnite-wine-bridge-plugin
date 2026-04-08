using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using WineBridgePlugin.Integrations.Heroic;
using WineBridgePlugin.Integrations.Lutris;
using WineBridgePlugin.Models;
using WineBridgePlugin.Settings;

namespace WineBridgePlugin.Patchers
{
    public static class EpicPatcher
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
                    .FirstOrDefault(a => a.GetName().Name == "EpicLibrary");

                if (assembly == null)
                {
                    Logger.Warn("Failed to find EpicLibrary assembly!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var mainType = assembly.GetType("EpicLibrary.EpicLauncher");
                var libraryType = assembly.GetType("EpicLibrary.EpicLibrary");

                if (mainType == null || libraryType == null)
                {
                    Logger.Warn("Failed to find EpicLibrary classes!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var isInstalledMethod = mainType.GetProperty("IsInstalled")?.GetGetMethod();
                var installPathMethod = mainType.GetProperty("InstallationPath")?.GetGetMethod();
                var clientPathMethod = mainType.GetProperty("ClientExecPath")?.GetGetMethod();

                var getInstallActionsMethod = libraryType.GetMethod("GetInstallActions");
                var getUninstallActionsMethod = libraryType.GetMethod("GetUninstallActions");
                var getPlayActionsMethod = libraryType.GetMethod("GetPlayActions");
                var getInstalledGamesMethod = libraryType.GetMethod("GetInstalledGames",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (isInstalledMethod == null || installPathMethod == null || clientPathMethod == null
                    || getInstallActionsMethod == null || getUninstallActionsMethod == null ||
                    getPlayActionsMethod == null
                    || getInstalledGamesMethod == null)
                {
                    Logger.Warn("Failed to find EpicLibrary methods!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var isInstalledPrefix = AccessTools.Method(typeof(EpicPatches), "IsInstalledPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(isInstalledMethod,
                    prefix: new HarmonyMethod(isInstalledPrefix));
                var installationPathPrefix = AccessTools.Method(typeof(EpicPatches), "InstallationPathPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(installPathMethod,
                    prefix: new HarmonyMethod(installationPathPrefix));
                var clientPathPrefix = AccessTools.Method(typeof(EpicPatches), "ClientExecPathPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(clientPathMethod,
                    prefix: new HarmonyMethod(clientPathPrefix));

                var getInstallActionsPrefix = AccessTools.Method(typeof(EpicLibraryPatches), "GetInstallActionsPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(getInstallActionsMethod,
                    prefix: new HarmonyMethod(getInstallActionsPrefix));
                var getUninstallActionsPrefix =
                    AccessTools.Method(typeof(EpicLibraryPatches), "GetUninstallActionsPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(getUninstallActionsMethod,
                    prefix: new HarmonyMethod(getUninstallActionsPrefix));
                var getPlayActionsPrefix = AccessTools.Method(typeof(EpicLibraryPatches), "GetPlayActionsPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(getPlayActionsMethod,
                    prefix: new HarmonyMethod(getPlayActionsPrefix));
                var getInstalledGamesPrefix = AccessTools.Method(typeof(EpicLibraryPatches), "GetInstalledGamesPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(getInstalledGamesMethod,
                    prefix: new HarmonyMethod(getInstalledGamesPrefix));

                Logger.Info("Epic methods patched successfully!");
                State = PatchingState.Patched;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while patching Epic methods!");
                State = PatchingState.Error;
            }
        }
    }

    internal static class EpicPatches
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool IsInstalledPrefix([SuppressMessage("ReSharper", "InconsistentNaming")] ref bool __result)
        {
            if (!WineBridgeSettings.AnyEpicIntegrationEnabled)
            {
                Logger.Debug("Heroic integration disabled.");
                return true;
            }

            Logger.Debug("Heroic integration enabled.");
            __result = true;
            return false;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool IsRunningPrefix([SuppressMessage("ReSharper", "InconsistentNaming")] ref bool __result)
        {
            if (!WineBridgeSettings.AnyEpicIntegrationEnabled)
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
            if (!WineBridgeSettings.AnyEpicIntegrationEnabled)
            {
                return true;
            }

            var installationPath = WineBridgeSettings.HeroicEpicIntegrationEnabled
                ? WineBridgeSettings.HeroicDataPathLinux
                : WineBridgeSettings.LutrisDataPathLinux;
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
            if (!WineBridgeSettings.AnyEpicIntegrationEnabled)
            {
                return true;
            }

            __result = WineBridgeSettings.HeroicEpicIntegrationEnabled
                ? Constants.DummyHeroicExe
                : Constants.DummyLutrisExe;
            return false;
        }
    }

    internal static class EpicLibraryPatches
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool GetInstallActionsPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            LibraryPlugin __instance,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref IEnumerable<InstallController> __result, GetInstallActionsArgs args)
        {
            if (!WineBridgeSettings.AnyEpicIntegrationEnabled)
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

            if (WineBridgeSettings.HeroicEpicIntegrationEnabled)
            {
                yield return new HeroicInstallController(args.Game, HeroicPlatform.Epic);
            }

            if (WineBridgeSettings.LutrisEpicIntegrationEnabled)
            {
                yield return new LutrisInstallController(args.Game, LutrisPlatform.Epic);
            }
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool GetUninstallActionsPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            LibraryPlugin __instance,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref IEnumerable<UninstallController> __result, GetUninstallActionsArgs args)
        {
            if (!WineBridgeSettings.AnyEpicIntegrationEnabled)
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

            if (WineBridgeSettings.HeroicEpicIntegrationEnabled && (!WineBridgeSettings.LutrisEpicIntegrationEnabled ||
                                                                    HeroicGamesService.IsGameInstalled(args.Game,
                                                                        HeroicPlatform.Epic)))
            {
                yield return new HeroicUninstallController(args.Game, HeroicPlatform.Epic);
            }

            if (WineBridgeSettings.LutrisEpicIntegrationEnabled && (!WineBridgeSettings.HeroicEpicIntegrationEnabled ||
                                                                    LutrisGamesService.IsGameInstalled(args.Game,
                                                                        LutrisPlatform.Epic)))
            {
                yield return new LutrisUninstallController(args.Game, LutrisPlatform.Epic);
            }
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool GetPlayActionsPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            LibraryPlugin __instance,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref IEnumerable<PlayController> __result,
            GetPlayActionsArgs args)
        {
            if (!WineBridgeSettings.AnyEpicIntegrationEnabled)
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

            if (WineBridgeSettings.HeroicEpicIntegrationEnabled && (!WineBridgeSettings.LutrisEpicIntegrationEnabled ||
                                                                    HeroicGamesService.IsGameInstalled(args.Game,
                                                                        HeroicPlatform.Epic)))
            {
                yield return new HeroicPlayController(args.Game, HeroicPlatform.Epic);
            }

            if (WineBridgeSettings.LutrisEpicIntegrationEnabled && (!WineBridgeSettings.HeroicEpicIntegrationEnabled ||
                                                                    LutrisGamesService.IsGameInstalled(args.Game,
                                                                        LutrisPlatform.Epic)))
            {
                yield return new LutrisPlayController(args.Game, LutrisPlatform.Epic);
            }
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool GetInstalledGamesPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref Dictionary<string, GameMetadata> __result)
        {
            if (!WineBridgeSettings.AnyEpicIntegrationEnabled)
            {
                return true;
            }

            var result = new Dictionary<string, GameMetadata>();
            if (WineBridgeSettings.HeroicEpicIntegrationEnabled)
            {
                HeroicGamesService.GetInstalledGames(HeroicPlatform.Epic)
                    .ForEach(game => result.Add(game.Key, game.Value));
            }

            if (WineBridgeSettings.LutrisEpicIntegrationEnabled)
            {
                LutrisGamesService.GetInstalledGames(LutrisPlatform.Epic).ForEach(game =>
                {
                    if (!result.ContainsKey(game.Key))
                    {
                        result.Add(game.Key, game.Value);
                    }
                });
            }

            __result = result;
            return false;
        }
    }
}