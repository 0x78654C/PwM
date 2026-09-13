using PwMLib;

namespace PwM.Tests.Encryption;

public class VaultFileTests
{
    [Fact]
    public async Task Oversized_import_is_rejected_before_reading_or_allocating_its_content()
    {
        using var stream = new OversizedStream();
        await Assert.ThrowsAsync<InvalidDataException>(() => VaultFile.ReadBytesAsync(stream));
        Assert.False(stream.WasRead);
    }

    [Fact]
    public async Task Normal_vault_import_preserves_the_encrypted_bytes()
    {
        var encrypted = Encoding.UTF8.GetBytes(PwMLib.AES.Encrypt("credentials", "StrongMasterPassword1!"));
        using var stream = new MemoryStream(encrypted);
        Assert.Equal(encrypted, await VaultFile.ReadBytesAsync(stream));
    }

    private sealed class OversizedStream : MemoryStream
    {
        public bool WasRead { get; private set; }
        public override long Length => (long)VaultFile.MaximumEncodedLength + 1;
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
        {
            WasRead = true;
            throw new InvalidOperationException("Oversized content must not be read.");
        }
    }
}
