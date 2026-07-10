using System.Security.Cryptography;
using System.Text;

namespace PwMLib
{
    public class Sha1Converter
    {
        public static string Hash(string input)
        {
            System.ArgumentNullException.ThrowIfNull(input);
            return System.Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(input)));
        }
    }
}
