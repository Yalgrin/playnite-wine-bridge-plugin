using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Playnite.SDK;
using WineBridgePlugin.Models;
using WineBridgePlugin.Processes;
using WineBridgePlugin.Settings;
using WineBridgePlugin.Utils;

namespace WineBridgePlugin.Patchers
{
    public static class HarmonyPatcher
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        public static readonly Harmony HarmonyInstance = new Harmony("pl.yalgrin.winebridge");

        public static void Initialize()
        {
            if (!WineDetector.IsRunningUnderWine())
            {
                Logger.Warn("Not running under Wine, skipping patching.");
                return;
            }

            Logger.Info("Running under Wine, running Harmony patches...");
            SystemPatcher.Patch();
            SteamPatcher.Patch();
            PlaynitePatcher.Patch();
            GogPatcher.Patch();
            AmazonPatcher.Patch();
            EpicPatcher.Patch();

            AppDomain.CurrentDomain.AssemblyLoad += (sender, args) =>
            {
                switch (args.LoadedAssembly.GetName().Name)
                {
                    case "SteamLibrary":
                        SteamPatcher.Patch();
                        break;
                    case "Playnite":
                        PlaynitePatcher.Patch();
                        break;
                    case "GogLibrary":
                        GogPatcher.Patch();
                        break;
                    case "AmazonLibrary":
                        AmazonPatcher.Patch();
                        break;
                    case "EpicLibrary":
                        EpicPatcher.Patch();
                        break;
                }
            };
        }

        public static void MakeScriptExecutable()
        {
            if (!WineBridgeSettings.SetScriptExecutePermissions)
            {
                return;
            }

            if (SystemPatcher.State != PatchingState.Patched)
            {
                Logger.Warn("System methods are not patched, skipping script executable modification.");
                return;
            }

            try
            {
                var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (directoryName == null)
                {
                    Logger.Error("Could not determine plugin directory.");
                    return;
                }

                var scriptPath = Path.Combine(directoryName, @"Resources\run-in-linux.sh");
                LinuxProcessStarter.StartRawCommand($"chmod a+x '{WineUtils.WindowsPathToLinux(scriptPath)}'");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while making script executable!");
            }
        }
    }
}