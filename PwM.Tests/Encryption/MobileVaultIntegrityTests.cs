using PwM.Mobile.Services;
using PwMLib;

namespace PwM.Tests.Encryption;

[CollectionDefinition("Vault storage", DisableParallelization = true)]
public class VaultStorageCollection { }

[Collection("Vault storage")]
public class MobileVaultIntegrityTests
{
    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"site/application\":\"site\",\"account\":\"account\",\"password\":null}")]
    public void Malformed_records_prevent_rewrites_instead_of_silently_losing_credentials(string invalidRecord)
    {
        const string password = "CorrectHorse1!Battery";
        string directory = Path.Combine(Path.GetTempPath(), "PwM-security-" + Guid.NewGuid().ToString("N"));
        string originalDirectory = GlobalVariables.passwordManagerDirectory;
        string path = VaultFilePath.GetPath(directory, "test");
        Directory.CreateDirectory(directory);
        try
        {
            GlobalVariables.passwordManagerDirectory = directory;
            string encrypted = PwMLib.AES.Encrypt(
                "{\"site/application\":\"site\",\"account\":\"account\",\"password\":\"secret\"}\n" + invalidRecord,
                password);
            File.WriteAllText(path, encrypted);
            var service = new VaultService();

            Assert.False(service.OpenVault("test", password).success);
            Assert.False(service.UpdatePassword("test", password, "site", "account", "replacement").success);
            Assert.False(service.DeleteCredential("test", password, "site", "account").success);
            Assert.Equal(encrypted, File.ReadAllText(path));
        }
        finally
        {
            GlobalVariables.passwordManagerDirectory = originalDirectory;
            File.Delete(path);
            Directory.Delete(directory);
        }
    }
}
