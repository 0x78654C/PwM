using System.Collections.ObjectModel;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PwM.Mobile.Models;
using PwM.Mobile.Services;

namespace PwM.Mobile.ViewModels;

public partial class VaultViewModel : BaseViewModel, IDisposable
{
    private readonly VaultService _vaultService;
    private readonly VaultSession _vaultSession;
    private readonly PasswordPromptService _passwordPromptService;
    private readonly SettingsService _settingsService;
    private readonly List<CredentialEntry> _allCredentials = [];
    private System.Timers.Timer? _autoLockTimer;

    public ObservableCollection<CredentialEntry> Credentials { get; } = [];

    public string VaultName => _vaultSession.VaultName;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLocked = true;

    public VaultViewModel(
        VaultService vaultService,
        VaultSession vaultSession,
        PasswordPromptService passwordPromptService,
        SettingsService settingsService)
    {
        _vaultService = vaultService;
        _vaultSession = vaultSession;
        _passwordPromptService = passwordPromptService;
        _settingsService = settingsService;
        Title = vaultSession.VaultName;
    }

    public async Task LoadCredentialsAsync()
    {
        Credentials.Clear();
        _allCredentials.Clear();
        if (!_vaultSession.IsUnlocked)
        {
            IsLocked = true;
            StopAutoLockTimer();
            return;
        }

        Title = VaultName;
        OnPropertyChanged(nameof(VaultName));
        var vaultName = VaultName;
        var masterPassword = _vaultSession.MasterPassword;
        IsBusy = true;
        var (ok, _, entries) = await Task.Run(
            () => _vaultService.OpenVault(vaultName, masterPassword));
        IsBusy = false;

        if (!ok ||
            !_vaultSession.IsUnlocked ||
            !string.Equals(VaultName, vaultName, StringComparison.Ordinal))
            return;

        _allCredentials.AddRange(entries.OrderBy(e => e.Application));
        ApplyCredentialFilter();

        IsLocked = false;
        StartAutoLockTimer();
    }

    [RelayCommand]
    public async Task DeleteCredentialAsync(CredentialEntry? entry)
    {
        if (entry is null)
        {
            await ShowMissingCredentialErrorAsync();
            return;
        }

        ResetAutoLockTimer();
        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Delete Credential",
            $"Remove '{entry.Account}' from '{entry.Application}'?",
            "Delete", "Cancel");
        if (!confirmed) return;

