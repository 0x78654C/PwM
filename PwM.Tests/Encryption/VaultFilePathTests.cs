namespace PwM.Tests.Encryption
{
    public class VaultFilePathTests
    {
        [Theory]
        [InlineData("../outside")]
        [InlineData("..\\outside")]
        [InlineData("C:\\outside")]
        [InlineData("vault/name")]
        [InlineData("CON")]
        [InlineData("name.")]
        public void Rejects_unsafe_vault_names(string name)
        {
            Assert.False(PwMLib.VaultFilePath.IsValidName(name));
            Assert.Throws<ArgumentException>(() => PwMLib.VaultFilePath.GetPath(Path.GetTempPath(), name));
        }

        [Fact]
        public void Resolves_valid_vault_as_an_immediate_child()
        {
            string root = Path.Combine(Path.GetTempPath(), "pwm-vaults");

            string path = PwMLib.VaultFilePath.GetPath(root, "personal-vault");

            Assert.Equal(Path.Combine(Path.GetFullPath(root), "personal-vault.x"), path);
        }
    }
}
