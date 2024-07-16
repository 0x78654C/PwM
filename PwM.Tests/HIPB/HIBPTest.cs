using PwMLib;

namespace PwM.Tests.HIPB
{
    public class HIBPTest
    {
        [Theory]
        [InlineData("pawn","336")]
        public void Check_API_HIBP(string password, string tBreaches)
        {
            var hibp = new HIBP();
            var totalBreaches = hibp.CheckIfPwnd(password);
            Assert.Equal(totalBreaches,tBreaches);
        }
    }
}
