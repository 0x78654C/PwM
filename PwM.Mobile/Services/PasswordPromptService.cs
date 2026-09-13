using PwM.Mobile.Pages;

namespace PwM.Mobile.Services;

public sealed class PasswordPromptService
{
    private readonly VaultSession _vaultSession;

    public PasswordPromptService(VaultSession vaultSession) => _vaultSession = vaultSession;

    public async Task<string?> ShowAsync(string title, string message, string placeholder)
    {
        var page = new PasswordPromptPage(title, message, placeholder);
        return await ShowPageAsync(page);
    }

    public async Task ShowPasswordAsync(string password)
    {
        if (!_vaultSession.IsUnlocked) return;
        await ShowPageAsync(new PasswordPromptPage(password));
    }

    private async Task<string?> ShowPageAsync(PasswordPromptPage page)
    {
        async void OnLocked(object? sender, EventArgs e) => await page.CancelAsync();
        _vaultSession.Locked += OnLocked;
        try
        {
            await Shell.Current.Navigation.PushModalAsync(page);
            return await page.Result;
        }
        finally
        {
            _vaultSession.Locked -= OnLocked;
        }
    }
}
