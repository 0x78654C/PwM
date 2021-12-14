using System.Windows.Controls;

namespace PwM.Utils
{
    /* Textbox and password box checks and cleaners class.*/
    public class TextPassBoxChanges
    {
        /// <summary>
        /// Check password and textboxess length and enable button if greater than 0.
        /// </summary>
        /// <param name="application"></param>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <param name="button"></param>
        public static void TextPassBoxChanged(TextBox application, TextBox account, PasswordBox password, Button button)
        {
            if (application.Text.Length > 0 && account.Text.Length > 0 && password.Password.Length > 0)
            {
                button.IsEnabled = true;
            }
            else
            {
                button.IsEnabled = false;
            }
        }

        /// <summary>
        /// Check password and textboxess length and enable button if greater than 0.
        /// </summary>
        /// <param name="application"></param>
        /// <param name="account"></param>
        /// <param name="button"></param>
        public static void TextPassBoxChanged(TextBox application, TextBox account, Button button)
        {
            if (application.Text.Length > 0 && account.Text.Length > 0)
            {
                button.IsEnabled = true;
            }
            else
            {
                button.IsEnabled = false;
            }
        }

        /// <summary>
        /// Clear textboxes and password boxes.
        /// </summary>
        /// <param name="applicaiton"></param>
        /// <param name="account"></param>
        /// <param name="password"></param>
        public static void ClearTextPassBox(TextBox applicaiton, TextBox account, PasswordBox password)
        {
            applicaiton.Clear();
            account.Clear();
            password.Clear();
        }

        /// <summary>
        /// Clear textboxes and password boxes.
        /// </summary>
        /// <param name="applicaiton"></param>
        /// <param name="account"></param>
        public static void ClearTextPassBox(TextBox applicaiton, TextBox account)
        {
            applicaiton.Clear();
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
            //textBox.Focus();
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
            //  passwordBox.Focus();
        }
    }
}