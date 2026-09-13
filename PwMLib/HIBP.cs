using System;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Threading;
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
        public async Task<string> CheckIfPwnd(string password, CancellationToken cancellationToken = default)
        {
            var sha1 = Sha1Converter.Hash(password);
            var prefixHash = sha1[..5];
            var suffixHash = sha1[5..];
            var httpService = new HttpService();
            var apiReq = $"{API}{prefixHash}";
            var httpData = await httpService.GetAsync(apiReq, cancellationToken);
            return GetBreachCount(httpData, suffixHash).ToString(CultureInfo.InvariantCulture);
        }

        public static long GetBreachCount(string response, string suffixHash)
        {
            long count = 0;
            bool hasEntries = false;
            using (StringReader sr = new StringReader(response))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    int separator = line.IndexOf(':');
                    if (separator != 35 || !line[..separator].All(Uri.IsHexDigit)
                        || !long.TryParse(line[(separator + 1)..], NumberStyles.None,
                            CultureInfo.InvariantCulture, out long occurrences))
                        throw new InvalidDataException("The breach service returned an invalid response.");

                    hasEntries = true;
                    if (string.Equals(line[..separator], suffixHash, StringComparison.OrdinalIgnoreCase))
                        count = Math.Max(count, occurrences);
                }
            }
            if (!hasEntries)
                throw new InvalidDataException("The breach service returned an empty response.");
            return count;
        }
    }
}
