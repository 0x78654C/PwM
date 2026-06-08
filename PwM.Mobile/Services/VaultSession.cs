using PwM.Mobile.Models;

namespace PwM.Mobile.Services;

public sealed class VaultSession
{
    private List<CredentialEntry>? _prefetchedCredentials;

    public string VaultName { get; private set; } = string.Empty;
    public string MasterPassword { get; private set; } = string.Empty;

    public bool IsUnlocked =>
        !string.IsNullOrEmpty(VaultName) &&
        !string.IsNullOrEmpty(MasterPassword);

    public void Unlock(
        string vaultName,
        string masterPassword,
        IEnumerable<CredentialEntry>? prefetchedCredentials = null)
    {
        VaultName = vaultName;
        MasterPassword = masterPassword;
        _prefetchedCredentials = prefetchedCredentials?.ToList();
    }

    public List<CredentialEntry>? TakePrefetchedCredentials(string vaultName)
    {
        if (!IsUnlocked ||
            !string.Equals(VaultName, vaultName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var credentials = _prefetchedCredentials;
        _prefetchedCredentials = null;
        return credentials;
    }

    public void Lock()
    {
        VaultName = string.Empty;
        MasterPassword = string.Empty;
        _prefetchedCredentials = null;
    }
}
