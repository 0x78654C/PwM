using System;
using System.IO;
using System.Linq;

namespace PwMLib
{
    /// <summary>
    /// Validates vault names and resolves them to an immediate child of a vault directory.
    /// </summary>
    public static class VaultFilePath
    {
        private static readonly string[] WindowsReservedNames =
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        public static bool IsValidName(string vaultName)
        {
            if (string.IsNullOrWhiteSpace(vaultName)
                || vaultName.Length > 128
                || vaultName is "." or ".."
                || vaultName != vaultName.Trim()
                || vaultName.EndsWith('.')
                || vaultName.Any(char.IsControl)
                || vaultName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || vaultName.IndexOfAny(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' }) >= 0)
            {
                return false;
            }

            string stem = vaultName.Split('.')[0];
            return !WindowsReservedNames.Contains(stem, StringComparer.OrdinalIgnoreCase);
        }

        public static string GetPath(string vaultDirectory, string vaultName)
        {
            if (string.IsNullOrWhiteSpace(vaultDirectory))
                throw new ArgumentException("Vault directory is required.", nameof(vaultDirectory));
            if (!IsValidName(vaultName))
                throw new ArgumentException("Vault name contains unsupported characters.", nameof(vaultName));

            string root = Path.GetFullPath(vaultDirectory);
            string path = Path.GetFullPath(Path.Combine(root, vaultName + ".x"));
            string relativePath = Path.GetRelativePath(root, path);
            if (Path.IsPathRooted(relativePath)
                || relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                || relativePath.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal)
                || relativePath == "..")
            {
                throw new ArgumentException("Vault path escapes the vault directory.", nameof(vaultName));
            }

            return path;
        }
    }
}
