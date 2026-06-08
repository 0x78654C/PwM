using PwM.Mobile.ViewModels;

namespace PwM.Mobile.Pages;

public partial class AddCredentialPage : ContentPage
{
    public AddCredentialPage(AddCredentialViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
