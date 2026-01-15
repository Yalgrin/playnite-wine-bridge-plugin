using System.Collections.Generic;

namespace WineBridgePlugin
{
    public static class Constants
    {
        public const string DummySteamExe = "DUMMY-STEAM-PATH.exe";
        public const string DummyHeroicExe = "DUMMY-HEROIC-PATH.exe";
        public const string DummyLutrisExe = "DUMMY-LUTRIS-PATH.exe";

        public const string WineBridgePrefix = "wine-bridge://";
        public const string WineBridgeAsyncPrefix = "wine-bridge-async://";
        public const string WineBridgeSteamPrefix = "wine-bridge-steam://";
        public const string WineBridgeHeroicPrefix = "wine-bridge-heroic://";
        public const string WineBridgeLutrisPrefix = "wine-bridge-lutris://";

        public const string HttpPrefix = "http://";
        public const string HttpsPrefix = "https://";
        public const string FilePrefix = "file://";

        public static readonly List<string> RedirectedProtocols = new List<string>
            { HttpPrefix, HttpsPrefix, FilePrefix };
    }
}