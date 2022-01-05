using Microsoft.Win32;
using System.Windows;
using System.Windows.Input;

namespace PwM
{
    /// <summary>
    /// Interaction logic for AddVault.xaml
    /// </summary>
    public partial class AddVault : Window
    {
        public AddVault()
        {
            InitializeComponent();
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged; // Exit vault on suspend.
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch); // Exit vault on lock screen.
        }

        /// <summary>
        /// Check if PC enters sleep or hibernate mode and closes window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Suspend:
                    Utils.GlobalVariables.closeAppConfirmation = "yes";
                    this.Close();
                    break;
            }
        }


        /// <summary>
        /// Check if lock screen and closes window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                Utils.GlobalVariables.closeAppConfirmation = "yes";
                this.Close();
            }
        }

        /// <summary>
        /// Check password length and enable create vault button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addVPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            createBTN.IsEnabled = (confirmVPassword.Password == addVPassword.Password && addVPassword.Password.Length >= 12);
        }
        /// <summary>
        /// Show/hide master password from create vault passwordbox using a textbox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowVaultPassword(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                Utils.TextPassBoxChanges.ShowPassword(addVPassword, vaultMassterPass);
                Utils.TextPassBoxChanges.ShowPassword(confirmVPassword, vaultConfirmMassterPass);
            }
            else if (e.ButtonState == MouseButtonState.Released)
            {
                Utils.TextPassBoxChanges.HidePassword(addVPassword, vaultMassterPass);
                Utils.TextPassBoxChanges.HidePassword(confirmVPassword, vaultConfirmMassterPass);
            }
        }

        private void saveBTN_Click(object sender, RoutedEventArgs e)
        {
            Utils.VaultManagement.CreateVault(vaultNameTXT.Text, addVPassword.Password, confirmVPassword.Password, Utils.GlobalVariables.passwordManagerDirectory);
            if (Utils.GlobalVariables.vaultChecks)
            {
                Utils.VaultManagement.ClearPBoxesInput(addVPassword, confirmVPassword);
                Utils.GlobalVariables.vaultChecks = false;
            }
            else
            {
                this.Close();
            }
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
        /// Check password length and enable create button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void confirmVPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            createBTN.IsEnabled = (confirmVPassword.Password == addVPassword.Password && confirmVPassword.Password.Length >= 12);
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
        /// Hide master password when mouse is moved over from eye icon.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowNewVaultPassword_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Utils.TextPassBoxChanges.HidePassword(addVPassword, vaultMassterPass);
            Utils.TextPassBoxChanges.HidePassword(confirmVPassword, vaultConfirmMassterPass);
        }
    }
}
