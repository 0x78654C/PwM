
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;
using System.Windows.Controls;

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
            string patternPassword = @"^(?=.*\d)(?=.*[a-z])(?=.*[A-Z]).{12,500}$";
            if (!string.IsNullOrEmpty(password))
            {
                if (CheckSpaceChar(password))
                {
                    return false;
                }

                if (!Regex.IsMatch(password, patternPassword))
                {
                    return false;
                }

                if (!SpecialCharCheck(password))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Check string for special character.
        /// </summary>
        /// <param name="input">Password string.</param>
        /// <returns></returns>
        private static bool SpecialCharCheck(string input)
        {
            string specialChar = @"\|!#$%&/()=?»«@£§€{}.-;'<>_,";
            if (input.IndexOfAny(specialChar.ToCharArray()) > -1)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check for empty space in password.
        /// </summary>
        /// <param name="input">Password string.</param>
        /// <returns></returns>
        private static bool CheckSpaceChar(string input)
        {
            if (input.Contains(" ")) { return true; }
            return false;
        }

        /// <summary>
        /// Converts the secure string to string.
        /// </summary>
        /// <returns>The secure string to string.</returns>
        /// <param name="data">Data.</param>
        public static string ConvertSecureStringToString(this SecureString data)
        {
            var pointer = IntPtr.Zero;
            try
            {
                pointer = Marshal.SecureStringToGlobalAllocUnicode(data);
                return Marshal.PtrToStringUni(pointer);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(pointer);
            }
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

        /// <summary>
        /// Password generator for new added applicaiton accounts.
        /// </summary>
        /// <param name="passwordBox"></param>
        public static void GeneratePassword(PasswordBox passwordBox)
        {
            passwordBox.Password =  PasswordGenerator.GeneratePassword(20);
        }
    }
}
