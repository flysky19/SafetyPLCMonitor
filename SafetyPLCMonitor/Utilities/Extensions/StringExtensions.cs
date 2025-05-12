// SafetyPLCMonitor/Utilities/Extensions/StringExtensions.cs
using System;

namespace SafetyPLCMonitor.Utilities.Extensions
{
    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
    }
}