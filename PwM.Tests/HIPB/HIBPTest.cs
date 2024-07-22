using PwMLib;

namespace PwM.Tests.HIPB
{
    public class HIBPTest
    {
        [Theory]
        [InlineData("pawn")]
        public void Check_API_HIBP(string password)
        {
            var hibp = new HIBP(GlobalVariables.apiHIBP);
            var totalBreaches = hibp.CheckIfPwnd(password).Result;
            bool isbreached = false;
            if (totalBreaches != "0")
                isbreached = true;
            Assert.True(isbreached);
        }
    }
}
