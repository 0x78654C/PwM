﻿using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Windows;


namespace PwM
{
    [SupportedOSPlatform("Windows")]
    /// <summary>
    /// Interaction logic for UpdatePassNotification.xaml
    /// </summary>
    public partial class UpdatePassNotification : Window
    {
        public UpdatePassNotification()
        {
            InitializeComponent();
            string account = PwMLib.GlobalVariables.accountName;
            notificationLBL.Text = $"Do you want tot update password for {account} account?";
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
                    PwMLib.GlobalVariables.updatePwdConfirmation = false;
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
                PwMLib.GlobalVariables.updatePwdConfirmation = false;
                this.Close();
            }
        }

        /// <summary>
        /// Close button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void confirmBTN_Click(object sender, RoutedEventArgs e)
        {
            PwMLib.GlobalVariables.updatePwdConfirmation = true;
            this.Close();
        }

        /// <summary>
        /// Cancel button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelBTN_Click(object sender, RoutedEventArgs e)
        {
            PwMLib.GlobalVariables.updatePwdConfirmation = false;
            this.Close();
        }
    }
}
