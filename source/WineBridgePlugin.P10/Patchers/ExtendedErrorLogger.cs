using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Playnite.SDK;
using WineBridgePlugin.Settings;

namespace WineBridgePlugin.Patchers
{
    public static class ExtendedErrorLogger
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        public static void Initialize()
        {
            AppDomain.CurrentDomain.FirstChanceException += (sender, args) => { LogException(args); };
        }

        private static void LogException(FirstChanceExceptionEventArgs args)
        {
            if (!WineBridgeSettings.ExtendedErrorLoggingEnabled)
            {
                return;
            }

            var ex = args.Exception;
            Logger.Error(ex, "First chance exception!");
            var targetSite = ex.TargetSite;
            var currentStack = new StackTrace(1, true);

            Logger.Error($"Exception: {ex.GetType().FullName}");
            Logger.Error($"Message: {ex.Message}");
            Logger.Error($"TargetSite: {targetSite?.DeclaringType?.FullName}.{targetSite?.Name}");
            Logger.Error("Current stack:");
            Logger.Error(currentStack.ToString());
            Logger.Error(new string('-', 80));
        }
    }
}