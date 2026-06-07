using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PwM.Mobile.Services;
using PwMLib;

namespace PwM.Mobile.ViewModels;

[QueryProperty(nameof(VaultName), "vaultName")]
[QueryProperty(nameof(MasterPassword), "masterPassword")]
public partial class AddCredentialViewModel : BaseViewModel
{
    private readonly VaultService _vaultService;
    private readonly HibpService _hibpService;

    [ObservableProperty] private string _vaultName = string.Empty;
    [ObservableProperty] private string _masterPassword = string.Empty;
    [ObservableProperty] private string _application = string.Empty;
    [ObservableProperty] private string _account = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private bool _isPasswordVisible;
    [ObservableProperty] private string _breachWarning = string.Empty;
    [ObservableProperty] private int _generatedLength = 16;

    public AddCredentialViewModel(VaultService vaultService, HibpService hibpService)
    {
        _vaultService = vaultService;
        _hibpService = hibpService;
        Title = "Add Credential";
    }

    [RelayCommand]
    public void GeneratePassword()
    {
        Password = PasswordGenerator.GeneratePassword(GeneratedLength);
    }

    [RelayCommand]
    public void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    [RelayCommand]
    public async Task CheckBreachAsync()
    {
        if (string.IsNullOrEmpty(Password)) return;
        IsBusy = true;
        bool breached = await _hibpService.IsBreachedAsync(Password);
        IsBusy = false;
        BreachWarning = breached ? "⚠️ This password was found in a data breach!" : "✅ Not found in known breaches.";
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Application) || string.IsNullOrWhiteSpace(Account) || string.IsNullOrWhiteSpace(Password))
        {
            await Shell.Current.DisplayAlert("Error", "All fields are required.", "OK");
            return;
        }

        IsBusy = true;

        // Auto breach check on save
        bool breached = await _hibpService.IsBreachedAsync(Password);
        if (breached)
        {
            bool proceed = await Shell.Current.DisplayAlert(
                "⚠️ Breach Warning",
                "This password was found in a known data breach. Save anyway?",
                "Save Anyway", "Cancel");
            if (!proceed) { IsBusy = false; return; }
        }

        var (ok, err) = _vaultService.AddCredential(VaultName, MasterPassword, Application.Trim(), Account.Trim(), Password);
        IsBusy = false;

        if (!ok)
        {
            await Shell.Current.DisplayAlert("Error", err, "OK");
            return;
        }

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    public async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
