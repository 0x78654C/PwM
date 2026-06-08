using PwM.Mobile.Pages;

namespace PwM.Mobile.Services;

public sealed class PasswordPromptService
{
    public async Task<string?> ShowAsync(string title, string message, string placeholder)
    {
        var page = new PasswordPromptPage(title, message, placeholder);
        await Shell.Current.Navigation.PushModalAsync(page);
        return await page.Result;
    }
}
