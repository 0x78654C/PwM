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
            try
            {
                var aes = new RijndaelManaged
                {
                    KeySize = 256,
                    BlockSize = 128,
                    Padding = PaddingMode.PKCS7,
                    Mode = CipherMode.CBC,
                    Key = Argon2.Argon2HashPassword(password)
                };
                aes.GenerateIV();
                var AESEncrypt = aes.CreateEncryptor(aes.Key, aes.IV);
                var buffer = encoding.GetBytes(plainText);
                var encryptedText = Convert.ToBase64String(AESEncrypt.TransformFinalBlock(buffer, 0, buffer.Length));
                var mac = "";
                mac = BitConverter.ToString(HmacSHA256(Convert.ToBase64String(aes.IV) + encryptedText, password)).Replace("-", "").ToLower();
                var keyValues = new Dictionary<string, object>
                {
                    { "iv", Convert.ToBase64String(aes.IV) },
                    { "value", encryptedText },
                    { "mac", mac },
                };
                Argon2.s_argon2.Reset();
                Argon2.s_argon2.Dispose();
                return Convert.ToBase64String(encoding.GetBytes(JsonSerializer.Serialize(keyValues)));
            }
            catch (Exception e)
            {
                return "Error encrypting: " + e.Message;
            }
        }

        /// <summary>
        /// AES Decryption 
        /// </summary>
        /// <param name="plainText">String input for decryption</param>
        /// <param name="password">Master Password</param>
        /// <returns>string</returns>
        public static string Decrypt(string plainText, string password)
        {
            try
            {
                var aes = new RijndaelManaged
                {
                    KeySize = 256,
                    BlockSize = 128,
                    Padding = PaddingMode.PKCS7,
                    Mode = CipherMode.CBC,
                    Key = Argon2.Argon2HashPassword(password)
                };
                var base64Decoded = Convert.FromBase64String(plainText);
                var base64DecodedStr = encoding.GetString(base64Decoded);
                var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(base64DecodedStr);
                aes.IV = Convert.FromBase64String(payload["iv"]);
                var AESDecrypt = aes.CreateDecryptor(aes.Key, aes.IV);
                var buffer = Convert.FromBase64String(payload["value"]);
                Argon2.s_argon2.Reset();
                Argon2.s_argon2.Dispose();
                return encoding.GetString(AESDecrypt.TransformFinalBlock(buffer, 0, buffer.Length));
            }
            catch (Exception e)
            {
                return "Error decrypting: " + e.Message;
            }
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
    }
}
