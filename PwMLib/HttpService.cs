using System.Net.Http;
using System.Threading.Tasks;

namespace PwMLib
{
    public class HttpService
    {
        private readonly HttpClient _client;

        public HttpService()
        {
            _client = new HttpClient();
        }

        /// <summary>
        /// GET request on custom API.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public async Task<string> GetAsync(string uri)
        {
            using HttpResponseMessage response = await _client.GetAsync(uri);
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
    }
}