        IsBusy = true;
        var (ok, err) = await Task.Run(() => _vaultService.DeleteCredential(
            VaultName, _vaultSession.MasterPassword, entry.Application, entry.Account));
        IsBusy = false;
        if (!ok)
            await Shell.Current.DisplayAlertAsync("Error", err, "OK");
        else
            await LoadCredentialsAsync();
    }

    [RelayCommand]
    public async Task UpdatePasswordAsync(CredentialEntry? entry)
    {
        if (entry is null)
        {
            await ShowMissingCredentialErrorAsync();
            return;
        }

        ResetAutoLockTimer();
        string? newPwd = await _passwordPromptService.ShowAsync(
            "Update Password",
            $"New password for '{entry.Account}' @ '{entry.Application}':",
            "New password");

        if (string.IsNullOrEmpty(newPwd)) return;

        IsBusy = true;
        var (ok, err) = await Task.Run(() => _vaultService.UpdatePassword(
            VaultName, _vaultSession.MasterPassword, entry.Application, entry.Account, newPwd));
        IsBusy = false;
        if (!ok)
            await Shell.Current.DisplayAlertAsync("Error", err, "OK");
        else
        {
            await Shell.Current.DisplayAlertAsync("Updated", "Password updated.", "OK");
            await LoadCredentialsAsync();
        }
    }

    [RelayCommand]
    public async Task CopyPasswordAsync(CredentialEntry? entry)
    {
        if (entry is null)
        {
            await ShowMissingCredentialErrorAsync();
            return;
        }

        ResetAutoLockTimer();
        await Clipboard.Default.SetTextAsync(entry.Password);
        await Shell.Current.DisplayAlertAsync("Copied", "Password copied. It will be cleared in 15 seconds.", "OK");

        // Auto-clear clipboard after 15 seconds
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(15));
            var current = await Clipboard.Default.GetTextAsync();
            if (current == entry.Password)
                await MainThread.InvokeOnMainThreadAsync(() => Clipboard.Default.SetTextAsync(string.Empty));
        });
    }

    [RelayCommand]
    public async Task ShowPasswordAsync(CredentialEntry? entry)
    {
        if (entry is null)
        {
            await ShowMissingCredentialErrorAsync();
            return;
        }

        ResetAutoLockTimer();
        try
        {
            await Shell.Current.DisplayAlertAsync("Password", entry.Password, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Could not show password: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task AddCredentialAsync()
    {
        ResetAutoLockTimer();
        await Shell.Current.GoToAsync(nameof(Pages.AddCredentialPage));
    }

    [RelayCommand]
    public async Task ChangeMasterPasswordAsync()
    {
        ResetAutoLockTimer();
        string? newPwd = await _passwordPromptService.ShowAsync(
            "Change Master Password",
            "Enter new master password:",
            "New master password");

        if (string.IsNullOrEmpty(newPwd)) return;

        string? confirmPwd = await _passwordPromptService.ShowAsync(
            "Confirm",
            "Confirm new master password:",
            "Confirm new master password");

        if (newPwd != confirmPwd)
        {
            await Shell.Current.DisplayAlertAsync("Error", "Passwords do not match.", "OK");
            return;
        }

        IsBusy = true;
        var (ok, err) = await Task.Run(
            () => _vaultService.ChangeMasterPassword(VaultName, _vaultSession.MasterPassword, newPwd));
        IsBusy = false;
        if (!ok)
            await Shell.Current.DisplayAlertAsync("Error", err, "OK");
        else
        {
            _vaultSession.Unlock(VaultName, newPwd);
            await Shell.Current.DisplayAlertAsync("Success", "Master password changed.", "OK");
        }
    }

    [RelayCommand]
    public async Task LockVaultAsync()
    {
        StopAutoLockTimer();
        _vaultSession.Lock();
        IsLocked = true;
        Credentials.Clear();
        await Shell.Current.GoToAsync("//VaultListPage");
    }

    private void StartAutoLockTimer()
    {
        StopAutoLockTimer();
        int minutes = _settingsService.AutoLockMinutes;
        if (minutes <= 0) return;

        _autoLockTimer = new System.Timers.Timer(TimeSpan.FromMinutes(minutes).TotalMilliseconds);
        _autoLockTimer.Elapsed += OnAutoLockElapsed;
        _autoLockTimer.AutoReset = false;
        _autoLockTimer.Start();
    }

    private void StopAutoLockTimer()
    {
        _autoLockTimer?.Stop();
        _autoLockTimer?.Dispose();
        _autoLockTimer = null;
    }

    private void OnAutoLockElapsed(object? sender, ElapsedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (IsLocked || !_vaultSession.IsUnlocked)
                return;

            await Shell.Current.DisplayAlertAsync("Auto-locked", "Vault was locked due to inactivity.", "OK");
            await LockVaultAsync();
        });
    }

    /// <summary>
    /// Called by the page when the user interacts — resets the auto-lock timer.
    /// </summary>
    public void ResetAutoLockTimer()
    {
        if (IsLocked || !_vaultSession.IsUnlocked)
            StopAutoLockTimer();
        else
            StartAutoLockTimer();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyCredentialFilter();
    }

    private void ApplyCredentialFilter()
    {
        var query = SearchText.Trim();
        var matches = string.IsNullOrEmpty(query)
            ? _allCredentials
            : _allCredentials
                .Where(entry =>
                    entry.Application.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    entry.Account.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

        Credentials.Clear();
        foreach (var entry in matches)
            Credentials.Add(entry);
    }

    private static Task ShowMissingCredentialErrorAsync() =>
        Shell.Current.DisplayAlertAsync("Error", "The selected credential could not be loaded.", "OK");

    public void Dispose()
    {
        StopAutoLockTimer();
        _vaultSession.Lock();
    }
}
