using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PwMLib
{
    public static class AES
    {
        private const string CurrentVersion = "2";
        private const int SaltSize = 16;
        private const int NonceSize = 12;
        private const int TagSize = 16;
        private const int MaximumEncodedVaultLength = VaultFile.MaximumEncodedLength;
        private static readonly UTF8Encoding Encoding = new(false, true);

        /// <summary>
        /// Encrypts a vault using Argon2id and AES-256-GCM.
        /// </summary>
        public static string Encrypt(string plainText, string password)
        {
            ArgumentNullException.ThrowIfNull(plainText);
            ArgumentNullException.ThrowIfNull(password);

            int iterations = GlobalVariables.argon2Iterations;
            int memorySize = GlobalVariables.argon2MemorySize;
            int parallelism = GlobalVariables.argon2Parallelism;

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
            byte[] plaintextBytes = Encoding.GetBytes(plainText);
            byte[] ciphertext = new byte[plaintextBytes.Length];
            byte[] tag = new byte[TagSize];
            byte[] key = Argon2.Argon2HashPassword(password, salt, parallelism, iterations, memorySize);

            string saltBase64 = Convert.ToBase64String(salt);
            byte[] associatedData = BuildAssociatedData(saltBase64, iterations, memorySize, parallelism);
            try
            {
                using var aes = new AesGcm(key, TagSize);
                aes.Encrypt(nonce, plaintextBytes, ciphertext, tag, associatedData);

                var payload = new Dictionary<string, string>
                {
                    ["version"] = CurrentVersion,
                    ["kdf"] = "argon2id",
                    ["iterations"] = iterations.ToString(CultureInfo.InvariantCulture),
                    ["memorySize"] = memorySize.ToString(CultureInfo.InvariantCulture),
                    ["parallelism"] = parallelism.ToString(CultureInfo.InvariantCulture),
                    ["salt"] = saltBase64,
                    ["nonce"] = Convert.ToBase64String(nonce),
                    ["value"] = Convert.ToBase64String(ciphertext),
                    ["tag"] = Convert.ToBase64String(tag)
                };

                return Convert.ToBase64String(Encoding.GetBytes(JsonSerializer.Serialize(payload)));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
                CryptographicOperations.ZeroMemory(plaintextBytes);
                CryptographicOperations.ZeroMemory(associatedData);
            }
        }

        /// <summary>
        /// Decrypts current AES-GCM vaults and authenticated legacy AES-CBC vaults.
        /// </summary>
        /// <exception cref="CryptographicException">
        /// Thrown for a wrong password, malformed payload, or modified vault.
        /// </exception>
        public static string Decrypt(string encryptedVault, string password)
        {
            ArgumentNullException.ThrowIfNull(encryptedVault);
            ArgumentNullException.ThrowIfNull(password);

            if (encryptedVault.Length == 0 || encryptedVault.Length > MaximumEncodedVaultLength)
                throw new CryptographicException("The vault payload is invalid.");

            try
            {
                byte[] decoded = Convert.FromBase64String(encryptedVault);
                Dictionary<string, string> payload = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    Encoding.GetString(decoded));
                if (payload is null)
                    throw new CryptographicException("The vault payload is invalid.");

                return payload.TryGetValue("version", out string version)
                    ? version == CurrentVersion
                        ? DecryptCurrent(payload, password)
                        : throw new CryptographicException("The vault format is not supported.")
                    : DecryptLegacy(payload, password);
            }
            catch (CryptographicException)
            {
                throw;
            }
            catch (Exception exception) when (exception is FormatException
                or JsonException
                or DecoderFallbackException
                or KeyNotFoundException
                or ArgumentException)
            {
                throw new CryptographicException("The vault payload is invalid or has been modified.", exception);
            }
        }

        private static string DecryptCurrent(Dictionary<string, string> payload, string password)
        {
            if (GetRequired(payload, "kdf") != "argon2id")
                throw new CryptographicException("The vault key derivation function is not supported.");

            int iterations = ParseParameter(payload, "iterations", 1, 200);
            int memorySize = ParseParameter(payload, "memorySize", 1024, 1_048_576);
            int parallelism = ParseParameter(payload, "parallelism", 1, 16);
            string saltBase64 = GetRequired(payload, "salt");
            byte[] salt = DecodeRequired(payload, "salt", SaltSize);
            byte[] nonce = DecodeRequired(payload, "nonce", NonceSize);
            byte[] tag = DecodeRequired(payload, "tag", TagSize);
            byte[] ciphertext = Convert.FromBase64String(GetRequired(payload, "value"));
            byte[] plaintext = new byte[ciphertext.Length];
            byte[] key = Argon2.Argon2HashPassword(password, salt, parallelism, iterations, memorySize);
            byte[] associatedData = BuildAssociatedData(saltBase64, iterations, memorySize, parallelism);

            try
            {
                using var aes = new AesGcm(key, TagSize);
                aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
                return Encoding.GetString(plaintext);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
                CryptographicOperations.ZeroMemory(plaintext);
                CryptographicOperations.ZeroMemory(associatedData);
            }
        }

        private static string DecryptLegacy(Dictionary<string, string> payload, string password)
        {
            string ivBase64 = GetRequired(payload, "iv");
            string cipherTextBase64 = GetRequired(payload, "value");
            string storedMac = GetRequired(payload, "mac");
            string expectedMac = Convert.ToHexString(HmacSha256(ivBase64 + cipherTextBase64, password)).ToLowerInvariant();

            byte[] storedMacBytes = Encoding.GetBytes(storedMac);
            byte[] expectedMacBytes = Encoding.GetBytes(expectedMac);
            bool validMac = storedMacBytes.Length == expectedMacBytes.Length
                && CryptographicOperations.FixedTimeEquals(storedMacBytes, expectedMacBytes);
            CryptographicOperations.ZeroMemory(storedMacBytes);
            CryptographicOperations.ZeroMemory(expectedMacBytes);
            if (!validMac)
                throw new CryptographicException("The password is incorrect or the vault has been modified.");

            byte[] iv = Convert.FromBase64String(ivBase64);
            if (iv.Length != 16)
                throw new CryptographicException("The vault payload is invalid.");
            byte[] ciphertext = Convert.FromBase64String(cipherTextBase64);

            try
            {
                return DecryptLegacyWithParameters(
                    ciphertext,
                    iv,
                    password,
                    GlobalVariables.argon2Parallelism,
                    GlobalVariables.argon2Iterations,
                    GlobalVariables.argon2MemorySize);
            }
            catch (Exception exception) when (
                exception is CryptographicException or DecoderFallbackException
                && (GlobalVariables.argon2Parallelism != 2
                || GlobalVariables.argon2Iterations != 40
                || GlobalVariables.argon2MemorySize != 4096))
            {
                // Vaults written before configurable KDF settings always used these defaults.
                return DecryptLegacyWithParameters(ciphertext, iv, password, 2, 40, 4096);
            }
        }

        private static string DecryptLegacyWithParameters(
            byte[] ciphertext,
            byte[] iv,
            string password,
            int parallelism,
            int iterations,
            int memorySize)
        {
            byte[] key = Argon2.LegacyHashPassword(password, parallelism, iterations, memorySize);
            try
            {
                using var aes = Aes.Create();
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;
                aes.Key = key;
                aes.IV = iv;

                using ICryptoTransform decryptor = aes.CreateDecryptor();
                byte[] plaintext = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                try
                {
                    return Encoding.GetString(plaintext);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(plaintext);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }

        private static byte[] BuildAssociatedData(
            string saltBase64,
            int iterations,
            int memorySize,
            int parallelism)
        {
            return Encoding.GetBytes(string.Join('|',
                CurrentVersion,
                "argon2id",
                iterations.ToString(CultureInfo.InvariantCulture),
                memorySize.ToString(CultureInfo.InvariantCulture),
                parallelism.ToString(CultureInfo.InvariantCulture),
                saltBase64));
        }

        private static int ParseParameter(
            Dictionary<string, string> payload,
            string name,
            int minimum,
            int maximum)
        {
            if (!int.TryParse(GetRequired(payload, name), NumberStyles.None, CultureInfo.InvariantCulture, out int value)
                || value < minimum
                || value > maximum)
            {
                throw new CryptographicException("The vault KDF parameters are invalid.");
            }

            return value;
        }

        private static byte[] DecodeRequired(Dictionary<string, string> payload, string name, int expectedLength)
        {
            byte[] value = Convert.FromBase64String(GetRequired(payload, name));
            if (value.Length != expectedLength)
                throw new CryptographicException("The vault payload is invalid.");
            return value;
        }

        private static string GetRequired(Dictionary<string, string> payload, string name)
        {
            if (!payload.TryGetValue(name, out string value) || string.IsNullOrEmpty(value))
                throw new CryptographicException("The vault payload is invalid.");
            return value;
        }

        private static byte[] HmacSha256(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.GetBytes(key));
            return hmac.ComputeHash(Encoding.GetBytes(data));
        }
    }
}
