using WineBridgePlugin.Integrations.Heroic;

namespace WineBridgePlugin.Utils
{
    public static class HeroicPlatformExtensions
    {
        public static HeroicPlatform ToHeroicPlatform(this string runner)
        {
            switch (runner)
            {
                case "gog":
                    return HeroicPlatform.Gog;
                case "nile":
                    return HeroicPlatform.Amazon;
                case "legendary":
                    return HeroicPlatform.Epic;
                default:
                    return HeroicPlatform.Custom;
            }
        }

        public static string ToHeroicRunner(this HeroicPlatform platform)
        {
            switch (platform)
            {
                case HeroicPlatform.Gog:
                    return "gog";
                case HeroicPlatform.Amazon:
                    return "nile";
                case HeroicPlatform.Epic:
                    return "legendary";
                case HeroicPlatform.Custom:
                default:
                    return "sideload";
            }
        }
    }
}