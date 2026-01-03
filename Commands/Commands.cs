using System;
using System.Diagnostics;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace WineBridgePlugin.Commands
{
    public static class Commands
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        public static RelayCommand<object> NavigateUrlCommand =>
            new RelayCommand<object>(url =>
            {
                try
                {
                    NavigateUrl(url);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to open url.");
                }
            });

        private static void NavigateUrl(object url)
        {
            switch (url)
            {
                case string stringUrl:
                    NavigateUrl(stringUrl);
                    break;
                case Link linkUrl:
                    NavigateUrl(linkUrl.Url);
                    break;
                case Uri uriUrl:
                    NavigateUrl(uriUrl.OriginalString);
                    break;
                default:
                    throw new Exception("Unsupported URL format.");
            }
        }

        private static void NavigateUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new Exception("No URL was given.");
            }

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                url = "http://" + url;
            }

            Process.Start(url);
        }
    }
}