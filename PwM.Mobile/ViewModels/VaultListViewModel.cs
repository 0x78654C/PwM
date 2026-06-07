using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PwM.Mobile.Models;
using PwM.Mobile.Services;

namespace PwM.Mobile.ViewModels;

public partial class VaultListViewModel : BaseViewModel
{
    private readonly VaultService _vaultService;

    public ObservableCollection<VaultInfo> Vaults { get; } = [];

    [ObservableProperty]
    private string _newVaultName = string.Empty;

    [ObservableProperty]
    private string _newMasterPassword = string.Empty;

    [ObservableProperty]
    private string _confirmMasterPassword = string.Empty;

    public VaultListViewModel(VaultService vaultService)
    {
        _vaultService = vaultService;
        Title = "Vaults";
    }

    [RelayCommand]
    public void LoadVaults()
    {
        Vaults.Clear();
        foreach (var v in _vaultService.ListVaults().OrderBy(v => v.Name))
            Vaults.Add(v);
    }

    [RelayCommand]
    public async Task CreateVaultAsync()
    {
        if (string.IsNullOrWhiteSpace(NewVaultName) || string.IsNullOrWhiteSpace(NewMasterPassword))
        {
            await Shell.Current.DisplayAlert("Error", "Vault name and password are required.", "OK");
            return;
        }

        IsBusy = true;
        var (ok, err) = _vaultService.CreateVault(NewVaultName.Trim(), NewMasterPassword, ConfirmMasterPassword);
        IsBusy = false;

        if (!ok)
        {
            await Shell.Current.DisplayAlert("Error", err, "OK");
            return;
        }

        NewVaultName = string.Empty;
        NewMasterPassword = string.Empty;
        ConfirmMasterPassword = string.Empty;
        LoadVaults();
        await Shell.Current.DisplayAlert("Success", "Vault created!", "OK");
    }

    [RelayCommand]
    public async Task DeleteVaultAsync(VaultInfo vault)
    {
        bool confirmed = await Shell.Current.DisplayAlert(
            "Delete Vault",
            $"Delete '{vault.Name}'? This is permanent.",
            "Delete", "Cancel");

        if (!confirmed) return;

        var (ok, err) = _vaultService.DeleteVault(vault.Name);
        if (!ok)
            await Shell.Current.DisplayAlert("Error", err, "OK");
        else
            LoadVaults();
    }

    [RelayCommand]
    public async Task OpenVaultAsync(VaultInfo vault)
    {
        string? password = await Shell.Current.DisplayPromptAsync(
            "Open Vault",
            $"Enter master password for '{vault.Name}':",
            keyboard: Keyboard.Default,
            placeholder: "Master password");

        if (string.IsNullOrEmpty(password)) return;

        IsBusy = true;
        var (ok, err, _) = _vaultService.OpenVault(vault.Name, password);
        IsBusy = false;

        if (!ok)
        {
            await Shell.Current.DisplayAlert("Error", err, "OK");
            return;
        }

        await Shell.Current.GoToAsync(
            $"{nameof(Pages.VaultPage)}?vaultName={Uri.EscapeDataString(vault.Name)}&masterPassword={Uri.EscapeDataString(password)}");
    }
}
