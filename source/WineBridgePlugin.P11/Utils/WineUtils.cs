using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Playnite;
using WineBridgePlugin.Settings;

namespace WineBridgePlugin.Utils
{
    public static class WineUtils
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        private static readonly Lazy<string> InnerScriptPathLinux = new(() => GetScriptPathLinux("run-in-linux.sh"));

        private static readonly Lazy<string> InnerOpenFilePickerScriptLinux =
            new(() => GetScriptPathLinux("open-file-picker.sh"));

        private static readonly ConcurrentDictionary<string, string> LinuxPathToWindowsCache = new();

        public static List<string> FileDirectorySelectorPrograms =>
        [
            "auto",
            "kdialog",
            "zenity",
            "yad"
        ];

        public static string ScriptPathLinux => InnerScriptPathLinux.Value;
        public static string OpenFileScriptPathLinux => InnerOpenFilePickerScriptLinux.Value;

        private static string GetScriptPathLinux(string scriptName)
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (directoryName == null)
            {
                throw new Exception("Could not determine plugin directory.");
            }

            var scriptPath = Path.Combine(directoryName, $@"Resources\{scriptName}");
            var linuxScriptPath = WindowsPathToLinux(scriptPath);
            if (linuxScriptPath == null)
            {
                throw new Exception($"Could not convert script path to Linux path: {scriptPath}");
            }

            return linuxScriptPath;
        }

        public static string? WindowsPathToLinux(string? windowsPath)
        {
            if (string.IsNullOrEmpty(windowsPath))
            {
                return null;
            }

            try
            {
                var debugLogging = WineBridgeSettings.DebugLoggingEnabled;

                while (windowsPath.EndsWith('\\'))
                {
                    windowsPath = windowsPath[..^1];
                }

                if (debugLogging)
                {
                    Logger.Debug($"Executing: winepath -u \"{windowsPath}\"");
                }

                var process = new Process();
                process.StartInfo.FileName = "winepath";
                process.StartInfo.Arguments = $"-u \"{windowsPath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                process.WaitForExit();

                var result = process.StandardOutput.ReadToEnd();
                while (result.EndsWith('\n'))
                {
                    result = result[..^1];
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

        public static string? LinuxPathToWindows(string? linuxPath)
        {
            if (string.IsNullOrEmpty(linuxPath))
            {
                return null;
            }

            try
            {
                if (linuxPath.StartsWith('\\') && !linuxPath.StartsWith(@"\\"))
                {
                    linuxPath = linuxPath.Replace('\\', '/');
                }

                var debugLogging = WineBridgeSettings.DebugLoggingEnabled;

                if (LinuxPathToWindowsCache.TryGetValue(linuxPath, out var cachedResult))
                {
                    if (debugLogging)
                    {
                        Logger.Debug($"Cache hit for Linux path: \"{linuxPath}\"");
                    }

                    return cachedResult;
                }

                if (debugLogging)
                {
                    Logger.Debug($"Executing: winepath -w \"{linuxPath}\"");
                }

                var process = new Process();
                process.StartInfo.FileName = "winepath";
                process.StartInfo.Arguments = $"-w \"{linuxPath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                process.WaitForExit();

                var result = process.StandardOutput.ReadToEnd();
                while (result.EndsWith('\n'))
                {
                    result = result[..^1];
                }

                LinuxPathToWindowsCache[linuxPath] = result;

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