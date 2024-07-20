using Microsoft.Win32;
using PwMLib;
using System.ComponentModel;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PwM
{
    [SupportedOSPlatform("Windows")]
    /// <summary>
    /// Interaction logic for AddApplications.xaml
    /// </summary>
    public partial class AddApplications : Window
    {
        private BackgroundWorker _worker;
        private string _breaches = "";
        Network network = new Network(PwMLib.GlobalVariables.apiHIBPMain);
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
        /// Add application button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addAppBTN_Click(object sender, RoutedEventArgs e)
        {
            PwMLib.GlobalVariables.applicationName = appNameTXT.Text;
            PwMLib.GlobalVariables.accountName = accountNameTXT.Text;
            PwMLib.GlobalVariables.accountPassword = accPasswordBox.Password;
            PwMLib.GlobalVariables.closeAppConfirmation = false;
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
            PwMLib.GlobalVariables.closeAppConfirmation = true;
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
        /// Password generator for new added application accounts.
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
            if (network.PingHost())
            {
                _worker = new BackgroundWorker();
                _worker.DoWork += BreackCheck_BW;
                _worker.RunWorkerCompleted += BreackCheck_RunWorkerCompleted;
                _worker.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Set visibility if password breaches are found.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BreackCheck_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_breaches == "0")
                breachLbl.Visibility = Visibility.Hidden;
            else
                breachLbl.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Get password breaches.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BreackCheck_BW(object sender, DoWorkEventArgs e)
        {
            var hibp = new HIBP(PwMLib.GlobalVariables.apiHIBP);
            if (!string.IsNullOrEmpty(accPasswordBox.Password))
            {
                _breaches = hibp.CheckIfPwnd(accPasswordBox.Password).Result;
            }
            else
                _breaches = "0";
        }

        /// <summary>
        /// Hide master password when mouse is moved over from eye icon.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowPassword_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Utils.TextPassBoxChanges.HidePassword(accPasswordBox, PasswordShow);
        }
        //-----------------------------
    }
}
