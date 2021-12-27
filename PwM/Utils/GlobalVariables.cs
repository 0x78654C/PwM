using System;
using System.Security;

namespace PwM.Utils
{
    /* Global variables definitions class */
    public static class GlobalVariables
    {
        public static SecureString masterPassword { get; set; }
        public static string applicationName { get; set; }
        public static string accountName { get; set; }
        public static string accountPassword { get; set; }
        public static string deleteConfirmation { get; set; }
        public static string createConfirmation { get; set; }
        public static string importConfirmation { get; set; }
        public static string closeAppConfirmation { get; set; }
        public static string vaultName { get; set; }
        public static int vaultsCount { get; set; }
        public static string gridColor { get; set; }
        public static string messageData { get; set; }
        public static bool vaultChecks = false;
        public static bool masterPasswordCheck = true;
        private static string s_rootPath = System.IO.Path.GetPathRoot(Environment.SystemDirectory);
        private static readonly string s_accountName = Environment.UserName;
        public static readonly string passwordManagerDirectory = $"{s_rootPath}Users\\{s_accountName}\\AppData\\Local\\PwM\\";
    }
}
