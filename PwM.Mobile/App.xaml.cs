using PwM.Mobile.Services;

namespace PwM.Mobile;

public partial class App : Application
{
    private readonly VaultSession _vaultSession;
    private bool _lockedForBackground;

    public App(SettingsService settingsService, VaultSession vaultSession)
    {
        InitializeComponent();
        _vaultSession = vaultSession;
        settingsService.Apply();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        window.Stopped += OnWindowStopped;
        window.Resumed += OnWindowResumed;
        return window;
    }

    private void OnWindowStopped(object? sender, EventArgs e)
    {
        if (!_vaultSession.IsUnlocked)
            return;

        _vaultSession.Lock();
        _lockedForBackground = true;
    }

    private async void OnWindowResumed(object? sender, EventArgs e)
    {
        if (!_lockedForBackground || Shell.Current is null)
            return;

        _lockedForBackground = false;
        await Shell.Current.GoToAsync("//VaultListPage");
    }
}
