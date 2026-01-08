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

                var gogType = assembly.GetType("AmazonGamesLibrary.AmazonGames");
                var gogLibraryType = assembly.GetType("AmazonGamesLibrary.AmazonGamesLibrary");
                // var steamPlayControllerType = assembly.GetType("AmazonGamesLibrary.SteamPlayController");

                if (gogType == null || gogLibraryType == null)
                {
                    Logger.Warn("Failed to find AmazonGamesLibrary classes!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var isInstalledMethod = gogType.GetProperty("IsInstalled")?.GetGetMethod();
                var isRunningMethod = gogType.GetProperty("IsRunning")?.GetGetMethod();
                var installPathMethod = gogType.GetProperty("InstallationPath")?.GetGetMethod();
                var clientPathMethod = gogType.GetProperty("ClientExecPath")?.GetGetMethod();

                var getInstallActionsMethod = gogLibraryType.GetMethod("GetInstallActions");
                var getUninstallActionsMethod = gogLibraryType.GetMethod("GetUninstallActions");
                var getPlayActionsMethod = gogLibraryType.GetMethod("GetPlayActions");
                var getInstalledGamesMethod = gogLibraryType.GetMethod("GetInstalledGames",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (isInstalledMethod == null || installPathMethod == null || clientPathMethod == null
                    || getInstallActionsMethod == null || getUninstallActionsMethod == null ||
                    getPlayActionsMethod == null
                    || getInstalledGamesMethod == null)
                {
                    Logger.Warn("Failed to find AmazonGamesLibrary methods!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var gogIsInstalledPrefix = AccessTools.Method(typeof(AmazonPatches), "IsInstalledPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(isInstalledMethod,
                    prefix: new HarmonyMethod(gogIsInstalledPrefix));
                var gogIsRunningPrefix = AccessTools.Method(typeof(AmazonPatches), "IsRunningPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(isRunningMethod,
                    prefix: new HarmonyMethod(gogIsRunningPrefix));
                var gogInstallationPathPrefix = AccessTools.Method(typeof(AmazonPatches), "InstallationPathPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(installPathMethod,
                    prefix: new HarmonyMethod(gogInstallationPathPrefix));
                var gogClientPathPrefix = AccessTools.Method(typeof(AmazonPatches), "ClientExecPathPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(clientPathMethod,
                    prefix: new HarmonyMethod(gogClientPathPrefix));

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
            if (!WineBridgeSettings.HeroicAmazonIntegrationEnabled)
            {
                return true;
            }

            __result = true;
            return false;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool IsRunningPrefix([SuppressMessage("ReSharper", "InconsistentNaming")] ref bool __result)
        {
            if (!WineBridgeSettings.HeroicAmazonIntegrationEnabled)
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
            if (!WineBridgeSettings.HeroicAmazonIntegrationEnabled)
            {
                return true;
            }

            var installationPath = WineBridgeSettings.HeroicDataPathLinux;
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
            if (!WineBridgeSettings.HeroicAmazonIntegrationEnabled)
            {
                return true;
            }

            __result = Constants.DummyHeroicExe;
            return false;
        }
    }

    internal static class AmazonGamesLibraryPatches
    {
        private static bool GetInstallActionsPrefix(LibraryPlugin __instance,
            ref IEnumerable<InstallController> __result, GetInstallActionsArgs args)
        {
            if (!WineBridgeSettings.HeroicAmazonIntegrationEnabled)
            {
                return true;
            }

            __result = GetInstallActions(__instance, args);
            return false;
        }

        private static IEnumerable<InstallController> GetInstallActions(LibraryPlugin __instance,
            GetInstallActionsArgs args)
        {
            if (args.Game.PluginId != __instance.Id)
            {
                yield break;
            }

            yield return new HeroicInstallController(args.Game, HeroicPlatform.Amazon);
        }

        private static bool GetUninstallActionsPrefix(LibraryPlugin __instance,
            ref IEnumerable<UninstallController> __result, GetUninstallActionsArgs args)
        {
            if (!WineBridgeSettings.HeroicAmazonIntegrationEnabled)
            {
                return true;
            }

            __result = GetUninstallActions(__instance, args);
            return false;
        }

        private static IEnumerable<UninstallController> GetUninstallActions(LibraryPlugin __instance,
            GetUninstallActionsArgs args)
        {
            if (args.Game.PluginId != __instance.Id)
            {
                yield break;
            }

            yield return new HeroicUninstallController(args.Game, HeroicPlatform.Amazon);
        }

        private static bool GetPlayActionsPrefix(LibraryPlugin __instance, ref IEnumerable<PlayController> __result,
            GetPlayActionsArgs args)
        {
            if (!WineBridgeSettings.HeroicAmazonIntegrationEnabled)
            {
                return true;
            }

            __result = GetPlayActions(__instance, args);
            return false;
        }

        private static IEnumerable<PlayController> GetPlayActions(LibraryPlugin __instance, GetPlayActionsArgs args)
        {
            if (args.Game.PluginId != __instance.Id)
            {
                yield break;
            }

            yield return new HeroicPlayController(args.Game, HeroicPlatform.Amazon);
        }


        private static bool GetInstalledGamesPrefix(ref Dictionary<string, GameMetadata> __result)
        {
            if (!WineBridgeSettings.HeroicAmazonIntegrationEnabled)
            {
                return true;
            }

            __result = HeroicGamesService.GetInstalledGames(HeroicPlatform.Amazon);
            return false;
        }
    }
}