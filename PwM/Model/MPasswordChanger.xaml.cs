using Microsoft.Win32;
using System.Windows;
using System.Windows.Input;

namespace PwM
{
    /// <summary>
    /// Interaction logic for MPasswordChanger.xaml
    /// </summary>
    public partial class MPasswordChanger : Window
    {
        public MPasswordChanger()
        {
            InitializeComponent();
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged; // Exit vault on suspend.
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch); // Exit vault on lock screen.
            vaultNameTB.Text = PwMLib.GlobalVariables.vaultName;
            PwMLib.GlobalVariables.closeAppConfirmation = false;
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
                    PwMLib.GlobalVariables.closeAppConfirmation = true;
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
                PwMLib.GlobalVariables.closeAppConfirmation = true;
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
            createBTN.IsEnabled = (OldMasterPassword.Password.Length >= 12 && NewMasterPassword.Password == ConfirmNewMasterPassword.Password && ConfirmNewMasterPassword.Password.Length >= 12);
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
                Utils.TextPassBoxChanges.ShowPassword(OldMasterPassword, OldMasterPasswordTXT);
                Utils.TextPassBoxChanges.ShowPassword(NewMasterPassword, NewMasterPassTXT);
                Utils.TextPassBoxChanges.ShowPassword(ConfirmNewMasterPassword, ConfirmNewMasterPassTXT);
            }
            else if (e.ButtonState == MouseButtonState.Released)
            {
                Utils.TextPassBoxChanges.HidePassword(OldMasterPassword, OldMasterPasswordTXT);
                Utils.TextPassBoxChanges.HidePassword(NewMasterPassword, NewMasterPassTXT);
                Utils.TextPassBoxChanges.HidePassword(ConfirmNewMasterPassword, ConfirmNewMasterPassTXT);
            }
        }

        private void saveBTN_Click(object sender, RoutedEventArgs e)
        {
            PwMLib.GlobalVariables.masterPassword = OldMasterPassword.SecurePassword;
            PwMLib.GlobalVariables.newMasterPassword = NewMasterPassword.SecurePassword;
            this.Close();
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
            createBTN.IsEnabled = (OldMasterPassword.Password.Length >= 12 && NewMasterPassword.Password == ConfirmNewMasterPassword.Password && ConfirmNewMasterPassword.Password.Length >= 12);
        }

        /// <summary>
        /// Close button label.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeLBL_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            PwMLib.GlobalVariables.closeAppConfirmation = true;
            this.Close();
        }

        /// <summary>
        /// Hide master password when mouse is moved over from eye icon.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowNewVaultPassword_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Utils.TextPassBoxChanges.HidePassword(OldMasterPassword, OldMasterPasswordTXT);
            Utils.TextPassBoxChanges.HidePassword(NewMasterPassword, NewMasterPassTXT);
            Utils.TextPassBoxChanges.HidePassword(ConfirmNewMasterPassword, ConfirmNewMasterPassTXT);
        }

        /// <summary>
        ///  Check old password length and enable create button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OldMasterPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            createBTN.IsEnabled = (OldMasterPassword.Password.Length >= 12 && NewMasterPassword.Password == ConfirmNewMasterPassword.Password && ConfirmNewMasterPassword.Password.Length >= 12);
        }
    }
}
