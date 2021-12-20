using System.Text;
using Xunit;

namespace Unit.Tests.Encryption
{
    public class Argon2Tests
    {
        /* we are not testing logic here just results so we can refactor or swap out and ensure backward compatiablity */
        [Theory]
        [InlineData("gweghwre52315herhrethej", ",= ?MN????????JiR???4?????3?a?")]
        [InlineData("herherhtrjrjj543y34y3", "??.c?????=?G???e?\\%s?nPf?:??")]
        public void Ensure_we_can_(string inParam, string expected)
        {
            var bytes = PwM.Encryption.Argon2.Argon2HashPassword(inParam);
            string asciiString = Encoding.ASCII.GetString(bytes, 0, bytes.Length);

            Assert.Equal(expected, asciiString);
        }
    }
}
