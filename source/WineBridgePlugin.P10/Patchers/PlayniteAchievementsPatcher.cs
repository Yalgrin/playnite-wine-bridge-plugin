using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Playnite.SDK;
using WineBridgePlugin.Models;

namespace WineBridgePlugin.Patchers
{
    public static class PlayniteAchievementsPatcher
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

                Logger.Info("PlayniteAchievements methods patched successfully!");
                State = PatchingState.Patched;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while patching PlayniteAchievements methods!");
                State = PatchingState.Error;
            }
        }
    }

    internal static class ProviderRegistryPatches
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static bool DiscoverProviderTypesPrefix(
            [SuppressMessage("ReSharper", "InconsistentNaming")] object __instance, ref IEnumerable<Type> __result)
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
}