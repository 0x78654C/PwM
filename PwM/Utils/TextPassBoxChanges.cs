using System.Windows.Controls;

namespace PwM.Utils
{
    /* Textbox and password box checks and cleaners class.*/
    public class TextPassBoxChanges
    {
        /// <summary>
        /// Check password and textboxes length and enable button if greater than 0.
        /// </summary>
        /// <param name="application"></param>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <param name="button"></param>
        public static void TextPassBoxChanged(TextBox application, TextBox account, PasswordBox password, Button button)
        {
            button.IsEnabled = (application.Text.Length > 0 && account.Text.Length > 0 && password.Password.Length > 0);
        }

        /// <summary>
        /// Check password and textboxes length and enable button if greater than 0.
        /// </summary>
        /// <param name="application"></param>
        /// <param name="account"></param>
        /// <param name="button"></param>
        public static void TextPassBoxChanged(TextBox application, TextBox account, Button button)
        {
            button.IsEnabled = (application.Text.Length > 0 && account.Text.Length > 0);
        }

        /// <summary>
        /// Clear textboxes and password boxes.
        /// </summary>
        /// <param name="application"></param>
        /// <param name="account"></param>
        /// <param name="password"></param>
        public static void ClearTextPassBox(TextBox application, TextBox account, PasswordBox password)
        {
            application.Clear();
            account.Clear();
            password.Clear();
        }

        /// <summary>
        /// Clear textboxes and password boxes.
        /// </summary>
        /// <param name="application"></param>
        /// <param name="account"></param>
        public static void ClearTextPassBox(TextBox application, TextBox account)
        {
            application.Clear();
            account.Clear();
        }
        /// <summary>
        /// Hide password from password box and show in text box.
        /// </summary>
        /// <param name="passwordBox"></param>
        /// <param name="textBox"></param>
        public static void ShowPassword(PasswordBox passwordBox, TextBox textBox)
        {
            passwordBox.Visibility = System.Windows.Visibility.Collapsed;
            textBox.Visibility = System.Windows.Visibility.Visible;
            textBox.Text = passwordBox.Password;
        }

        /// <summary>
        /// Hide password from text box and show in password box.
        /// </summary>
        /// <param name="passwordBox"></param>
        /// <param name="textBox"></param>
        public static void HidePassword(PasswordBox passwordBox, TextBox textBox)
        {
            passwordBox.Visibility = System.Windows.Visibility.Visible;
            textBox.Visibility = System.Windows.Visibility.Collapsed;
            textBox.Clear();
        }

        /// <summary>
        /// Clear PasswordBoxes input.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="confirmPassword"></param>
        public static void ClearPBoxesInput(PasswordBox password, PasswordBox confirmPassword)
        {
            password.Clear();
            confirmPassword.Clear();
        }

        /// <summary>
        /// Clear PasswordBoxes input.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="confirmPassword"></param>
        public static void ClearPBoxesInput(PasswordBox oldPassword, PasswordBox newPassword, PasswordBox confirmPassword)
        {
            oldPassword.Clear();
            newPassword.Clear();
            confirmPassword.Clear();
        }
    }
}