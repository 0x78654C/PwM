using System.Text.Json;
using PwM.Mobile.Models;
using PwMLib;

namespace PwM.Mobile.Services;

/// <summary>
/// Handles all vault file I/O and credential encryption/decryption using PwMLib.
/// </summary>
public class VaultService
{
    private static string VaultDir => PwMLib.GlobalVariables.passwordManagerDirectory;

    public void EnsureDirectoryExists()
    {
        if (!Directory.Exists(VaultDir))
            Directory.CreateDirectory(VaultDir);
    }

    public IEnumerable<VaultInfo> ListVaults()
    {
        EnsureDirectoryExists();
        return new DirectoryInfo(VaultDir)
            .GetFiles("*.x")
            .Select(f => new VaultInfo
            {
                Name = Path.GetFileNameWithoutExtension(f.Name),
                FilePath = f.FullName,
                CreatedAt = f.CreationTime
            });
    }

    public (bool success, string error) CreateVault(string name, string password, string confirmPassword)
    {
        EnsureDirectoryExists();

        if (name.Length < 3)
            return (false, "Vault name must be at least 3 characters.");

        if (!PasswordValidator.ValidatePassword(confirmPassword))
            return (false, "Password must be ≥ 12 characters with upper, lower, digit, special, no spaces.");

        if (password != confirmPassword)
            return (false, "Passwords do not match.");

        var path = VaultPath(name);
        if (File.Exists(path))
            return (false, $"Vault '{name}' already exists.");

        var encrypted = AES.Encrypt(string.Empty, confirmPassword);
        File.WriteAllText(path, encrypted);
        return (true, string.Empty);
    }

    public (bool success, string error, List<CredentialEntry> entries) OpenVault(string name, string password)
    {
        var path = VaultPath(name);
        if (!File.Exists(path))
            return (false, $"Vault '{name}' not found.", []);

        string decrypted;
        try
        {
            decrypted = AES.Decrypt(File.ReadAllText(path), password);
        }
        catch
        {
            return (false, "Wrong master password or vault is corrupted.", []);
        }

        var entries = ParseEntries(decrypted);
        return (true, string.Empty, entries);
    }

    public (bool success, string error) AddCredential(string vaultName, string password,
        string application, string account, string entryPassword)
    {
        if (application.Length < 1)
            return (false, "Application name cannot be empty.");
        if (account.Length < 3)
            return (false, "Account name must be at least 3 characters.");

        var path = VaultPath(vaultName);
        var (ok, err, entries) = OpenVault(vaultName, password);
        if (!ok) return (false, err);

        if (entries.Any(e => e.Application == application && e.Account == account))
            return (false, $"'{application}' already has account '{account}'.");

        var newLine = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "site/application", application },
            { "account", account },
            { "password", entryPassword }
        });

        var currentContent = AES.Decrypt(File.ReadAllText(path), password);
        var updated = string.IsNullOrEmpty(currentContent)
            ? newLine
            : currentContent + "\n" + newLine;

        File.WriteAllText(path, AES.Encrypt(updated, password));
        return (true, string.Empty);
    }

    public (bool success, string error) DeleteCredential(string vaultName, string password,
        string application, string account)
    {
        var path = VaultPath(vaultName);
        var (ok, err, entries) = OpenVault(vaultName, password);
        if (!ok) return (false, err);

        var remaining = entries
            .Where(e => !(e.Application == application && e.Account == account))
            .ToList();

        if (remaining.Count == entries.Count)
            return (false, "Credential not found.");

        var newContent = string.Join("\n", remaining.Select(e => JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "site/application", e.Application },
            { "account", e.Account },
            { "password", e.Password }
        })));

        File.WriteAllText(path, AES.Encrypt(newContent, password));
        return (true, string.Empty);
    }

    public (bool success, string error) UpdatePassword(string vaultName, string masterPassword,
        string application, string account, string newEntryPassword)
    {
        var (ok, err, entries) = OpenVault(vaultName, masterPassword);
        if (!ok) return (false, err);

        var target = entries.FirstOrDefault(e => e.Application == application && e.Account == account);
        if (target == null) return (false, "Credential not found.");

        target.Password = newEntryPassword;

        var newContent = string.Join("\n", entries.Select(e => JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "site/application", e.Application },
            { "account", e.Account },
            { "password", e.Password }
        })));

        File.WriteAllText(VaultPath(vaultName), AES.Encrypt(newContent, masterPassword));
        return (true, string.Empty);
    }

    public (bool success, string error) ChangeMasterPassword(string vaultName,
        string oldPassword, string newPassword)
    {
        if (!PasswordValidator.ValidatePassword(newPassword))
            return (false, "New password must be ≥ 12 characters with upper, lower, digit, special, no spaces.");

        var path = VaultPath(vaultName);
        string decrypted;
        try { decrypted = AES.Decrypt(File.ReadAllText(path), oldPassword); }
        catch { return (false, "Old master password is incorrect."); }

        File.WriteAllText(path, AES.Encrypt(decrypted, newPassword));
        return (true, string.Empty);
    }

    public (bool success, string error) DeleteVault(string vaultName)
    {
        var path = VaultPath(vaultName);
        if (!File.Exists(path))
            return (false, $"Vault '{vaultName}' not found.");
        File.Delete(path);
        return (true, string.Empty);
    }

    private static string VaultPath(string name) =>
        Path.Combine(VaultDir, $"{name}.x");

    private static List<CredentialEntry> ParseEntries(string decryptedVault)
    {
        var entries = new List<CredentialEntry>();
        using var reader = new StringReader(decryptedVault);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(line);
                if (dict != null)
                    entries.Add(new Models.CredentialEntry
                    {
                        Application = dict.GetValueOrDefault("site/application", ""),
                        Account = dict.GetValueOrDefault("account", ""),
                        Password = dict.GetValueOrDefault("password", "")
                    });
            }
            catch { /* skip malformed lines */ }
        }
        return entries;
    }
}
