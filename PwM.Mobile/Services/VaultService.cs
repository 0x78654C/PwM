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
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .Select(f => new VaultInfo
            {
                Name = Path.GetFileNameWithoutExtension(f.Name),
                FilePath = f.FullName,
                CreatedAt = f.CreationTime
            })
            .ToList();
    }

    public bool VaultExists(string vaultName) =>
        File.Exists(VaultPath(vaultName));

    public async Task<(bool success, string error)> ImportVaultAsync(FileResult file, bool overwrite)
    {
        EnsureDirectoryExists();

        var fileName = Path.GetFileName(file.FileName);
        if (!string.Equals(Path.GetExtension(fileName), ".x", StringComparison.OrdinalIgnoreCase))
            return (false, $"'{fileName}' is not a PwM vault file.");

        var vaultName = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(vaultName) || !IsValidVaultName(vaultName))
            return (false, $"'{fileName}' has an invalid vault name.");

        byte[] content;
        try
        {
            await using var source = await file.OpenReadAsync();
            using var buffer = new MemoryStream();
            await source.CopyToAsync(buffer);
            content = buffer.ToArray();
        }
        catch (Exception ex)
        {
            return (false, $"Could not read '{fileName}': {ex.Message}");
        }

        if (!LooksLikeVault(content))
            return (false, $"'{fileName}' is not a valid PwM vault file.");

        var destination = VaultPath(vaultName);
        if (File.Exists(destination) && !overwrite)
            return (false, $"Vault '{vaultName}' already exists.");

        try
        {
            await File.WriteAllBytesAsync(destination, content);
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, $"Could not import '{fileName}': {ex.Message}");
        }
    }

    public async Task<(bool success, string error, string exportPath)> PrepareVaultExportAsync(string vaultName)
    {
        var source = VaultPath(vaultName);
        if (!File.Exists(source))
            return (false, $"Vault '{vaultName}' not found.", string.Empty);

        try
        {
            var exportDirectory = Path.Combine(FileSystem.CacheDirectory, "vault-exports");
            Directory.CreateDirectory(exportDirectory);
            var exportPath = Path.Combine(exportDirectory, $"{vaultName}.x");
            await using var sourceStream = File.OpenRead(source);
            await using var destinationStream = File.Create(exportPath);
            await sourceStream.CopyToAsync(destinationStream);
            return (true, string.Empty, exportPath);
        }
        catch (Exception ex)
        {
            return (false, $"Could not export '{vaultName}': {ex.Message}", string.Empty);
        }
    }

    public (bool success, string error) CreateVault(string name, string password, string confirmPassword)
    {
        EnsureDirectoryExists();

        if (name.Length < 3)
            return (false, "Vault name must be at least 3 characters.");
        if (!IsValidVaultName(name))
            return (false, "Vault name contains unsupported characters.");

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

    private static bool IsValidVaultName(string name) =>
        name is not "." and not ".." &&
        name.IndexOfAny(['\\', '/', ':', '*', '?', '"', '<', '>', '|']) < 0;

    private static bool LooksLikeVault(byte[] content)
    {
        try
        {
            var decoded = Convert.FromBase64String(System.Text.Encoding.UTF8.GetString(content));
            var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(decoded);
            return payload != null &&
                   payload.ContainsKey("iv") &&
                   payload.ContainsKey("value") &&
                   payload.ContainsKey("mac");
        }
        catch
        {
            return false;
        }
    }

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
