using Konscious.Security.Cryptography;
using System.Text;
namespace PwM.Encryption
{
    public static class Argon2
    {

        /// <summary>
        /// Argon2 Password Hash
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public static byte[] Argon2HashPassword(string password)
        {
            byte[] bytes = new byte[32];
            using (var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = Encoding.UTF8.GetBytes(password.Substring(2, 10)),
                DegreeOfParallelism = 2,
                Iterations = 40,
                MemorySize = 4096
            })
            {
                bytes = argon2.GetBytes(32);
            }

            return bytes;
        }
    }
}
