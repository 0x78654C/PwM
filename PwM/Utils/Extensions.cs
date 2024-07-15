using System;
using System.IO;

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

        /// <summary>
        /// Check if file is locked.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        internal static bool IsLocked(this FileInfo f)
        {
            try
            {
                string fpath = f.FullName;
                FileStream fs = File.OpenWrite(fpath);
                fs.Close();
                return false;
            }
            catch (Exception) { return true; }
        }

    }
}
