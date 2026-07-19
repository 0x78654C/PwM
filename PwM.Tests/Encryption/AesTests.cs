using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PwM.Tests.Encryption
{
    public class AesTests
    {
        private const string Password = "CorrectHorse1!Battery";

        [Fact]
        public void Encrypt_uses_versioned_authenticated_format_and_round_trips()
        {
            const string plaintext = "sensitive vault data";

            string encrypted = PwMLib.AES.Encrypt(plaintext, Password);
            var payload = DecodePayload(encrypted);

            Assert.Equal("2", payload["version"]);
            Assert.Equal("argon2id", payload["kdf"]);
            Assert.Equal(plaintext, PwMLib.AES.Decrypt(encrypted, Password));
        }

        [Fact]
        public void Encrypt_uses_a_unique_salt_and_nonce()
        {
            var first = DecodePayload(PwMLib.AES.Encrypt("same", Password));
            var second = DecodePayload(PwMLib.AES.Encrypt("same", Password));

            Assert.NotEqual(first["salt"], second["salt"]);
            Assert.NotEqual(first["nonce"], second["nonce"]);
            Assert.NotEqual(first["value"], second["value"]);
        }

        [Fact]
        public void Decrypt_rejects_modified_metadata_or_ciphertext()
        {
            var payload = DecodePayload(PwMLib.AES.Encrypt("secret", Password));
            payload["iterations"] = "41";
            string modified = EncodePayload(payload);

            Assert.ThrowsAny<CryptographicException>(() => PwMLib.AES.Decrypt(modified, Password));
        }

        [Fact]
        public void Decrypt_reads_authenticated_legacy_vaults()
        {
            string legacy = EncryptLegacy("legacy data", Password);

            Assert.Equal("legacy data", PwMLib.AES.Decrypt(legacy, Password));
        }

        private static Dictionary<string, string> DecodePayload(string encrypted)
        {
            string json = Encoding.UTF8.GetString(Convert.FromBase64String(encrypted));
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;
        }

        private static string EncodePayload(Dictionary<string, string> payload) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));

        private static string EncryptLegacy(string plaintext, string password)
        {
            byte[] salt = Encoding.UTF8.GetBytes(password.Substring(2, 10));
            byte[] key = PwMLib.Argon2.Argon2HashPassword(password, salt);
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;
            aes.Key = key;
            aes.GenerateIV();

            using ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            string ciphertext = Convert.ToBase64String(
                encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length));
            string iv = Convert.ToBase64String(aes.IV);
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(password));
            string mac = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(iv + ciphertext))).ToLowerInvariant();

            return EncodePayload(new Dictionary<string, string>
            {
                ["iv"] = iv,
                ["value"] = ciphertext,
                ["mac"] = mac
            });
        }
    }
}
