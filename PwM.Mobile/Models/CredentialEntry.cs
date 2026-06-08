using CommunityToolkit.Mvvm.ComponentModel;

namespace PwM.Mobile.Models;

public partial class CredentialEntry : ObservableObject
{
    public string Application { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _hasBreach;

    [ObservableProperty]
    private bool _isBreachCheckPending = true;
}
