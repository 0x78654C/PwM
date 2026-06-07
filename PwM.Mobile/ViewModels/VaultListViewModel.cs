using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PwM.Mobile.Models;
using PwM.Mobile.Services;

namespace PwM.Mobile.ViewModels;

public partial class VaultListViewModel : BaseViewModel
{
    private readonly VaultService _vaultService;
    private readonly VaultSession _vaultSession;
    private readonly PasswordPromptService _passwordPromptService;
    private readonly List<VaultInfo> _allVaults = [];

    public ObservableCollection<VaultInfo> Vaults { get; } = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _newVaultName = string.Empty;

    [ObservableProperty]
    private string _newMasterPassword = string.Empty;

    [ObservableProperty]
    private string _confirmMasterPassword = string.Empty;

    public VaultListViewModel(
        VaultService vaultService,
        VaultSession vaultSession,
        PasswordPromptService passwordPromptService)
    {
        _vaultService = vaultService;
        _vaultSession = vaultSession;
        _passwordPromptService = passwordPromptService;
        Title = "Vaults";
    }

    [RelayCommand]
    public void LoadVaults()
    {
        Vaults.Clear();
        _allVaults.Clear();
        try
        {
            foreach (var vault in _vaultService.ListVaults())
            {
                vault.IsOpen = _vaultSession.IsUnlocked &&
                               string.Equals(
                                   vault.Name,
                                   _vaultSession.VaultName,
                                   StringComparison.OrdinalIgnoreCase);
                _allVaults.Add(vault);
            }
            ApplyVaultFilter();
        }
        catch (Exception ex)
        {
            _ = Shell.Current.DisplayAlertAsync("Error", $"Could not list vaults: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task ExportVaultAsync(VaultInfo? vault)
    {
        if (!await EnsureVaultAsync(vault))
            return;

        IsBusy = true;
        try
        {
            var (ok, error, exportPath) = await _vaultService.PrepareVaultExportAsync(vault!.Name);
            if (!ok)
            {
                await Shell.Current.DisplayAlertAsync("Export Failed", error, "OK");
                return;
            }

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"Export {vault.Name}",
                File = new ShareFile(exportPath)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Export Failed", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ChangeMasterPasswordAsync(VaultInfo? vault)
    {
        if (!await EnsureVaultAsync(vault))
            return;

        string? oldPassword = await _passwordPromptService.ShowAsync(
            "Change Master Password",
            $"Enter the current master password for '{vault!.Name}':",
            "Current master password");
        if (string.IsNullOrEmpty(oldPassword))
            return;

        string? newPassword = await _passwordPromptService.ShowAsync(
            "Change Master Password",
            "Enter a new master password:",
            "New master password");
        if (string.IsNullOrEmpty(newPassword))
            return;

        string? confirmation = await _passwordPromptService.ShowAsync(
            "Confirm Master Password",
            "Enter the new master password again:",
            "Confirm new master password");
        if (newPassword != confirmation)
        {
            await Shell.Current.DisplayAlertAsync("Error", "Passwords do not match.", "OK");
            return;
        }

        IsBusy = true;
        var (ok, error) = await Task.Run(
            () => _vaultService.ChangeMasterPassword(vault.Name, oldPassword, newPassword));
        IsBusy = false;
        await Shell.Current.DisplayAlertAsync(
            ok ? "Success" : "Error",
            ok ? "Master password changed." : error,
            "OK");
    }

    [RelayCommand]
    public async Task CreateVaultAsync()
    {
        if (string.IsNullOrWhiteSpace(NewVaultName) || string.IsNullOrWhiteSpace(NewMasterPassword))
        {
            await Shell.Current.DisplayAlertAsync("Error", "Vault name and password are required.", "OK");
            return;
        }

        IsBusy = true;
        var name = NewVaultName.Trim();
        var password = NewMasterPassword;
        var confirmation = ConfirmMasterPassword;
        var (ok, err) = await Task.Run(
            () => _vaultService.CreateVault(name, password, confirmation));
        IsBusy = false;

        if (!ok)
        {
            await Shell.Current.DisplayAlertAsync("Error", err, "OK");
            return;
        }

        NewVaultName = string.Empty;
        NewMasterPassword = string.Empty;
        ConfirmMasterPassword = string.Empty;
        LoadVaults();
        await Shell.Current.DisplayAlertAsync("Success", "Vault created.", "OK");
    }

    [RelayCommand]
    public async Task ImportVaultsAsync()
    {
        IReadOnlyList<FileResult> files;
        try
        {
            var pickedFiles = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Select PwM vault files"
            });
            files = pickedFiles?.OfType<FileResult>().ToList() ?? [];
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Import Failed", ex.Message, "OK");
            return;
        }

        if (files.Count == 0)
            return;

        IsBusy = true;
        var imported = 0;
        var errors = new List<string>();

        foreach (var file in files)
        {
            var vaultName = Path.GetFileNameWithoutExtension(file.FileName);
            var overwrite = false;

            if (_vaultService.VaultExists(vaultName))
            {
                overwrite = await Shell.Current.DisplayAlertAsync(
                    "Replace Vault",
                    $"A vault named '{vaultName}' already exists. Replace it?",
                    "Replace",
                    "Skip");

                if (!overwrite)
                    continue;
            }

            var (ok, error) = await _vaultService.ImportVaultAsync(file, overwrite);
            if (ok)
                imported++;
            else
                errors.Add(error);
        }

        IsBusy = false;
        LoadVaults();

        var message = imported == 1
            ? "1 vault imported."
            : $"{imported} vaults imported.";

        if (errors.Count > 0)
            message += $"\n\n{string.Join("\n", errors)}";

        await Shell.Current.DisplayAlertAsync(
            errors.Count > 0 ? "Import Completed with Errors" : "Import Complete",
            message,
            "OK");
    }

    [RelayCommand]
    public async Task DeleteVaultAsync(VaultInfo? vault)
    {
        if (vault == null)
        {
            await Shell.Current.DisplayAlertAsync("Error", "The selected vault could not be loaded.", "OK");
            LoadVaults();
            return;
        }

        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Delete Vault",
            $"Delete '{vault.Name}'? This is permanent.",
            "Delete", "Cancel");

        if (!confirmed) return;

        var (ok, err) = _vaultService.DeleteVault(vault.Name);
        if (!ok)
            await Shell.Current.DisplayAlertAsync("Error", err, "OK");
        else
        {
            if (_vaultSession.IsUnlocked &&
                string.Equals(
                    vault.Name,
                    _vaultSession.VaultName,
                    StringComparison.OrdinalIgnoreCase))
            {
                _vaultSession.Lock();
            }

            LoadVaults();
        }
    }

    [RelayCommand]
    public async Task OpenVaultAsync(VaultInfo? vault)
    {
        if (vault == null)
        {
            await Shell.Current.DisplayAlertAsync("Error", "The selected vault could not be loaded.", "OK");
            LoadVaults();
            return;
        }

        string? password = await _passwordPromptService.ShowAsync(
            "Open Vault",
            $"Enter master password for '{vault.Name}':",
            "Master password");

        if (string.IsNullOrEmpty(password)) return;

        try
        {
            IsBusy = true;
            var (ok, err, _) = await Task.Run(
                () => _vaultService.OpenVault(vault.Name, password));

            if (!ok)
            {
                await Shell.Current.DisplayAlertAsync("Error", err, "OK");
                return;
            }

            _vaultSession.Unlock(vault.Name, password);
            await Shell.Current.GoToAsync(nameof(Pages.VaultPage));
        }
        catch (Exception ex)
        {
            _vaultSession.Lock();
            await Shell.Current.DisplayAlertAsync("Open Vault Failed", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyVaultFilter();
    }

    private void ApplyVaultFilter()
    {
        var query = SearchText.Trim();
        var matches = string.IsNullOrEmpty(query)
            ? _allVaults
            : _allVaults
                .Where(vault => vault.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

        Vaults.Clear();
        foreach (var vault in matches)
            Vaults.Add(vault);
    }

    private static async Task<bool> EnsureVaultAsync(VaultInfo? vault)
    {
        if (vault is not null)
            return true;

        await Shell.Current.DisplayAlertAsync("Error", "The selected vault could not be loaded.", "OK");
        return false;
    }
}
