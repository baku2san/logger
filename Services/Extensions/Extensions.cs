using System;
using System.Linq;

namespace loggerApp.Extensions
{
    static class Extensions
    {
        public static string[] ToStrings<T>(this T[] objectArray)
        {
            return Array.ConvertAll<T, string>(objectArray, o => o.ToString());
        }

        public static string Join(this string[] stringArray, string separator = ",")
        {
            return string.Join(separator, stringArray);
        }

        public static bool SequenceEqualWithNull(this byte[] original, Object target)
        {
            var targetBytes = target as byte[];
            return (targetBytes == null)? false : original.SequenceEqual(targetBytes);
        }
    }
}
