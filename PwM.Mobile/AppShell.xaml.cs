using PwM.Mobile.Pages;

namespace PwM.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(VaultPage), typeof(VaultPage));
        Routing.RegisterRoute(nameof(AddCredentialPage), typeof(AddCredentialPage));
    }
}
