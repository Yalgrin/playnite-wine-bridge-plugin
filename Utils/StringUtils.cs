using System;
using System.Text;
using System.Text.RegularExpressions;

namespace WineBridgePlugin.Utils
{
    public static class StringUtils
    {
        public static string Base64Encode(this string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string WindowsPathToLinuxPath(this string path)
        {
            if (Regex.IsMatch(path, "^[A-Za-z]:"))
            {
                path = $"drive_{char.ToLowerInvariant(path[0])}{path.Substring(2)}";
            }

            return path.Replace(@"\", "/");
        }
    }
}