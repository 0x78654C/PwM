using System;
using System.Collections.Generic;
using System.IO;
using System.Security;

namespace PwMLib
{
    /* Global variables definitions class */
    public static class GlobalVariables
    {
        public static SecureString masterPassword { get; set; }
        public static SecureString newMasterPassword { get; set; }
        public static string applicationName { get; set; }
        public static string accountName { get; set; }
        public static string accountPassword { get; set; }
        public static string newAccountPassword { get; set; }
        public static bool deleteConfirmation = false;
        public static bool createConfirmation = false;
        public static bool importConfirmation = false;
        public static bool updatePwdConfirmation = false;
        public static bool closeAppConfirmation = false;
        public static string vaultName { get; set; }
        public static int vaultsCount { get; set; }
        public static string gridColor { get; set; }
        public static string messageData { get; set; }
        public static bool vaultChecks = false;
        public static bool vaultOpen = false;
        public static bool sharedVault = false;
        public static bool masterPasswordCheck = true;
        public static string passwordManagerDirectory { get; set; } = GetDefaultPasswordManagerDirectory();
        public static readonly string registryPath = "SOFTWARE\\PwM";
        public static string jsonSharedVaults => Path.Combine(passwordManagerDirectory, "PwM.Json");
        public static readonly string vaultExpireReg = "VaultExpireSession";
        public static string lockedUser => Path.Combine(passwordManagerDirectory, "lockedUser");

        private static string GetDefaultPasswordManagerDirectory()
        {
            if (OperatingSystem.IsWindows())
            {
                var rootPath = Path.GetPathRoot(Environment.SystemDirectory);
                var accountName = Environment.UserName;
                return $"{rootPath}Users\\{accountName}\\AppData\\Local\\PwM\\";
            }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PwM");
        }
        public static int vaultExpireInterval { get; set; }
        public const string apiHIBP = "https://api.pwnedpasswords.com/range/";
        public const string apiHIBPMain = "api.pwnedpasswords.com";
        public static List<string> listItems = new List<string>();

        // Argon2 hashing parameters (defaults match the original hardcoded values)
        public static readonly string argon2IterationsReg = "Argon2Iterations";
        public static readonly string argon2MemorySizeReg = "Argon2MemorySize";
        public static readonly string argon2ParallelismReg = "Argon2Parallelism";
        public static int argon2Iterations { get; set; } = 40;
        public static int argon2MemorySize { get; set; } = 4096;
        public static int argon2Parallelism { get; set; } = 2;
    }
}
