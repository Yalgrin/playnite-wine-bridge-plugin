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

        public static string EscapeRegex(this string input)
        {
            return Regex.Escape(input);
        }
    }
}