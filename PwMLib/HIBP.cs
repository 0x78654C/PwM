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
        public HIBP()
        {
            API = "https://api.pwnedpasswords.com/range/";
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
            var suffixHash = sha1.Substring(sha1.Length-5);
            var httpService = new HttpService();
            var apiReq = $"{API}{prefixHash}";
            var httpData = await httpService.GetAsync(apiReq);
            var countBreachs = "0";
            using (StringReader sr = new StringReader(httpData))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var lineSplit = line.Split(':');
                    if (lineSplit[0].EndsWith(suffixHash))
                        countBreachs = lineSplit[1];
                }
            }
            return countBreachs;
        }
    }
}
