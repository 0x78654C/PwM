using System.Windows;
using System.Windows.Input;

namespace PwM
{
    /// <summary>
    /// Interaction logic for UpdateApplication.xaml
    /// </summary>
    public partial class UpdateApplication : Window
    {
        public UpdateApplication()
        {
            InitializeComponent();
            AccountNameTXT.Text = Utils.GlobalVariables.accountName;
            ApplicationNameTXT.Text = Utils.GlobalVariables.applicationName;
        }

        /// <summary>
        /// Mouse window drag function
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        /// <summary>
        /// Close button label.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeLBL_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Utils.GlobalVariables.closeAppConfirmation = "yes";
            this.Close();
        }

        /// <summary>
        /// Label button function for minimiza window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miniMizeLBL_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Show/hide master password from update account passwordbox using a textbox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowHideNewPassword(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                Utils.TextPassBoxChanges.ShowPassword(newPassAccBox, NewPasswordShow);
            }
            else if (e.ButtonState == MouseButtonState.Released)
            {
                Utils.TextPassBoxChanges.HidePassword(newPassAccBox, NewPasswordShow);
            }
        }

        /// <summary>
        /// Password generator for updated accounts password.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GenerateNewPassAcc_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Encryption.PasswordValidator.GeneratePassword(newPassAccBox);
        }

        /// <summary>
        /// Update account password from a applicaiton.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void updateAccPassBTN_Click(object sender, RoutedEventArgs e)
        {
            Utils.GlobalVariables.accountPassword = newPassAccBox.Password;
            this.Close();
        }

        private void newPassAccBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            updateAccPassBTN.IsEnabled = (newPassAccBox.Password.Length > 0) ? true : false;
        }
    }
}
