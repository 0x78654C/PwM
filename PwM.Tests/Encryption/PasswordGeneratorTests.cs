namespace PwM.Tests.Encryption
{
    public class PasswordGeneratorTests
    {
        const string Numbers = "0123456789";
        const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string Symbols = "`~!@#$%^&*()-_=+[]{}\\|;:'\\,<.>/?";
        const string Lower = "abcdefghijklmnopqrstuvwxyz";

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Ensure_if_we_set_password_to_less_than_one_we_are_told_about_it(int length)
        {
            Assert.Throws<ArgumentException>(() => PwMLib.PasswordGenerator.GeneratePassword(length));
        }
        [Fact]
        public void Ensure_if_we_do_not_use_any_char_types_we_are_told_about_it()
        {
            Assert.Throws<ArgumentException>(() => PwMLib.PasswordGenerator.GeneratePassword(33, false, false, false, false));
        }

        [Theory]
        [InlineData(10)]
        [InlineData(15)]
        [InlineData(20)]
        public void Ensure_we_can_generate_a_pasword_of_length(int length)
        {
            var password = PwMLib.PasswordGenerator.GeneratePassword(length);
            Assert.Equal(length, password.Length);
        }

        [Fact]
        public void Ensure_we_can_generate_a_password_with_upper_case()
        {
            var password = PwMLib.PasswordGenerator.GeneratePassword(20, true, false, false, false);
            Assert.All(password, t => Upper.Contains(t));
        }

        [Fact]
        public void Ensure_we_can_generate_a_password_with_lower_case()
        {
            var password = PwMLib.PasswordGenerator.GeneratePassword(20, false, true, false, false);
            Assert.All(password, t => Lower.Contains(t));
        }

        [Fact]
        public void Ensure_we_can_generate_a_password_with_symbols()
        {
            var password = PwMLib.PasswordGenerator.GeneratePassword(20, false, false, true, false);
            Assert.All(password, t => Symbols.Contains(t));
        }

        [Fact]
        public void Ensure_we_can_generate_a_password_with_Numbers()
        {
            var password = PwMLib.PasswordGenerator.GeneratePassword(20, false, false, false, true);
            Assert.All(password, t => Numbers.Contains(t));
        }


        [Fact]
        public void Ensure_we_can_generate_a_withAll()
        {
            var password = PwMLib.PasswordGenerator.GeneratePassword(20);
            Assert.Contains(password, e => Lower.Contains(e));
            Assert.Contains(password, e => Upper.Contains(e));
            Assert.Contains(password, e => Symbols.Contains(e));
            Assert.Contains(password, e => Numbers.Contains(e));
        }
    }
}
