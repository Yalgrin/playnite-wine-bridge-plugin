using System;
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
            BattleNetPatcher.Patch();
            ItchIoPatcher.Patch();
            EaPatcher.Patch();

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
                    case "BattleNetLibrary":
                        BattleNetPatcher.Patch();
                        break;
                    case "ItchioLibrary":
                        ItchIoPatcher.Patch();
                        break;
                    case "EaLibrary":
                        EaPatcher.Patch();
                        break;
                }
            };
        }

        public static void MakeScriptExecutable()
        {
            if (!WineDetector.IsRunningUnderWine())
            {
                Logger.Warn("Not running under Wine, skipping making script executable.");
                return;
            }

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
                LinuxProcessStarter.StartRawCommand($"chmod a+x '{WineUtils.ScriptPathLinux}'");
                LinuxProcessStarter.StartRawCommand($"chmod a+x '{WineUtils.OpenFileScriptPathLinux}'");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while making script executable!");
            }
        }
    }
}