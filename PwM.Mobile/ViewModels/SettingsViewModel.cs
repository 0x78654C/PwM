using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using PwM.Mobile.Services;

namespace PwM.Mobile.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly SettingsService _settingsService;

    public string AppVersion => AppInfo.Current.VersionString;
    public IReadOnlyList<string> ThemeOptions { get; } = ["System", "Light", "Dark"];

    [ObservableProperty] private int _argon2Iterations;
    [ObservableProperty] private int _argon2MemorySize;
    [ObservableProperty] private int _argon2Parallelism;
    [ObservableProperty] private int _autoLockMinutes;
    [ObservableProperty] private string _selectedTheme = "System";

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        Title = "Settings";
        Load();
    }

    private void Load()
    {
        Argon2Iterations = _settingsService.Argon2Iterations;
        Argon2MemorySize = _settingsService.Argon2MemorySize;
        Argon2Parallelism = _settingsService.Argon2Parallelism;
        AutoLockMinutes = _settingsService.AutoLockMinutes;
        SelectedTheme = _settingsService.Theme;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (Argon2Iterations < 10 || Argon2Iterations > 200)
        { await Shell.Current.DisplayAlertAsync("Invalid", "Iterations must be 10-200.", "OK"); return; }

        if (Argon2MemorySize < 4096 || Argon2MemorySize > 1_048_576)
        { await Shell.Current.DisplayAlertAsync("Invalid", "Memory size must be 4096-1 048 576 KB.", "OK"); return; }

        if (Argon2Parallelism < 1 || Argon2Parallelism > 16)
        { await Shell.Current.DisplayAlertAsync("Invalid", "Parallelism must be 1-16.", "OK"); return; }

        if (AutoLockMinutes < 0)
        { await Shell.Current.DisplayAlertAsync("Invalid", "Auto-lock must be 0 or greater (0 = disabled).", "OK"); return; }

        _settingsService.Argon2Iterations = Argon2Iterations;
        _settingsService.Argon2MemorySize = Argon2MemorySize;
        _settingsService.Argon2Parallelism = Argon2Parallelism;
        _settingsService.AutoLockMinutes = AutoLockMinutes;
        _settingsService.Theme = SelectedTheme;

        await Shell.Current.DisplayAlertAsync("Saved",
            "Settings saved.\nChanging Argon2 parameters affects all vaults. Re-create them to apply new values.",
            "OK");
    }

    [RelayCommand]
    public void ResetDefaults()
    {
        Argon2Iterations = 40;
        Argon2MemorySize = 4096;
        Argon2Parallelism = 2;
        AutoLockMinutes = 10;
        SelectedTheme = "System";
    }

    [RelayCommand]
    public Task OpenProjectPageAsync()
    {
        return Browser.Default.OpenAsync(
            "https://github.com/0x78654C/PwM",
            BrowserLaunchMode.SystemPreferred);
    }
}
