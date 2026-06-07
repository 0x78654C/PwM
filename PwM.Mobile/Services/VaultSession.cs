namespace PwM.Mobile.Services;

public sealed class VaultSession
{
    public string VaultName { get; private set; } = string.Empty;
    public string MasterPassword { get; private set; } = string.Empty;

    public bool IsUnlocked =>
        !string.IsNullOrEmpty(VaultName) &&
        !string.IsNullOrEmpty(MasterPassword);

    public void Unlock(string vaultName, string masterPassword)
    {
        VaultName = vaultName;
        MasterPassword = masterPassword;
    }

    public void Lock()
    {
        VaultName = string.Empty;
        MasterPassword = string.Empty;
    }
}
