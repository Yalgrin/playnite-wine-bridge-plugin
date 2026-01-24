using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Playnite.SDK;
using WineBridgePlugin.Settings;

namespace WineBridgePlugin.Utils
{
    public static class WineUtils
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        private static readonly Lazy<string> InnerScriptPathLinux = new Lazy<string>(GetScriptPathLinux);

        public static string ScriptPathLinux => InnerScriptPathLinux.Value;

        private static string GetScriptPathLinux()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (directoryName == null)
            {
                throw new Exception("Could not determine plugin directory.");
            }

            var scriptPath = Path.Combine(directoryName, @"Resources\run-in-linux.sh");
            return WindowsPathToLinux(scriptPath);
        }

        public static string WindowsPathToLinux(string windowsPath)
        {
            if (string.IsNullOrEmpty(windowsPath))
            {
                return null;
            }

            try
            {
                var debugLogging = WineBridgeSettings.DebugLoggingEnabled;
                if (debugLogging)
                {
                    Logger.Debug($"Executing: winepath -u \"{windowsPath}\"");
                }

                var process = new Process();
                process.StartInfo.FileName = "winepath";
                process.StartInfo.Arguments =
                    $"-u \"{windowsPath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                process.WaitForExit();

                var result = process.StandardOutput.ReadToEnd();
                while (result.EndsWith("\n"))
                {
                    result = result.Substring(0, result.Length - 1);
                }

                if (debugLogging)
                {
                    Logger.Debug($"Result: \"{result}\"");
                }

                return result;
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Failed to convert Windows path to Linux path: {windowsPath}");
                throw;
            }
        }

        public static string LinuxPathToWindows(string linuxPath)
        {
            if (string.IsNullOrEmpty(linuxPath))
            {
                return null;
            }

            try
            {
                var debugLogging = WineBridgeSettings.DebugLoggingEnabled;
                if (debugLogging)
                {
                    Logger.Debug($"Executing: winepath -w \"{linuxPath}\"");
                }

                var process = new Process();
                process.StartInfo.FileName = "winepath";
                process.StartInfo.Arguments =
                    $"-w \"{linuxPath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                process.WaitForExit();

                var result = process.StandardOutput.ReadToEnd();
                while (result.EndsWith("\n"))
                {
                    result = result.Substring(0, result.Length - 1);
                }

                if (debugLogging)
                {
                    Logger.Debug($"Result: \"{result}\"");
                }

                return result;
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Failed to convert Linux path to Windows path: {linuxPath}");
                throw;
            }
        }
    }
}