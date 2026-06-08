namespace PwM.Mobile.Models;

public class VaultInfo
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsOpen { get; set; }
}
