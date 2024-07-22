namespace PwM.Tests.Encryption
{
    public class Sha1Test
    {
        [Theory]
        [InlineData("pawn", "1245E238CD5CD7E0D3E5CF5233265A6F2095E5B0")]
        public void Ensure_we_can_(string inParam, string expected)
        {
            var hashSha1 = PwMLib.Sha1Converter.Hash(inParam);
            Assert.Equal(expected, hashSha1);
        }
    }
}
