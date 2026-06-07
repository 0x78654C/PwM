using PwMLib;

namespace PwM.Mobile.Services;

/// <summary>
/// Checks passwords against the HaveIBeenPwned k-anonymity API.
/// </summary>
public class HibpService
{
    private readonly HIBP _hibp = new(PwMLib.GlobalVariables.apiHIBP);

    public async Task<bool> IsBreachedAsync(string password)
    {
        try
        {
            var count = await _hibp.CheckIfPwnd(password);
            return int.TryParse(count.Trim(), out var n) && n > 0;
        }
        catch
        {
            return false;
        }
    }
}
