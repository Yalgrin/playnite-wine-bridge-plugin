using System;

namespace WineBridgePlugin.Integrations.Lutris
{
    public static class LutrisUtils
    {
        public static string GetLutrisService(LutrisPlatform platform)
        {
            switch (platform)
            {
                case LutrisPlatform.Gog:
                    return "gog";
                case LutrisPlatform.Epic:
                    return "egs";
                case LutrisPlatform.Amazon:
                    return "amazon";
                case LutrisPlatform.EaApp:
                    return "ea_app";
                case LutrisPlatform.BattleNet:
                    return "battlenet";
                case LutrisPlatform.ItchIo:
                    return "itchio";
                default:
                    throw new ArgumentOutOfRangeException(nameof(platform), platform, "Unsupported platform.");
            }
        }
    }
}