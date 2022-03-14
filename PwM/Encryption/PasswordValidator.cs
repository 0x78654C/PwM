using System.Security;
using System.Text.RegularExpressions;

namespace PwM.Encryption
{
    public static class PasswordValidator
    {
        /// <summary>
        /// Password complexity check: digit, upper case and lower case.
        /// </summary>
        /// <param name="password">Password string.</param>
        /// <returns>bool</returns>
        public static bool ValidatePassword(string password)
        {
            const string patternPassword = @"^(?=.*\d)(?=.*[a-z])(?=.*[A-Z]).{12,500}$";
            return !(string.IsNullOrEmpty(password) || CheckSpaceChar(password) || !Regex.IsMatch(password, patternPassword) || !SpecialCharCheck(password));
        }

        /// <summary>
        /// Check string for special character.
        /// </summary>
        /// <param name="input">Password string.</param>
        /// <returns></returns>
        private static bool SpecialCharCheck(string input)
        {
            return input.IndexOfAny(@"\|!#$%&/()=?»«@£§€{}.-;'<>_,".ToCharArray()) > -1;
        }

        /// <summary>
        /// Check for empty space in password.
        /// </summary>
        /// <param name="input">Password string.</param>
        /// <returns></returns>
        private static bool CheckSpaceChar(string input)
        {
            return input.Contains(" ");
        }

        /// <summary>
        /// Converts the secure string to string.
        /// </summary>
        /// <returns>The secure string to string.</returns>
        /// <param name="data">Data.</param>
        public static string ConvertSecureStringToString(this SecureString data)
        {
            return new System.Net.NetworkCredential(string.Empty, data).Password;
        }
        /// <summary>
        /// Convert string to secure string.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static SecureString StringToSecureString(string data)
        {
            SecureString secureString = new SecureString();
            foreach (var c in data)
            {
                secureString.AppendChar(c);
            }
            return secureString;
        }
    }
}
