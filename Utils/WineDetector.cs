using System;
using System.Runtime.InteropServices;
using Playnite.SDK;

namespace WineBridgePlugin.Utils
{
    public static class WineDetector
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public static bool IsRunningUnderWine()
        {
            try
            {
                var hModule = GetModuleHandle("ntdll.dll");
                if (hModule == IntPtr.Zero)
                {
                    return false;
                }

                return GetProcAddress(hModule, "wine_get_version") != IntPtr.Zero;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred while checking if the app is running under Wine!");
                return false;
            }
        }
    }
}