using System.Collections.Concurrent;

namespace PwM.Mobile.Services;

/// <summary>
/// Checks passwords against the HaveIBeenPwned k-anonymity API.
/// </summary>
public class HibpService
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(4)
    };
    private readonly ConcurrentDictionary<string, Task<bool>> _checks =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _requestLimit = new(4);

    public async Task<bool> IsBreachedAsync(
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        var hash = PwMLib.Sha1Converter.Hash(password);
        var check = _checks.GetOrAdd(hash, CheckHashAsync);

        try
        {
            return await check.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            _checks.TryRemove(hash, out _);
            return false;
        }
    }

    private async Task<bool> CheckHashAsync(string hash)
    {
        await _requestLimit.WaitAsync();
        try
        {
            var prefix = hash[..5];
            var suffix = hash[5..];
            var response = await _httpClient.GetStringAsync(
                $"{PwMLib.GlobalVariables.apiHIBP}{prefix}");

            using var reader = new StringReader(response);
            while (reader.ReadLine() is { } line)
            {
                var separator = line.IndexOf(':');
                if (separator > 0 &&
                    string.Equals(line[..separator], suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            _requestLimit.Release();
        }
    }
}
