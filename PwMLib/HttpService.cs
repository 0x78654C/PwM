using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PwMLib
{
    public class HttpService
    {
        private const long MaximumResponseSize = 8 * 1024 * 1024;
        private static readonly HttpClient Client = new(new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromSeconds(10),
            MaxResponseContentBufferSize = MaximumResponseSize
        };

        /// <summary>
        /// Performs a bounded HTTPS GET request for the HIBP range API.
        /// </summary>
        public async Task<string> GetAsync(string uri, CancellationToken cancellationToken = default)
        {
            if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri requestUri)
                || requestUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException("Only absolute HTTPS API addresses are allowed.", nameof(uri));
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Add("Add-Padding", "true");
            using HttpResponseMessage response = await Client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
    }
}
