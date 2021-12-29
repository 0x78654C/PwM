using Xunit;

namespace Unit.Tests.Encryption
{
    public class MD5Tests
    {
        [Theory]
        [InlineData("4bf8b4ec2ad05cc0c04913d9e7c36e00", @"pwm.exe")]
        public void Md5CheckSum_Result_Test(string md5, string file)
        {
            string md5file = PwM.Encryption.MD5Check.MD5CheckSum(file);
            Assert.Equal(md5, md5file);
        }
    }
}
