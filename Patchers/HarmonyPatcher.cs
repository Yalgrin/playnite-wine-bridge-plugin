using System;
using HarmonyLib;
using Playnite.SDK;
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
                }
            };
        }
    }
}