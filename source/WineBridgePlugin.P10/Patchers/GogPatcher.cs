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
    public static class GogPatcher
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
                    .FirstOrDefault(a => a.GetName().Name == "GogLibrary");

                if (assembly == null)
                {
                    Logger.Warn("Failed to find GogLibrary assembly!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var mainType = assembly.GetType("GogLibrary.Gog");
                var libraryType = assembly.GetType("GogLibrary.GogLibrary");

                if (mainType == null || libraryType == null)
                {
                    Logger.Warn("Failed to find GogLibrary classes!");
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
                var getInstalledGamesMethod = libraryType.GetMethod("GetInstalledGames",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                if (isInstalledMethod == null || installPathMethod == null || clientPathMethod == null
                    || getInstallActionsMethod == null || getUninstallActionsMethod == null ||
                    getPlayActionsMethod == null
                    || getInstalledGamesMethod == null)
                {
                    Logger.Warn("Failed to find GogLibrary methods!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var isInstalledPrefix = AccessTools.Method(typeof(GogPatches), "IsInstalledPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(isInstalledMethod,
                    prefix: new HarmonyMethod(isInstalledPrefix));
                var isRunningPrefix = AccessTools.Method(typeof(GogPatches), "IsRunningPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(isRunningMethod,
                    prefix: new HarmonyMethod(isRunningPrefix));
                var installationPathPrefix = AccessTools.Method(typeof(GogPatches), "InstallationPathPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(installPathMethod,
                    prefix: new HarmonyMethod(installationPathPrefix));
                var clientPathPrefix = AccessTools.Method(typeof(GogPatches), "ClientExecPathPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(clientPathMethod,
                    prefix: new HarmonyMethod(clientPathPrefix));

                var getInstallActionsPrefix = AccessTools.Method(typeof(GogLibraryPatches), "GetInstallActionsPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(getInstallActionsMethod,
                    prefix: new HarmonyMethod(getInstallActionsPrefix));
                var getUninstallActionsPrefix =
                    AccessTools.Method(typeof(GogLibraryPatches), "GetUninstallActionsPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(getUninstallActionsMethod,
                    prefix: new HarmonyMethod(getUninstallActionsPrefix));
                var getPlayActionsPrefix = AccessTools.Method(typeof(GogLibraryPatches), "GetPlayActionsPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(getPlayActionsMethod,
                    prefix: new HarmonyMethod(getPlayActionsPrefix));
                var getInstalledGamesPrefix = AccessTools.Method(typeof(GogLibraryPatches), "GetInstalledGamesPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(getInstalledGamesMethod,
                    prefix: new HarmonyMethod(getInstalledGamesPrefix));

                Logger.Info("GOG methods patched successfully!");
                State = PatchingState.Patched;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while patching GOG methods!");
                State = PatchingState.Error;
            }
        }
    }

    internal static class GogPatches
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool IsInstalledPrefix([SuppressMessage("ReSharper", "InconsistentNaming")] ref bool __result)
        {
            if (!WineBridgeSettings.AnyGogIntegrationEnabled)
            {
                return true;
            }

            __result = true;
            return false;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool IsRunningPrefix([SuppressMessage("ReSharper", "InconsistentNaming")] ref bool __result)
        {
            if (!WineBridgeSettings.AnyGogIntegrationEnabled)
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
            if (!WineBridgeSettings.AnyGogIntegrationEnabled)
            {
                return true;
            }

            var installationPath = WineBridgeSettings.HeroicGogIntegrationEnabled
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
            if (!WineBridgeSettings.AnyGogIntegrationEnabled)
            {
                return true;
            }

            __result = WineBridgeSettings.HeroicGogIntegrationEnabled
                ? Constants.DummyHeroicExe
                : Constants.DummyLutrisExe;
            return false;
        }
    }

    internal static class GogLibraryPatches
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool GetInstallActionsPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            LibraryPlugin __instance,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref IEnumerable<InstallController> __result, GetInstallActionsArgs args)
        {
            if (!WineBridgeSettings.AnyGogIntegrationEnabled)
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

            if (WineBridgeSettings.HeroicGogIntegrationEnabled)
            {
                yield return new HeroicInstallController(args.Game, HeroicPlatform.Gog);
            }

            if (WineBridgeSettings.LutrisGogIntegrationEnabled)
            {
                yield return new LutrisInstallController(args.Game, LutrisPlatform.Gog);
            }
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool GetUninstallActionsPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            LibraryPlugin __instance,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref IEnumerable<UninstallController> __result, GetUninstallActionsArgs args)
        {
            if (!WineBridgeSettings.AnyGogIntegrationEnabled)
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

            if (WineBridgeSettings.HeroicGogIntegrationEnabled && (!WineBridgeSettings.LutrisGogIntegrationEnabled ||
                                                                   HeroicGamesService.IsGameInstalled(args.Game,
                                                                       HeroicPlatform.Gog)))
            {
                yield return new HeroicUninstallController(args.Game, HeroicPlatform.Gog);
            }

            if (WineBridgeSettings.LutrisGogIntegrationEnabled && (!WineBridgeSettings.HeroicGogIntegrationEnabled ||
                                                                   LutrisGamesService.IsGameInstalled(args.Game,
                                                                       LutrisPlatform.Gog)))
            {
                yield return new LutrisUninstallController(args.Game, LutrisPlatform.Gog);
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
            if (!WineBridgeSettings.AnyGogIntegrationEnabled)
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

            if (WineBridgeSettings.HeroicGogIntegrationEnabled && (!WineBridgeSettings.LutrisGogIntegrationEnabled ||
                                                                   HeroicGamesService.IsGameInstalled(args.Game,
                                                                       HeroicPlatform.Gog)))
            {
                yield return new HeroicPlayController(args.Game, HeroicPlatform.Gog);
            }

            if (WineBridgeSettings.LutrisGogIntegrationEnabled && (!WineBridgeSettings.HeroicGogIntegrationEnabled ||
                                                                   LutrisGamesService.IsGameInstalled(args.Game,
                                                                       LutrisPlatform.Gog)))
            {
                yield return new LutrisPlayController(args.Game, LutrisPlatform.Gog);
            }
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool GetInstalledGamesPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref Dictionary<string, GameMetadata> __result)
        {
            if (!WineBridgeSettings.AnyGogIntegrationEnabled)
            {
                return true;
            }

            var result = new Dictionary<string, GameMetadata>();
            if (WineBridgeSettings.HeroicGogIntegrationEnabled)
            {
                HeroicGamesService.GetInstalledGames(HeroicPlatform.Gog)
                    .ForEach(game => result.Add(game.Key, game.Value));
            }

            if (WineBridgeSettings.LutrisGogIntegrationEnabled)
            {
                LutrisGamesService.GetInstalledGames(LutrisPlatform.Gog).ForEach(game =>
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