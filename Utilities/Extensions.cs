using System;

namespace Utilities
{
    public static class Extensions
    {
        /// <summary>
        /// Returns an array of lines, derived by splitting the given <paramref name="string"/> with the current <see cref="Environment.NewLine"/>.
        /// </summary>
        public static string[] Lines(this string @string, StringSplitOptions stringSplitOptions = StringSplitOptions.None)
        {
            return @string.Split(new[] {Environment.NewLine}, stringSplitOptions);
        }
    }
}
