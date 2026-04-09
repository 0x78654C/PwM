using System.Text;

namespace PwM.Tests.Encryption
{
    public class Argon2Tests
    {
        /* we are not testing logic here just results so we can refactor or swap out and ensure backward compatiablity */
        [Theory]
        [InlineData("gweghwre52315herhrethej", "eghwre5231", "2C3D20C54D4E9BB0B4F3F8D7E7844A69065298E7893489FF84947FA033D26181")]
        [InlineData("herherhtrjrjj543y34y3", "rherhtrjrj", "9F8C2E6385CDEECC943DE947FAC7BA65A75C1B190C250173AD6E5066CB3AAAF6")]
        public void Ensure_we_can_(string inParam, string saltStr, string expectedHex)
        {
            var salt = Encoding.UTF8.GetBytes(saltStr);
            var bytes = PwMLib.Argon2.Argon2HashPassword(inParam, salt);
            string hex = BitConverter.ToString(bytes).Replace("-", "");

            Assert.Equal(expectedHex, hex);
        }
    }
}
