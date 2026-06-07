using System.Collections.ObjectModel;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PwM.Mobile.Models;
using PwM.Mobile.Services;

namespace PwM.Mobile.ViewModels;

[QueryProperty(nameof(VaultName), "vaultName")]
[QueryProperty(nameof(MasterPassword), "masterPassword")]
public partial class VaultViewModel : BaseViewModel, IDisposable
{
    private readonly VaultService _vaultService;
    private readonly SettingsService _settingsService;
    private readonly HibpService _hibpService;
    private System.Timers.Timer? _autoLockTimer;

    public ObservableCollection<CredentialEntry> Credentials { get; } = [];

    [ObservableProperty]
    private string _vaultName = string.Empty;

    [ObservableProperty]
    private string _masterPassword = string.Empty;

    [ObservableProperty]
    private bool _isLocked = true;

    public VaultViewModel(VaultService vaultService, SettingsService settingsService, HibpService hibpService)
    {
        _vaultService = vaultService;
        _settingsService = settingsService;
        _hibpService = hibpService;
    }

    partial void OnVaultNameChanged(string value)
    {
        Title = value;
    }

    partial void OnMasterPasswordChanged(string value)
    {
        if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(VaultName))
            LoadCredentials();
    }

    public void LoadCredentials()
    {
        Credentials.Clear();
        var (ok, err, entries) = _vaultService.OpenVault(VaultName, MasterPassword);
        if (!ok) return;

        foreach (var e in entries.OrderBy(e => e.Application))
            Credentials.Add(e);

        IsLocked = false;
        StartAutoLockTimer();
    }

    [RelayCommand]
    public async Task DeleteCredentialAsync(CredentialEntry entry)
    {
        bool confirmed = await Shell.Current.DisplayAlert(
            "Delete Credential",
            $"Remove '{entry.Account}' from '{entry.Application}'?",
            "Delete", "Cancel");
        if (!confirmed) return;

        var (ok, err) = _vaultService.DeleteCredential(VaultName, MasterPassword, entry.Application, entry.Account);
        if (!ok)
            await Shell.Current.DisplayAlert("Error", err, "OK");
        else
            LoadCredentials();
    }

    [RelayCommand]
    public async Task UpdatePasswordAsync(CredentialEntry entry)
    {
        string? newPwd = await Shell.Current.DisplayPromptAsync(
            "Update Password",
            $"New password for '{entry.Account}' @ '{entry.Application}':",
            placeholder: "New password");

        if (string.IsNullOrEmpty(newPwd)) return;

        var (ok, err) = _vaultService.UpdatePassword(VaultName, MasterPassword, entry.Application, entry.Account, newPwd);
        if (!ok)
            await Shell.Current.DisplayAlert("Error", err, "OK");
        else
        {
            await Shell.Current.DisplayAlert("Updated", "Password updated.", "OK");
            LoadCredentials();
        }
    }

    [RelayCommand]
    public async Task CopyPasswordAsync(CredentialEntry entry)
    {
        await Clipboard.Default.SetTextAsync(entry.Password);
        await Shell.Current.DisplayAlert("Copied", "Password copied. It will be cleared in 15 seconds.", "OK");

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
    public async Task ShowPasswordAsync(CredentialEntry entry)
    {
        await Shell.Current.DisplayAlert("Password", entry.Password, "OK");
    }

    [RelayCommand]
    public async Task AddCredentialAsync()
    {
        await Shell.Current.GoToAsync(
            $"{nameof(Pages.AddCredentialPage)}?vaultName={Uri.EscapeDataString(VaultName)}&masterPassword={Uri.EscapeDataString(MasterPassword)}");
    }

    [RelayCommand]
    public async Task ChangeMasterPasswordAsync()
    {
        string? newPwd = await Shell.Current.DisplayPromptAsync(
            "Change Master Password",
            "Enter new master password:",
            placeholder: "New master password");

        if (string.IsNullOrEmpty(newPwd)) return;

        string? confirmPwd = await Shell.Current.DisplayPromptAsync(
            "Confirm",
            "Confirm new master password:",
            placeholder: "Confirm new master password");

        if (newPwd != confirmPwd)
        {
            await Shell.Current.DisplayAlert("Error", "Passwords do not match.", "OK");
            return;
        }

        var (ok, err) = _vaultService.ChangeMasterPassword(VaultName, MasterPassword, newPwd);
        if (!ok)
            await Shell.Current.DisplayAlert("Error", err, "OK");
        else
        {
            MasterPassword = newPwd;
            await Shell.Current.DisplayAlert("Success", "Master password changed.", "OK");
        }
    }

    [RelayCommand]
    public async Task LockVaultAsync()
    {
        StopAutoLockTimer();
        MasterPassword = string.Empty;
        IsLocked = true;
        Credentials.Clear();
        await Shell.Current.GoToAsync("..");
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
            await Shell.Current.DisplayAlert("Auto-locked", "Vault was locked due to inactivity.", "OK");
            await LockVaultAsync();
        });
    }

    /// <summary>
    /// Called by the page when the user interacts — resets the auto-lock timer.
    /// </summary>
    public void ResetAutoLockTimer() => StartAutoLockTimer();

    public void Dispose()
    {
        StopAutoLockTimer();
        MasterPassword = string.Empty;
    }
}
