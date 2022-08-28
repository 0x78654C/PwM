namespace PwM.Tests.Encryption.PasswordValidator
{
    public class ConvertSecureStringToStringTests
    {
        [Theory]
        [InlineData("Test32g342gh34")]
        [InlineData("LogTest321")]
        [InlineData("!fwe235423GE")]
        public void Ensure_we_build_a_secure_string(string item)
        {
            var ss = PwMLib.PasswordValidator.ConvertSecureStringToString(GetSecureString(item));
            Assert.Equal(item, ss);
        }


        private static System.Security.SecureString GetSecureString(string item)
        {
            var ss = new System.Security.SecureString();
            foreach (char c in item) { ss.AppendChar(c); }
            return ss;
        }
    }
}
