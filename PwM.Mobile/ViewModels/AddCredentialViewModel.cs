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
    private CancellationTokenSource? _breachCheckCancellation;

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

    partial void OnPasswordChanged(string value)
    {
        BreachWarning = string.Empty;
        CancelScheduledBreachCheck();
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
        if (string.IsNullOrEmpty(Password) || !_vaultSession.IsUnlocked) return;

        CancelScheduledBreachCheck();
        _breachCheckCancellation = new CancellationTokenSource();
        var cancellationToken = _breachCheckCancellation.Token;
        var password = Password;
        IsBusy = true;
        try
        {
            var breached = await _hibpService.IsBreachedAsync(password, cancellationToken);
            if (!cancellationToken.IsCancellationRequested && Password == password && _vaultSession.IsUnlocked)
                SetBreachWarning(breached);
        }
        catch (OperationCanceledException) { }
        finally { IsBusy = false; }
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
        var vaultName = _vaultSession.VaultName;
        var sessionVersion = _vaultSession.Version;
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
        if (!_vaultSession.IsCurrent(sessionVersion)) return;

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

    public void Activate()
    {
        _vaultSession.Locked -= OnSessionLocked;
        _vaultSession.Locked += OnSessionLocked;
    }

    public void Deactivate()
    {
        _vaultSession.Locked -= OnSessionLocked;
        ClearFields();
    }

    private void OnSessionLocked(object? sender, EventArgs e) => ClearFields();

    private void ClearFields()
    {
        CancelScheduledBreachCheck();
        Password = string.Empty;
        Account = string.Empty;
        Application = string.Empty;
        IsPasswordVisible = false;
    }

    private void CancelScheduledBreachCheck()
    {
        _breachCheckCancellation?.Cancel();
        _breachCheckCancellation?.Dispose();
        _breachCheckCancellation = null;
    }

    private void SetBreachWarning(bool? breached)
    {
        BreachWarning = breached switch
        {
            true => "⚠️ This password was found in a data breach!",
            false => "✅ Not found in known breaches.",
            null => "Breach check unavailable. Try again when connected."
        };
    }
}
