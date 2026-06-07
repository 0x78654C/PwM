using System;
using System.Runtime.Versioning;
using System.Windows.Controls;
using GV = PwMLib.GlobalVariables;

namespace PwM.Utils
{
    [SupportedOSPlatform("Windows")]
    public static class Argon2SettingsManager
    {
        public const int DefaultIterations  = 40;
        public const int DefaultMemorySize  = 4096;
        public const int DefaultParallelism = 2;

        public const int MinIterations  = 10;
        public const int MaxIterations  = 200;
        public const int MinMemorySize  = 4096;
        public const int MaxMemorySize  = 1048576; // 1 GB
        public const int MinParallelism = 1;
        public const int MaxParallelism = 16;

        /// <summary>
        /// Loads all Argon2 parameters from the registry and populates the UI text boxes.
        /// Falls back to defaults if a key is missing or invalid.
        /// </summary>
        public static void LoadSettings(string registryPath,
                                        TextBox iterationsTxt,
                                        TextBox memorySizeTxt,
                                        TextBox parallelismTxt)
        {
            GV.argon2Iterations  = LoadInt(registryPath, GV.argon2IterationsReg,  DefaultIterations.ToString(),  DefaultIterations,  MinIterations,  MaxIterations);
            GV.argon2MemorySize  = LoadInt(registryPath, GV.argon2MemorySizeReg,  DefaultMemorySize.ToString(),  DefaultMemorySize,  MinMemorySize,  MaxMemorySize);
            GV.argon2Parallelism = LoadInt(registryPath, GV.argon2ParallelismReg, DefaultParallelism.ToString(), DefaultParallelism, MinParallelism, MaxParallelism);

            iterationsTxt.Text  = GV.argon2Iterations.ToString();
            memorySizeTxt.Text  = GV.argon2MemorySize.ToString();
            parallelismTxt.Text = GV.argon2Parallelism.ToString();
        }

        /// <summary>
        /// Validates and saves the three Argon2 parameters from the UI to the registry.
        /// Returns a user-facing error message, or null on success.
        /// </summary>
        public static string ValidateAndSave(string registryPath,
                                              string iterationsText,
                                              string memorySizeText,
                                              string parallelismText)
        {
            if (!TryParseInRange(iterationsText,  MinIterations,  MaxIterations,  out int iterations))
                return $"Iterations must be a number between {MinIterations} and {MaxIterations}.";
            if (!TryParseInRange(memorySizeText,  MinMemorySize,  MaxMemorySize,  out int memorySize))
                return $"Memory size must be between {MinMemorySize} KB and {MaxMemorySize} KB.";
            if (!TryParseInRange(parallelismText, MinParallelism, MaxParallelism, out int parallelism))
                return $"Parallelism must be a number between {MinParallelism} and {MaxParallelism}.";

            RegistryManagement.RegKey_WriteSubkey(registryPath, GV.argon2IterationsReg,  iterations.ToString());
            RegistryManagement.RegKey_WriteSubkey(registryPath, GV.argon2MemorySizeReg,  memorySize.ToString());
            RegistryManagement.RegKey_WriteSubkey(registryPath, GV.argon2ParallelismReg, parallelism.ToString());

            GV.argon2Iterations  = iterations;
            GV.argon2MemorySize  = memorySize;
            GV.argon2Parallelism = parallelism;

            return null;
        }

        private static int LoadInt(string registryPath, string key, string defaultValue,
                                   int defaultInt, int min, int max)
        {
            try
            {
                string value = RegistryManagement.RegKey_Read("HKEY_CURRENT_USER\\" + registryPath, key);
                if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int parsed) && parsed >= min && parsed <= max)
                    return parsed;
                RegistryManagement.RegKey_CreateKey(registryPath, key, defaultValue);
                return defaultInt;
            }
            catch
            {
                RegistryManagement.RegKey_CreateKey(registryPath, key, defaultValue);
                return defaultInt;
            }
        }

        private static bool TryParseInRange(string text, int min, int max, out int value)
        {
            return int.TryParse(text, out value) && value >= min && value <= max;
        }
    }
}
