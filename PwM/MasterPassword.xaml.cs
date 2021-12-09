using System.Security;
using System.Windows;
using System.Windows.Input;

namespace PwM
{
    /// <summary>
    /// Interaction logic for MasterPassword.xaml
    /// </summary>
    public partial class MasterPassword : Window
    {
        public SecureString masterPassword;

        public MasterPassword()
        {
            InitializeComponent();
            vaultNameLBL.Text = Utils.GlobalVariables.vaultName;
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
        /// Label button function for minimiza window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miniMizeLBL_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }


        /// <summary>
        /// Pass the secure password from passwordbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void confirmBTN_Click(object sender, RoutedEventArgs e)
        {
            if (!Encryption.PasswordValidator.ValidatePassword(masterPasswordPWD.Password))
            {
                Utils.Notification.ShowNotificationInfo("orange", "Password must be at least 12 characters, and must include at least one upper case letter, one lower case letter, one numeric digit, one special character and no space!");
                masterPasswordPWD.Clear();
                return;
            }
            masterPassword = masterPasswordPWD.SecurePassword;
            this.Close();
        }

        /// <summary>
        /// Close button label.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeLBL_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
    }
}
