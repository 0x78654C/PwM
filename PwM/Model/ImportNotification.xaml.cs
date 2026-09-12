using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Windows;

namespace PwM
{
    [SupportedOSPlatform("Windows")]
    /// <summary>
    /// Interaction logic for ImportNotification.xaml
    /// </summary>
    public partial class ImportNotification : Window
    {
        public ImportNotification()
        {
            InitializeComponent();
            string vault = PwMLib.GlobalVariables.vaultName;
            vault = vault.Substring(0, vault.Length - 2);
            notificationLBL.Text = $"Vault {vault} already exists. Do you want to replace it?";
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged; // Exit vault on suspend.
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch); // Exit vault on lock screen.
            Closed += (_, _) =>
            {
                SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
                SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
            };
        }

        /// <summary>
        /// Check if PC enters sleep or hibernate mode and closes window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new System.Action(() => SystemEvents_PowerModeChanged(sender, e)));
                return;
            }
            switch (e.Mode)
            {
                case PowerModes.Suspend:
                    PwMLib.GlobalVariables.importConfirmation = false;
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
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new System.Action(() => SystemEvents_SessionSwitch(sender, e)));
                return;
            }
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                PwMLib.GlobalVariables.importConfirmation = false;
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
            PwMLib.GlobalVariables.importConfirmation = true;
            this.Close();
        }

        /// <summary>
        /// Cancel button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelBTN_Click(object sender, RoutedEventArgs e)
        {
            PwMLib.GlobalVariables.importConfirmation = false;
            this.Close();
        }
    }
}
