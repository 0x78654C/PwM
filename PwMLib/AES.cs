using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PwMLib
{
    public static class AES
    {
        private static readonly Encoding encoding = Encoding.UTF8;


        /// <summary>
        /// AES Encryption
        /// </summary>
        /// <param name="plainText">String input for encryption.</param>
        /// <param name="password">Master Password</param>>
        /// <returns>string</returns>
        public static string Encrypt(string plainText, string password)
        {
            var salt = new byte[16];
            RandomNumberGenerator.Fill(salt);

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;
            aes.Key = Argon2.Argon2HashPassword(password, salt);
            aes.GenerateIV();

            var AESEncrypt = aes.CreateEncryptor(aes.Key, aes.IV);
            var buffer = encoding.GetBytes(plainText);
            var encryptedText = Convert.ToBase64String(AESEncrypt.TransformFinalBlock(buffer, 0, buffer.Length));
            var ivBase64 = Convert.ToBase64String(aes.IV);
            var mac = BitConverter.ToString(HmacSHA256(ivBase64 + encryptedText, password)).Replace("-", "").ToLower();

            var keyValues = new Dictionary<string, object>
            {
                { "iv", ivBase64 },
                { "value", encryptedText },
                { "mac", mac },
                { "salt", Convert.ToBase64String(salt) },
            };
            Argon2.s_argon2.Reset();
            Argon2.s_argon2.Dispose();
            return Convert.ToBase64String(encoding.GetBytes(JsonSerializer.Serialize(keyValues)));
        }

        /// <summary>
        /// AES Decryption 
        /// </summary>
        /// <param name="plainText">String input for decryption</param>
        /// <param name="password">Master Password</param>
        /// <returns>string</returns>
        /// <exception cref="CryptographicException">
        /// Thrown when MAC verification fails (wrong password or tampered data) or decryption fails.
        /// </exception>
        public static string Decrypt(string plainText, string password)
        {
            var base64Decoded = Convert.FromBase64String(plainText);
            var base64DecodedStr = encoding.GetString(base64Decoded);
            var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(base64DecodedStr);

            var ivBase64 = payload["iv"];
            var cipherText = payload["value"];
            var storedMac = payload["mac"];

            // Verify MAC before decrypting to detect wrong password or tampered ciphertext
            var expectedMac = BitConverter.ToString(HmacSHA256(ivBase64 + cipherText, password)).Replace("-", "").ToLower();
            if (!CryptographicEquals(storedMac, expectedMac))
                throw new CryptographicException("MAC verification failed. Password may be incorrect or data has been tampered with.");

            // Use stored random salt when available; fall back to legacy derived salt for old vaults
            byte[] salt;
            if (payload.TryGetValue("salt", out string saltBase64))
                salt = Convert.FromBase64String(saltBase64);
            else
                salt = encoding.GetBytes(password.Substring(2, 10));

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;
            aes.Key = Argon2.Argon2HashPassword(password, salt);
            aes.IV = Convert.FromBase64String(ivBase64);

            var AESDecrypt = aes.CreateDecryptor(aes.Key, aes.IV);
            var buffer = Convert.FromBase64String(cipherText);
            Argon2.s_argon2.Reset();
            Argon2.s_argon2.Dispose();
            return encoding.GetString(AESDecrypt.TransformFinalBlock(buffer, 0, buffer.Length));
        }

        /// <summary>
        /// Hash computation with SHA256
        /// </summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static byte[] HmacSHA256(String data, String key)
        {
            using (var hmac = new HMACSHA256(encoding.GetBytes(key)))
            {
                return hmac.ComputeHash(encoding.GetBytes(data));
            }
        }

        /// <summary>
        /// Constant-time string comparison to prevent timing attacks on MAC verification.
        /// </summary>
        private static bool CryptographicEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            var diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
