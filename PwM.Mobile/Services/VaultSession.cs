using PwM.Mobile.Models;

namespace PwM.Mobile.Services;

public sealed class VaultSession
{
    private List<CredentialEntry>? _prefetchedCredentials;
    public event EventHandler? Locked;
    public long Version { get; private set; }

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
        Lock();
        VaultName = vaultName;
        MasterPassword = masterPassword;
        _prefetchedCredentials = prefetchedCredentials?.ToList();
    }

    public bool IsCurrent(long version) => IsUnlocked && Version == version;

    public bool TryUnlock(long version, string vaultName, string masterPassword,
        IEnumerable<CredentialEntry>? credentials = null)
    {
        if (Version != version)
            return false;

        Unlock(vaultName, masterPassword, credentials);
        return true;
    }

    public bool TryUpdateMasterPassword(long version, string password)
    {
        if (!IsCurrent(version))
            return false;

        MasterPassword = password;
        return true;
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
        Version++;
        VaultName = string.Empty;
        MasterPassword = string.Empty;
        if (_prefetchedCredentials is not null)
            foreach (var entry in _prefetchedCredentials)
                entry.Password = string.Empty;
        _prefetchedCredentials = null;
        Locked?.Invoke(this, EventArgs.Empty);
    }
}
