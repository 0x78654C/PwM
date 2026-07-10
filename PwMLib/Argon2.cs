using Konscious.Security.Cryptography;
using System;
using System.Security.Cryptography;
using System.Text;

namespace PwMLib
{
    public static class Argon2
    {
        private const int MinimumSaltLength = 8;

        public static byte[] Argon2HashPassword(string password, byte[] salt)
        {
            return Argon2HashPassword(
                password,
                salt,
                GlobalVariables.argon2Parallelism,
                GlobalVariables.argon2Iterations,
                GlobalVariables.argon2MemorySize);
        }

        internal static byte[] Argon2HashPassword(
            string password,
            byte[] salt,
            int parallelism,
            int iterations,
            int memorySize)
        {
            ArgumentNullException.ThrowIfNull(password);
            ArgumentNullException.ThrowIfNull(salt);

            if (salt.Length < MinimumSaltLength)
                throw new ArgumentException($"Argon2 salt must be at least {MinimumSaltLength} bytes.", nameof(salt));
            if (parallelism is < 1 or > 16)
                throw new ArgumentOutOfRangeException(nameof(parallelism));
            if (iterations is < 1 or > 200)
                throw new ArgumentOutOfRangeException(nameof(iterations));
            if (memorySize is < 1024 or > 1_048_576)
                throw new ArgumentOutOfRangeException(nameof(memorySize));

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            try
            {
                using var argon2 = new Argon2id(passwordBytes)
                {
                    Salt = salt,
                    DegreeOfParallelism = parallelism,
                    Iterations = iterations,
                    MemorySize = memorySize
                };
                return argon2.GetBytes(32);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(passwordBytes);
            }
        }

        internal static byte[] LegacyHashPassword(
            string password,
            int parallelism,
            int iterations,
            int memorySize)
        {
            if (password is null || password.Length < 12)
                throw new CryptographicException("The vault password is invalid.");

            byte[] legacySalt = Encoding.UTF8.GetBytes(password.Substring(2, 10));
            try
            {
                return Argon2HashPassword(password, legacySalt, parallelism, iterations, memorySize);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(legacySalt);
            }
        }
    }
}
