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
    public static class AmazonPatcher
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
                    .FirstOrDefault(a => a.GetName().Name == "AmazonGamesLibrary");

                if (assembly == null)
                {
                    Logger.Warn("Failed to find AmazonGamesLibrary assembly!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var mainType = assembly.GetType("AmazonGamesLibrary.AmazonGames");
                var libraryType = assembly.GetType("AmazonGamesLibrary.AmazonGamesLibrary");

                if (mainType == null || libraryType == null)
                {
                    Logger.Warn("Failed to find AmazonGamesLibrary classes!");
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
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (isInstalledMethod == null || installPathMethod == null || clientPathMethod == null
                    || getInstallActionsMethod == null || getUninstallActionsMethod == null ||
                    getPlayActionsMethod == null || getInstalledGamesMethod == null)
                {
                    Logger.Warn("Failed to find AmazonGamesLibrary methods!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var isInstalledPrefix = AccessTools.Method(typeof(AmazonPatches), "IsInstalledPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(isInstalledMethod,
                    prefix: new HarmonyMethod(isInstalledPrefix));
                var isRunningPrefix = AccessTools.Method(typeof(AmazonPatches), "IsRunningPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(isRunningMethod,
                    prefix: new HarmonyMethod(isRunningPrefix));
                var installationPathPrefix = AccessTools.Method(typeof(AmazonPatches), "InstallationPathPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(installPathMethod,
                    prefix: new HarmonyMethod(installationPathPrefix));
                var clientPathPrefix = AccessTools.Method(typeof(AmazonPatches), "ClientExecPathPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(clientPathMethod,
                    prefix: new HarmonyMethod(clientPathPrefix));

                var getInstallActionsPrefix =
                    AccessTools.Method(typeof(AmazonGamesLibraryPatches), "GetInstallActionsPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(getInstallActionsMethod,
                    prefix: new HarmonyMethod(getInstallActionsPrefix));
                var getUninstallActionsPrefix =
                    AccessTools.Method(typeof(AmazonGamesLibraryPatches), "GetUninstallActionsPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(getUninstallActionsMethod,
                    prefix: new HarmonyMethod(getUninstallActionsPrefix));
                var getPlayActionsPrefix =
                    AccessTools.Method(typeof(AmazonGamesLibraryPatches), "GetPlayActionsPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(getPlayActionsMethod,
                    prefix: new HarmonyMethod(getPlayActionsPrefix));
                var getInstalledGamesPrefix =
                    AccessTools.Method(typeof(AmazonGamesLibraryPatches), "GetInstalledGamesPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(getInstalledGamesMethod,
                    prefix: new HarmonyMethod(getInstalledGamesPrefix));

                Logger.Info("Amazon methods patched successfully!");
                State = PatchingState.Patched;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while patching Amazon methods!");
                State = PatchingState.Error;
            }
        }
    }

    internal static class AmazonPatches
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool IsInstalledPrefix([SuppressMessage("ReSharper", "InconsistentNaming")] ref bool __result)
        {
            if (!WineBridgeSettings.AnyAmazonIntegrationEnabled)
            {
                return true;
            }

            __result = true;
            return false;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool IsRunningPrefix([SuppressMessage("ReSharper", "InconsistentNaming")] ref bool __result)
        {
            if (!WineBridgeSettings.AnyAmazonIntegrationEnabled)
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
            if (!WineBridgeSettings.AnyAmazonIntegrationEnabled)
            {
                return true;
            }

            var installationPath = WineBridgeSettings.HeroicAmazonIntegrationEnabled
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
            if (!WineBridgeSettings.AnyAmazonIntegrationEnabled)
            {
                return true;
            }

            __result = WineBridgeSettings.HeroicAmazonIntegrationEnabled
                ? Constants.DummyHeroicExe
                : Constants.DummyLutrisExe;
            return false;
        }
    }

    internal static class AmazonGamesLibraryPatches
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool GetInstallActionsPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            LibraryPlugin __instance,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref IEnumerable<InstallController> __result, GetInstallActionsArgs args)
        {
            if (!WineBridgeSettings.AnyAmazonIntegrationEnabled)
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

            if (WineBridgeSettings.HeroicAmazonIntegrationEnabled)
            {
                yield return new HeroicInstallController(args.Game, HeroicPlatform.Amazon);
            }

            if (WineBridgeSettings.LutrisAmazonIntegrationEnabled)
            {
                yield return new LutrisInstallController(args.Game, LutrisPlatform.Amazon);
            }
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool GetUninstallActionsPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            LibraryPlugin __instance,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref IEnumerable<UninstallController> __result, GetUninstallActionsArgs args)
        {
            if (!WineBridgeSettings.AnyAmazonIntegrationEnabled)
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

            if (WineBridgeSettings.HeroicAmazonIntegrationEnabled &&
                (!WineBridgeSettings.LutrisAmazonIntegrationEnabled ||
                 HeroicGamesService.IsGameInstalled(args.Game, HeroicPlatform.Amazon)))
            {
                yield return new HeroicUninstallController(args.Game, HeroicPlatform.Amazon);
            }

            if (WineBridgeSettings.LutrisAmazonIntegrationEnabled &&
                (!WineBridgeSettings.HeroicAmazonIntegrationEnabled ||
                 LutrisGamesService.IsGameInstalled(args.Game, LutrisPlatform.Amazon)))
            {
                yield return new LutrisUninstallController(args.Game, LutrisPlatform.Amazon);
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
            if (!WineBridgeSettings.AnyAmazonIntegrationEnabled)
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

            if (WineBridgeSettings.HeroicAmazonIntegrationEnabled &&
                (!WineBridgeSettings.LutrisAmazonIntegrationEnabled ||
                 HeroicGamesService.IsGameInstalled(args.Game, HeroicPlatform.Amazon)))
            {
                yield return new HeroicPlayController(args.Game, HeroicPlatform.Amazon);
            }

            if (WineBridgeSettings.LutrisAmazonIntegrationEnabled &&
                (!WineBridgeSettings.HeroicAmazonIntegrationEnabled ||
                 LutrisGamesService.IsGameInstalled(args.Game, LutrisPlatform.Amazon)))
            {
                yield return new LutrisPlayController(args.Game, LutrisPlatform.Amazon);
            }
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool GetInstalledGamesPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref Dictionary<string, GameMetadata> __result)
        {
            if (!WineBridgeSettings.AnyAmazonIntegrationEnabled)
            {
                return true;
            }

            var result = new Dictionary<string, GameMetadata>();
            if (WineBridgeSettings.HeroicAmazonIntegrationEnabled)
            {
                HeroicGamesService.GetInstalledGames(HeroicPlatform.Amazon)
                    .ForEach(game => result.Add(game.Key, game.Value));
            }

            if (WineBridgeSettings.LutrisAmazonIntegrationEnabled)
            {
                LutrisGamesService.GetInstalledGames(LutrisPlatform.Amazon).ForEach(game =>
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