using Microsoft.Win32;
using PwM.Encryption;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace PwM
{
    /// <summary>
    /// Interaction logic for AddApplications.xaml
    /// </summary>
    public partial class AddApplications : Window
    {
        public AddApplications()
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
        /// Add application button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addAppBTN_Click(object sender, RoutedEventArgs e)
        {
            Utils.GlobalVariables.applicationName = appNameTXT.Text;
            Utils.GlobalVariables.accountName = accountNameTXT.Text;
            Utils.GlobalVariables.accountPassword = accPasswordBox.Password;
            Utils.GlobalVariables.closeAppConfirmation = "";
            Utils.TextPassBoxChanges.ClearTextPassBox(appNameTXT, accountNameTXT, accPasswordBox);
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
        /// Show/hide master password from add new application account passwordbox using a textbox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowHidePassword(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                Utils.TextPassBoxChanges.ShowPassword(accPasswordBox, PasswordShow);
            }
            else if (e.ButtonState == MouseButtonState.Released)
            {
                Utils.TextPassBoxChanges.HidePassword(accPasswordBox, PasswordShow);
            }
        }

        /// <summary>
        /// Password generator for new added applicaiton accounts.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GeneratePassAcc_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            accPasswordBox.Password = PasswordGenerator.GeneratePassword(20);
        }

        // Password, text boxes length check and add application button enable .
        private void appNameTXT_TextChanged(object sender, TextChangedEventArgs e)
        {
            Utils.TextPassBoxChanges.TextPassBoxChanged(appNameTXT, accountNameTXT, accPasswordBox, addAppBTN);
        }

        private void accountNameTXT_TextChanged(object sender, TextChangedEventArgs e)
        {
            Utils.TextPassBoxChanges.TextPassBoxChanged(appNameTXT, accountNameTXT, accPasswordBox, addAppBTN);
        }

        private void accPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Utils.TextPassBoxChanges.TextPassBoxChanged(appNameTXT, accountNameTXT, accPasswordBox, addAppBTN);
        }
        //-----------------------------
    }
}
