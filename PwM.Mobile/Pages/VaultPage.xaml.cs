using PwM.Mobile.ViewModels;

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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadCredentials();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.ResetAutoLockTimer();
    }
}
