using PwM.Mobile.ViewModels;

namespace PwM.Mobile.Pages;

public partial class AddCredentialPage : ContentPage
{
    private readonly AddCredentialViewModel _vm;
    public AddCredentialPage(AddCredentialViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Activate();
    }

    protected override void OnDisappearing()
    {
        _vm.Deactivate();
        base.OnDisappearing();
    }
}
