using Microsoft.Win32;
using System.Windows;

namespace PwM
{
    /// <summary>
    /// Interaction logic for DeleteVault.xaml
    /// </summary>
    public partial class DeleteVault : Window
    {
        public DeleteVault()
        {
            InitializeComponent();
            string vault = Utils.GlobalVariables.vaultName;
            notificationLBL.Text = $"Do you want tot delete {vault} vault?";
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
                    Utils.GlobalVariables.deleteConfirmation = "";
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
                Utils.GlobalVariables.deleteConfirmation = "";
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
            Utils.GlobalVariables.deleteConfirmation = "yes";
            this.Close();
        }

        private void CancelBTN_Click(object sender, RoutedEventArgs e)
        {
            Utils.GlobalVariables.deleteConfirmation = "";
            this.Close();
        }
    }
}
