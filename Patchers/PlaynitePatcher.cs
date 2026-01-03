using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using WineBridgePlugin.Models;
using WineBridgePlugin.Processes;

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

                var genericPlayControllerType = assembly.GetType("Playnite.Controllers.GenericPlayController");
                var gamesEditorType = assembly.GetType("Playnite.GamesEditor");
                if (genericPlayControllerType == null || gamesEditorType == null)
                {
                    Logger.Warn("Failed to find Playnite classes!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var startMethod = genericPlayControllerType.GetMethod("Start",
                    BindingFlags.Instance | BindingFlags.Public, null,
                    new[] { typeof(GameAction), typeof(bool), typeof(OnGameStartingEventArgs) }, null);
                var disposeMethod = genericPlayControllerType.GetMethod("Dispose");
                var powershellErrorField = AccessTools.Field(gamesEditorType, "showedPowerShellError");
                if (startMethod == null || disposeMethod == null || powershellErrorField == null)
                {
                    Logger.Warn("Failed to find Playnite methods!");
                    State = PatchingState.MissingClasses;
                    return;
                }

                var startControllerPlayPrefix = AccessTools.Method(typeof(GenericPlayGamePatcher), "StartPrefix");
                HarmonyPatcher.HarmonyInstance.Patch(startMethod, prefix: new HarmonyMethod(startControllerPlayPrefix));
                var startControllerDisposePrefix = AccessTools.Method(typeof(GenericPlayGamePatcher), "DisposePrefix");
                HarmonyPatcher.HarmonyInstance.Patch(disposeMethod,
                    prefix: new HarmonyMethod(startControllerDisposePrefix));
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

    public static class GenericPlayGamePatcher
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
        private static bool StartPrefix([SuppressMessage("ReSharper", "InconsistentNaming")] PlayController __instance,
            GameAction playAction, [SuppressMessage("ReSharper", "UnusedParameter.Local")] bool asyncExec,
            OnGameStartingEventArgs startingArgs)
        {
            if (playAction.Type == GameActionType.File)
            {
                if (playAction.Path.StartsWith("wine-bridge://"))
                {
                    AccessTools.Property(__instance.GetType(), "StartingArgs").SetValue(__instance, startingArgs);
                    var watcherToken = new CancellationTokenSource();
                    PlayCancelationTokenSources[__instance] = watcherToken;
                    var process = LinuxProcessStarter.Start(playAction.Path.Substring("wine-bridge://".Length));
                    LinuxProcessMonitor.TrackLinuxProcess(__instance, process, watcherToken);
                    return false;
                }

                if (playAction.Path.StartsWith("wine-bridge-async://"))
                {
                    AccessTools.Property(__instance.GetType(), "StartingArgs").SetValue(__instance, startingArgs);
                    var watcherToken = new CancellationTokenSource();
                    PlayCancelationTokenSources[__instance] = watcherToken;
                    var process = LinuxProcessStarter.Start(playAction.Path.Substring("wine-bridge-async://".Length),
                        true, playAction.Arguments);
                    LinuxProcessMonitor.TrackLinuxProcess(__instance, process, watcherToken);
                    return false;
                }
            }

            return true;
        }
    }
}