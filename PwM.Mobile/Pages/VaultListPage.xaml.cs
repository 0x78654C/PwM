using PwM.Mobile.Models;
using PwM.Mobile.ViewModels;

namespace PwM.Mobile.Pages;

public partial class VaultListPage : ContentPage
{
    private readonly VaultListViewModel _vm;

    public VaultListPage(VaultListViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadVaults();
    }

    private async void OnVaultSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var vault = e.CurrentSelection.FirstOrDefault() as VaultInfo;
        if (vault is null)
            return;

        if (sender is CollectionView collectionView)
            collectionView.SelectedItem = null;

        await _vm.OpenVaultAsync(vault);
    }

    private async void OnExportVaultInvoked(object? sender, EventArgs e)
    {
        await RunVaultActionAsync(sender, _vm.ExportVaultAsync);
    }

    private async void OnChangeMasterPasswordInvoked(object? sender, EventArgs e)
    {
        await RunVaultActionAsync(sender, _vm.ChangeMasterPasswordAsync);
    }

    private async void OnDeleteVaultInvoked(object? sender, EventArgs e)
    {
        await RunVaultActionAsync(sender, _vm.DeleteVaultAsync);
    }

    private async Task RunVaultActionAsync(object? sender, Func<VaultInfo?, Task> action)
    {
        var vault = sender switch
        {
            SwipeItem swipeItem => swipeItem.CommandParameter as VaultInfo
                                   ?? swipeItem.BindingContext as VaultInfo,
            _ => null
        };
        await action(vault);
    }
}
