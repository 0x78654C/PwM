using System;
using System.IO;
using System.Threading.Tasks;

namespace PwMLib
{
    /*
     Powerd by Have I Been Pwned. haveibeenpwned.com
     */
    public class HIBP
    {
        private readonly string API;
        public HIBP(string apiAddress)
        {
            if (!Uri.TryCreate(apiAddress, UriKind.Absolute, out Uri apiUri)
                || apiUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException("HIBP API address must be an absolute HTTPS URI.", nameof(apiAddress));
            }

            API = apiUri.AbsoluteUri;
        }

        /// <summary>
        /// Check if password was breached.
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<string> CheckIfPwnd(string password)
        {
            var sha1 = Sha1Converter.Hash(password);
            var prefixHash = sha1[..5];
            var suffixHash = sha1[5..];
            var httpService = new HttpService();
            var apiReq = $"{API}{prefixHash}";
            var httpData = await httpService.GetAsync(apiReq);
            var countBreachs = "0";
            using (StringReader sr = new StringReader(httpData))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    int separator = line.IndexOf(':');
                    if (separator <= 0)
                        continue;

                    if (string.Equals(line[..separator], suffixHash, StringComparison.OrdinalIgnoreCase))
                        countBreachs = line[(separator + 1)..].Trim();
                }
            }
            return countBreachs;
        }
    }
}
