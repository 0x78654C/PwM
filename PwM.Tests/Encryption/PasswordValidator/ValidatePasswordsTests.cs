using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PwM.Tests.Encryption.PasswordValidator
{
    public class ValidatePasswordsTests
    {
        [Fact]
        public void Ensure_we_can_succeed()
        {
            Assert.True(PwMLib.PasswordValidator.ValidatePassword("Abc123%^&&2145"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Ensure_we_Fail_if_passowrd_is_null_or_empty(string item)
        {
            Assert.False(PwMLib.PasswordValidator.ValidatePassword(item));
        }

        [Theory]
        [InlineData("A", false)]
        [InlineData("£", true)]
        public void Ensure_if_we_miss_special_char_we_fail(string append, bool expected)
        {
            Assert.Equal(expected, PwMLib.PasswordValidator.ValidatePassword(append + "Abc152362cdevr34eGR"));
        }

        [Theory]
        [InlineData("A", false)]
        [InlineData("3", true)]
        public void Ensure_if_we_miss_Number_char_we_fail(string append, bool expected)
        {
            Assert.Equal(expected, PwMLib.PasswordValidator.ValidatePassword(append + "agvrberer&%*$bnNDNR"));
        }

        [Theory]
        [InlineData("abc", false)]
        [InlineData("ABC", true)]
        public void Ensure_if_we_miss_a_upper_case_char_we_fail(string append, bool expected)
        {
            Assert.Equal(expected, PwMLib.PasswordValidator.ValidatePassword(append + "agrekjjger£$^&£&$%23235"));
        }

        [Theory]
        [InlineData("ABC", false)]
        [InlineData("abc", true)]
        public void Ensure_if_we_miss_a_lower_case_char_we_fail(string append, bool expected)
        {
            Assert.Equal(expected, PwMLib.PasswordValidator.ValidatePassword(append + "AVBDSG65346346@^£$$"));
        }
    }
}
