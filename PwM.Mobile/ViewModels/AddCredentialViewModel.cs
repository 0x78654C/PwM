using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PwM.Mobile.Services;
using PwMLib;

namespace PwM.Mobile.ViewModels;

public partial class AddCredentialViewModel : BaseViewModel
{
    private readonly VaultService _vaultService;
    private readonly VaultSession _vaultSession;
    private readonly HibpService _hibpService;

    [ObservableProperty] private string _application = string.Empty;
    [ObservableProperty] private string _account = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private bool _isPasswordVisible;
    [ObservableProperty] private string _breachWarning = string.Empty;
    [ObservableProperty] private int _generatedLength = 16;

    public string PasswordVisibilityLabel => IsPasswordVisible ? "Hide" : "Show";

    public AddCredentialViewModel(VaultService vaultService, VaultSession vaultSession, HibpService hibpService)
    {
        _vaultService = vaultService;
        _vaultSession = vaultSession;
        _hibpService = hibpService;
        Title = "Add Credential";
    }

    partial void OnIsPasswordVisibleChanged(bool value) =>
        OnPropertyChanged(nameof(PasswordVisibilityLabel));

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
            await Shell.Current.DisplayAlertAsync("Error", "All fields are required.", "OK");
            return;
        }

        if (!_vaultSession.IsUnlocked)
        {
            await Shell.Current.DisplayAlertAsync("Locked", "Unlock the vault before adding a credential.", "OK");
            await Shell.Current.GoToAsync("//VaultListPage");
            return;
        }

        IsBusy = true;

        // Auto breach check on save
        bool breached = await _hibpService.IsBreachedAsync(Password);
        if (breached)
        {
            bool proceed = await Shell.Current.DisplayAlertAsync(
                "⚠️ Breach Warning",
                "This password was found in a known data breach. Save anyway?",
                "Save Anyway", "Cancel");
            if (!proceed) { IsBusy = false; return; }
        }

        var vaultName = _vaultSession.VaultName;
        var masterPassword = _vaultSession.MasterPassword;
        var application = Application.Trim();
        var account = Account.Trim();
        var entryPassword = Password;
        var (ok, err) = await Task.Run(() => _vaultService.AddCredential(
            vaultName,
            masterPassword,
            application,
            account,
            entryPassword));
        IsBusy = false;

        if (!ok)
        {
            await Shell.Current.DisplayAlertAsync("Error", err, "OK");
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
