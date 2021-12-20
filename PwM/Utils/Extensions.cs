using System;

namespace PwM.Utils
{
    internal static class Extensions
    {
        /// <summary>
        /// Split string by a specific text.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="parameter"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        internal static string SplitByText(this string input, string parameter, int index)
        {
            string[] output = input.Split(new string[] { parameter }, StringSplitOptions.None);
            return output[index];
        }
    }
}
