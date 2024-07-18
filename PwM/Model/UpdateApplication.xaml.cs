using Microsoft.Win32;
using PwM.Utils;
using PwMLib;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace PwM
{
    /// <summary>
    /// Interaction logic for UpdateApplication.xaml
    /// </summary>
    public partial class UpdateApplication : Window
    {
        private BackgroundWorker _worker;
        private string _breaches = "";
        Network network = new Network(GlobalVariables.apiHIBPMain);
        public UpdateApplication()
        {
            InitializeComponent();
            AccountNameTXT.Text = Utils.GlobalVariables.accountName;
            ApplicationNameTXT.Text = Utils.GlobalVariables.applicationName;
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
                    Utils.GlobalVariables.closeAppConfirmation = true;
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
                Utils.GlobalVariables.closeAppConfirmation = true;
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
        /// Close button label.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeLBL_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Utils.GlobalVariables.closeAppConfirmation = true;
            this.Close();
        }

        /// <summary>
        /// Label button function for minimize window
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
            newPassAccBox.Password = PasswordGenerator.GeneratePassword(20);
        }

        /// <summary>
        /// Update account password from a application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void updateAccPassBTN_Click(object sender, RoutedEventArgs e)
        {
            UpdatePassNotification updatePassNotification = new UpdatePassNotification();
            updatePassNotification.ShowDialog();
            if (Utils.GlobalVariables.updatePwdConfirmation)
            {
                Utils.GlobalVariables.newAccountPassword = newPassAccBox.Password;
                this.Close();
            }
        }

        private void newPassAccBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            updateAccPassBTN.IsEnabled = (newPassAccBox.Password.Length > 0) ? true : false;
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
            var hibp = new HIBP(GlobalVariables.apiHIBP);
            if (!string.IsNullOrEmpty(newPassAccBox.Password))
            {
                _breaches = hibp.CheckIfPwnd(newPassAccBox.Password).Result;
            }
            else
                _breaches = "0";
        }

        /// <summary>
        /// Hide master password when mouse is moved over from eye icon.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowNewPassword_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Utils.TextPassBoxChanges.HidePassword(newPassAccBox, NewPasswordShow);
        }
    }
}
