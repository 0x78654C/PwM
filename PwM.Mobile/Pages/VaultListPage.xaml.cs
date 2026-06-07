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
}
