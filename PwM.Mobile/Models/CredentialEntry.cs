namespace PwM.Mobile.Models;

public class CredentialEntry
{
    public string Application { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool HasBreach { get; set; }
}
