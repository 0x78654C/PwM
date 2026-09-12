namespace PwM.Mobile.Services;

/// <summary>Checks complete passwords without retaining password hashes in a cache.</summary>
public class HibpService
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _requestLimit = new(4);

    public HibpService() : this(new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(4),
        MaxResponseContentBufferSize = 8 * 1024 * 1024
    }) { }

    public HibpService(HttpClient httpClient) => _httpClient = httpClient;

    // null means the check failed; it must never be presented as a clean result.
    public async Task<bool?> IsBreachedAsync(string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(password)) return null;

        await _requestLimit.WaitAsync(cancellationToken);
        try
        {
            var hash = PwMLib.Sha1Converter.Hash(password);
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"{PwMLib.GlobalVariables.apiHIBP}{hash[..5]}");
            request.Headers.Add("Add-Padding", "true");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return PwMLib.HIBP.GetBreachCount(body, hash[5..]) > 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException or InvalidDataException)
        {
            return null;
        }
        finally
        {
            _requestLimit.Release();
        }
    }
}
