using PwM.Mobile.ViewModels;
using PwM.Mobile.Models;

namespace PwM.Mobile.Pages;

public partial class VaultPage : ContentPage
{
    private readonly VaultViewModel _vm;

    public VaultPage(VaultViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCredentialsAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.ResetAutoLockTimer();
    }

    private async void OnShowPasswordClicked(object? sender, EventArgs e)
    {
        await RunCredentialActionAsync(sender, _vm.ShowPasswordAsync);
    }

    private async void OnCopyPasswordInvoked(object? sender, EventArgs e)
    {
        await RunCredentialActionAsync(sender, _vm.CopyPasswordAsync);
    }

    private async void OnUpdatePasswordInvoked(object? sender, EventArgs e)
    {
        await RunCredentialActionAsync(sender, _vm.UpdatePasswordAsync);
    }

    private async void OnDeleteCredentialInvoked(object? sender, EventArgs e)
    {
        await RunCredentialActionAsync(sender, _vm.DeleteCredentialAsync);
    }

    private async Task RunCredentialActionAsync(object? sender, Func<CredentialEntry?, Task> action)
    {
        var entry = sender switch
        {
            Button button => button.CommandParameter as CredentialEntry
                             ?? button.BindingContext as CredentialEntry,
            SwipeItem swipeItem => swipeItem.CommandParameter as CredentialEntry
                                   ?? swipeItem.BindingContext as CredentialEntry,
            _ => null
        };
        await action(entry);
    }
}
