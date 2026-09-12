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
    private readonly HibpService _hibpService;
    private readonly SensitiveClipboardService _clipboardService;
    private readonly List<CredentialEntry> _allCredentials = [];
    private System.Timers.Timer? _autoLockTimer;
    private int _credentialLoadVersion;
    private CancellationTokenSource? _breachChecks;

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
        SettingsService settingsService,
        HibpService hibpService,
        SensitiveClipboardService clipboardService)
    {
        _vaultService = vaultService;
        _vaultSession = vaultSession;
        _passwordPromptService = passwordPromptService;
        _settingsService = settingsService;
        _hibpService = hibpService;
        _clipboardService = clipboardService;
        Title = vaultSession.VaultName;
    }

    public async Task LoadCredentialsAsync()
    {
        _vaultSession.Locked -= OnSessionLocked;
        _vaultSession.Locked += OnSessionLocked;
        var loadVersion = ++_credentialLoadVersion;
        var sessionVersion = _vaultSession.Version;
        _breachChecks?.Cancel();
        _breachChecks?.Dispose();
        _breachChecks = new CancellationTokenSource();
        Credentials.Clear();
        foreach (var entry in _allCredentials)
            entry.Password = string.Empty;
        _allCredentials.Clear();
        if (!_vaultSession.IsUnlocked)
        {
            OnSessionLocked(this, EventArgs.Empty);
            return;
        }

        Title = VaultName;
        OnPropertyChanged(nameof(VaultName));
        var vaultName = VaultName;
        var masterPassword = _vaultSession.MasterPassword;
        IsBusy = true;
        var entries = _vaultSession.TakePrefetchedCredentials(vaultName);
        var ok = entries is not null;

        if (entries is null)
        {
            var openResult = await Task.Run(
                () => _vaultService.OpenVault(vaultName, masterPassword));
            ok = openResult.success;
            entries = openResult.entries;
        }

        if (!ok || loadVersion != _credentialLoadVersion ||
            !_vaultSession.IsCurrent(sessionVersion) ||
            !string.Equals(VaultName, vaultName, StringComparison.Ordinal))
        {
            foreach (var entry in entries ?? [])
                entry.Password = string.Empty;
            IsBusy = false;
            return;
        }

        _allCredentials.AddRange(entries.OrderBy(e => e.Application));
        IsLocked = false;
        ApplyCredentialFilter();

        IsBusy = false;
        StartAutoLockTimer();
        _ = CheckBreachesAsync(entries, vaultName, loadVersion, _breachChecks.Token);
    }

    [RelayCommand]
    public async Task DeleteCredentialAsync(CredentialEntry? entry)
    {
        if (!CanAccess(entry)) return;
        var sessionVersion = _vaultSession.Version;
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
        if (!confirmed || !_vaultSession.IsCurrent(sessionVersion)) return;

        IsBusy = true;
        var vaultName = VaultName;
        var masterPassword = _vaultSession.MasterPassword;
        var (ok, err) = await Task.Run(() => _vaultService.DeleteCredential(
            vaultName, masterPassword, entry.Application, entry.Account));
        IsBusy = false;
        if (!_vaultSession.IsCurrent(sessionVersion)) return;
        if (!ok)
            await Shell.Current.DisplayAlertAsync("Error", err, "OK");
        else
            await LoadCredentialsAsync();
    }

    [RelayCommand]
    public async Task UpdatePasswordAsync(CredentialEntry? entry)
    {
        if (!CanAccess(entry)) return;
        var sessionVersion = _vaultSession.Version;
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

        if (string.IsNullOrEmpty(newPwd) || !_vaultSession.IsCurrent(sessionVersion)) return;

        IsBusy = true;
        var vaultName = VaultName;
        var masterPassword = _vaultSession.MasterPassword;
        var (ok, err) = await Task.Run(() => _vaultService.UpdatePassword(
            vaultName, masterPassword, entry.Application, entry.Account, newPwd));
        IsBusy = false;
        if (!_vaultSession.IsCurrent(sessionVersion)) return;
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
        if (!CanAccess(entry)) return;
        if (entry is null)
        {
            await ShowMissingCredentialErrorAsync();
            return;
        }

        ResetAutoLockTimer();
        try
        {
            await _clipboardService.CopyForAsync(entry.Password, TimeSpan.FromSeconds(15));
            await Shell.Current.DisplayAlertAsync(
                "Copied",
                "Password copied. It will be cleared in 15 seconds.",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Copy Failed", ex.Message, "OK");
        }
    }

    [RelayCommand]
    public async Task ShowPasswordAsync(CredentialEntry? entry)
    {
        if (!CanAccess(entry)) return;
        if (entry is null)
        {
            await ShowMissingCredentialErrorAsync();
            return;
        }

        ResetAutoLockTimer();
        try
        {
            await _passwordPromptService.ShowPasswordAsync(entry.Password);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Could not show password: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task AddCredentialAsync()
    {
        if (IsLocked || !_vaultSession.IsUnlocked) return;
        ResetAutoLockTimer();
        await Shell.Current.GoToAsync(nameof(Pages.AddCredentialPage));
    }

    [RelayCommand]
    public async Task ChangeMasterPasswordAsync()
    {
        if (IsLocked || !_vaultSession.IsUnlocked) return;
        var sessionVersion = _vaultSession.Version;
        var vaultName = VaultName;
        var masterPassword = _vaultSession.MasterPassword;
        ResetAutoLockTimer();
        string? newPwd = await _passwordPromptService.ShowAsync(
            "Change Master Password",
            "Enter new master password:",
            "New master password");

        if (string.IsNullOrEmpty(newPwd) || !_vaultSession.IsCurrent(sessionVersion)) return;

        string? confirmPwd = await _passwordPromptService.ShowAsync(
            "Confirm",
            "Confirm new master password:",
            "Confirm new master password");

        if (!_vaultSession.IsCurrent(sessionVersion)) return;
        if (newPwd != confirmPwd)
        {
            await Shell.Current.DisplayAlertAsync("Error", "Passwords do not match.", "OK");
            return;
        }

        IsBusy = true;
        var (ok, err) = await Task.Run(
            () => _vaultService.ChangeMasterPassword(vaultName, masterPassword, newPwd));
        IsBusy = false;
        if (!_vaultSession.IsCurrent(sessionVersion)) return;
        if (!ok)
            await Shell.Current.DisplayAlertAsync("Error", err, "OK");
        else
        {
            _vaultSession.TryUpdateMasterPassword(sessionVersion, newPwd);
            await Shell.Current.DisplayAlertAsync("Success", "Master password changed.", "OK");
        }
    }

    [RelayCommand]
    public async Task LockVaultAsync()
    {
        _vaultSession.Lock();
        await Shell.Current.GoToAsync("//VaultListPage");
    }

    private bool CanAccess(CredentialEntry? entry) =>
        !IsLocked && _vaultSession.IsUnlocked && entry is not null && _allCredentials.Contains(entry);

    private void OnSessionLocked(object? sender, EventArgs e)
    {
        _breachChecks?.Cancel();
        _breachChecks?.Dispose();
        _breachChecks = null;
        StopAutoLockTimer();
        ++_credentialLoadVersion;
        IsLocked = true;
        foreach (var entry in _allCredentials)
            entry.Password = string.Empty;
        _allCredentials.Clear();
        Credentials.Clear();
        SearchText = string.Empty;
        _vaultSession.Locked -= OnSessionLocked;
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
            if (!ReferenceEquals(sender, _autoLockTimer) || IsLocked || !_vaultSession.IsUnlocked)
                return;

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
        if (IsLocked || !_vaultSession.IsUnlocked)
        {
            Credentials.Clear();
            return;
        }
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

    private async Task CheckBreachesAsync(
        IEnumerable<CredentialEntry> entries,
        string vaultName,
        int loadVersion,
        CancellationToken cancellationToken)
    {
        await Task.WhenAll(entries.Select(async entry =>
        {
            bool? hasBreach;
            try
            {
                hasBreach = await _hibpService.IsBreachedAsync(entry.Password, cancellationToken);
            }
            catch (OperationCanceledException) { return; }
            if (loadVersion != _credentialLoadVersion ||
                !_vaultSession.IsUnlocked ||
                !string.Equals(VaultName, vaultName, StringComparison.Ordinal))
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (loadVersion != _credentialLoadVersion || !_vaultSession.IsUnlocked)
                    return;
                entry.HasBreach = hasBreach == true;
                entry.IsBreachCheckUnavailable = hasBreach is null;
                entry.IsBreachCheckPending = false;
            });
        }));
    }

    private static Task ShowMissingCredentialErrorAsync() =>
        Shell.Current.DisplayAlertAsync("Error", "The selected credential could not be loaded.", "OK");

    public void Dispose()
    {
        OnSessionLocked(this, EventArgs.Empty);
    }
}
