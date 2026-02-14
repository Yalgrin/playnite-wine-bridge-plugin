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
    public static class ItchIoPatcher
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
                    .FirstOrDefault(a => a.GetName().Name == "ItchioLibrary");

                if (assembly == null)
                {
                    Logger.Warn("Failed to find ItchioLibrary assembly!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var mainType = assembly.GetType("ItchioLibrary.Itch");
                var libraryType = assembly.GetType("ItchioLibrary.ItchioLibrary");

                if (mainType == null || libraryType == null)
                {
                    Logger.Warn("Failed to find ItchioLibrary classes!");
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
                    Logger.Warn("Failed to find ItchioLibrary methods!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var isInstalledPrefix = AccessTools.Method(typeof(ItchIoPatches), "IsInstalledPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(isInstalledMethod,
                    prefix: new HarmonyMethod(isInstalledPrefix));
                var installationPathPrefix = AccessTools.Method(typeof(ItchIoPatches), "InstallationPathPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(installPathMethod,
                    prefix: new HarmonyMethod(installationPathPrefix));
                var clientPathPrefix = AccessTools.Method(typeof(ItchIoPatches), "ClientExecPathPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(clientPathMethod,
                    prefix: new HarmonyMethod(clientPathPrefix));

                var getInstallActionsPrefix =
                    AccessTools.Method(typeof(ItchioLibraryPatches), "GetInstallActionsPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(getInstallActionsMethod,
                    prefix: new HarmonyMethod(getInstallActionsPrefix));
                var getUninstallActionsPrefix =
                    AccessTools.Method(typeof(ItchioLibraryPatches), "GetUninstallActionsPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(getUninstallActionsMethod,
                    prefix: new HarmonyMethod(getUninstallActionsPrefix));
                var getPlayActionsPrefix =
                    AccessTools.Method(typeof(ItchioLibraryPatches), "GetPlayActionsPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(getPlayActionsMethod,
                    prefix: new HarmonyMethod(getPlayActionsPrefix));
                var getInstalledGamesPrefix =
                    AccessTools.Method(typeof(ItchioLibraryPatches), "GetInstalledGamesPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(getInstalledGamesMethod,
                    prefix: new HarmonyMethod(getInstalledGamesPrefix));

                Logger.Info("Itch.io methods patched successfully!");
                State = PatchingState.Patched;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while patching Itch.io methods!");
                State = PatchingState.Error;
            }
        }
    }

    internal static class ItchIoPatches
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool IsInstalledPrefix([SuppressMessage("ReSharper", "InconsistentNaming")] ref bool __result)
        {
            if (!WineBridgeSettings.LutrisItchIoIntegrationEnabled)
            {
                return true;
            }

            __result = true;
            return false;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool IsRunningPrefix([SuppressMessage("ReSharper", "InconsistentNaming")] ref bool __result)
        {
            if (!WineBridgeSettings.LutrisItchIoIntegrationEnabled)
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
            if (!WineBridgeSettings.LutrisItchIoIntegrationEnabled)
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
            if (!WineBridgeSettings.LutrisItchIoIntegrationEnabled)
            {
                return true;
            }

            __result = Constants.DummyLutrisExe;
            return false;
        }
    }

    internal static class ItchioLibraryPatches
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool GetInstallActionsPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            LibraryPlugin __instance,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref IEnumerable<InstallController> __result, GetInstallActionsArgs args)
        {
            if (!WineBridgeSettings.LutrisItchIoIntegrationEnabled)
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

            yield return new LutrisInstallController(args.Game, LutrisPlatform.ItchIo);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool GetUninstallActionsPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            LibraryPlugin __instance,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref IEnumerable<UninstallController> __result, GetUninstallActionsArgs args)
        {
            if (!WineBridgeSettings.LutrisItchIoIntegrationEnabled)
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

            yield return new LutrisUninstallController(args.Game, LutrisPlatform.ItchIo);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool GetPlayActionsPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            LibraryPlugin __instance,
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref IEnumerable<PlayController> __result,
            GetPlayActionsArgs args)
        {
            if (!WineBridgeSettings.LutrisItchIoIntegrationEnabled)
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

            yield return new LutrisPlayController(args.Game, LutrisPlatform.ItchIo);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool GetInstalledGamesPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            ref Dictionary<string, GameMetadata> __result)
        {
            if (!WineBridgeSettings.LutrisItchIoIntegrationEnabled)
            {
                return true;
            }

            __result = LutrisGamesService.GetInstalledGames(LutrisPlatform.ItchIo);
            return false;
        }
    }
}