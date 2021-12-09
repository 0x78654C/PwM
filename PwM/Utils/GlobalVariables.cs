using System;

namespace PwM.Utils
{
    /* Global variables definitions class */
    public static class GlobalVariables
    {
        public static string vaultName { get; set; }
        public static string gridColor { get; set; }
        public static string messageData { get; set; }
        public static string decyrpt { get; set; }
        private static string s_rootPath = System.IO.Path.GetPathRoot(Environment.SystemDirectory);
        private static readonly string s_accountName = Environment.UserName;
        public static string passwordManagerDirectory = $"{s_rootPath}Users\\{s_accountName}\\AppData\\Local\\PwM\\";
    }
}
