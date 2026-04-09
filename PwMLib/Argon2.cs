using Konscious.Security.Cryptography;
using System.Text;

namespace PwMLib
{
    /*Master password hash ecnryption.*/
    public static class Argon2
    {
        public static Argon2id s_argon2;

        /// <summary>
        /// Argon2 Password Hash
        /// </summary>
        /// <param name="password"></param>
        /// <param name="salt">Random salt bytes. Must be unique per encryption operation.</param>
        /// <returns></returns>
        public static byte[] Argon2HashPassword(string password, byte[] salt)
        {
            s_argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = 2,
                Iterations = 40,
                MemorySize = 4096
            };
            return s_argon2.GetBytes(32);
        }
    }
}
