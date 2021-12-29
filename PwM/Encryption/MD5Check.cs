using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PwM.Encryption
{
    public class MD5Check
    {
        /// <summary>
        /// Check MD5 CheckSum of a file.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string MD5CheckSum(string file)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(file))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}
