using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Playnite.SDK;
using WineBridgePlugin.Models;
using WineBridgePlugin.Settings;

namespace WineBridgePlugin.Patchers
{
    public static class PlayniteAchievementsPatcher
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        public static PatchingState State { get; private set; } = PatchingState.Unpatched;

        internal static void Patch()
        {
            if (State == PatchingState.Patched || State == PatchingState.PartiallyPatched)
            {
                return;
            }

            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "PlayniteAchievements");

                if (assembly == null)
                {
                    Logger.Warn("Failed to find PlayniteAchievements assembly!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var providerRegistryType = assembly.GetType("PlayniteAchievements.Providers.ProviderRegistry");

                if (providerRegistryType == null)
                {
                    Logger.Warn("Failed to find PlayniteAchievements classes!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var discoverMethod = providerRegistryType.GetMethod("DiscoverProviderTypes",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (discoverMethod == null)
                {
                    Logger.Warn("Failed to find PlayniteAchievements methods!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var discoverProviderTypesPrefix =
                    AccessTools.Method(typeof(ProviderRegistryPatches), "DiscoverProviderTypesPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(discoverMethod,
                    prefix: new HarmonyMethod(discoverProviderTypesPrefix));

                Logger.Info("PlayniteAchievements base methods patched successfully!");

                var notificationPatched = PatchNotificationService(assembly);
                var recordingPatched = PatchRecordingService(assembly);

                if (notificationPatched && recordingPatched)
                {
                    State = PatchingState.Patched;
                }
                else
                {
                    State = PatchingState.PartiallyPatched;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while patching PlayniteAchievements methods!");
                State = PatchingState.Error;
            }
        }

        private static bool PatchNotificationService(Assembly assembly)
        {
            try
            {
                var toastNotificationService =
                    assembly.GetType("PlayniteAchievements.Services.UI.ToastNotificationService");
                var achievementUnlockedArgs =
                    assembly.GetType("PlayniteAchievements.Models.AchievementUnlockedEventArgs");
                if (toastNotificationService == null || achievementUnlockedArgs == null)
                {
                    Logger.Warn("PatchNotificationService > types not found!");
                    return false;
                }

                var shouldProcessMethod = toastNotificationService.GetMethod("ShouldProcess",
                    BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { achievementUnlockedArgs }, null);
                if (shouldProcessMethod == null)
                {
                    Logger.Warn("PatchNotificationService > shouldProcessMethod not found!");
                    return false;
                }


                var shouldProcessPrefix =
                    AccessTools.Method(typeof(ToastNotificationServicePatches), "ShouldProcessPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(shouldProcessMethod,
                    prefix: new HarmonyMethod(shouldProcessPrefix));

                Logger.Info("PlayniteAchievements ToastNotificationService methods patched successfully!");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "PlayniteAchievements ToastNotificationService methods patching failed!");
                return false;
            }
        }

        private static bool PatchRecordingService(Assembly assembly)
        {
            try
            {
                var recordingService =
                    assembly.GetType("PlayniteAchievements.Services.Recording.UnlockRecordingService");
                var achievementUnlockedArgs =
                    assembly.GetType("PlayniteAchievements.Models.AchievementUnlockedEventArgs");
                if (recordingService == null || achievementUnlockedArgs == null)
                {
                    Logger.Warn("PatchRecordingService > types not found!");
                    return false;
                }

                var achievementUnlockedMethod = recordingService.GetMethod("OnAchievementUnlocked",
                    BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(object), achievementUnlockedArgs },
                    null);
                if (achievementUnlockedMethod == null)
                {
                    Logger.Warn("PatchRecordingService > OnAchievementUnlocked not found!");
                    return false;
                }


                var achievementUnblockedPrefix =
                    AccessTools.Method(typeof(UnlockRecordingServicePatches), "OnAchievementUnlockedPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(achievementUnlockedMethod,
                    prefix: new HarmonyMethod(achievementUnblockedPrefix));

                Logger.Info("PlayniteAchievements UnlockRecordingService methods patched successfully!");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "PlayniteAchievements UnlockRecordingService methods patching failed!");
                return false;
            }
        }
    }

    internal static class ProviderRegistryPatches
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool DiscoverProviderTypesPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            object __instance, ref IEnumerable<Type> __result)
        {
            try
            {
                var type = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Select(a => a.GetType("PlayniteAchievements.Providers.IDataProvider"))
                    .FirstOrDefault(t => t != null);
                if (type == null)
                {
                    Logger.Warn("No provider type found!");
                    return true;
                }

                try
                {
                    __result = __instance.GetType().Assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters &&
                                    type.IsAssignableFrom(t));
                }
                catch (ReflectionTypeLoadException ex)
                {
                    __result = ex.Types.Where(t =>
                        t != null && t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters &&
                        type.IsAssignableFrom(t));
                }

                return false;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while replacing method for looking for provider types!");
            }

            return true;
        }
    }

    internal static class ToastNotificationServicePatches
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool ShouldProcessPrefix([SuppressMessage("ReSharper", "InconsistentNaming")] ref bool __result)
        {
            if (WineBridgeSettings.DebugLoggingEnabled)
            {
                Logger.Debug("Masked ShouldProcess in ToastNotificationService");
            }

            __result = false;
            return false;
        }
    }

    internal static class UnlockRecordingServicePatches
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool OnAchievementUnlockedPrefix()
        {
            if (WineBridgeSettings.DebugLoggingEnabled)
            {
                Logger.Debug("Masked OnAchievementUnlocked in UnlockRecordingService");
            }

            return false;
        }
    }
}