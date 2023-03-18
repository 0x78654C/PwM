using Microsoft.Win32;
using System.Windows;


namespace PwM
{
    /// <summary>
    /// Interaction logic for DelApplications.xaml
    /// </summary>
    public partial class DelApplications : Window
    {
        public DelApplications()
        {
            InitializeComponent();

            string application = Utils.GlobalVariables.applicationName;
            string account = Utils.GlobalVariables.accountName;
            notificationLBL.Text = $"Do you want tot delete {account} account for {application} application?";
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
                    Utils.GlobalVariables.applicationName = "";
                    Utils.GlobalVariables.accountName = "";
                    Utils.GlobalVariables.deleteConfirmation = false;
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
                Utils.GlobalVariables.applicationName = "";
                Utils.GlobalVariables.accountName = "";
                Utils.GlobalVariables.deleteConfirmation = false;
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
            Utils.GlobalVariables.deleteConfirmation = true;
            this.Close();
        }

        private void CancelBTN_Click(object sender, RoutedEventArgs e)
        {
            Utils.GlobalVariables.applicationName = "";
            Utils.GlobalVariables.accountName = "";
            Utils.GlobalVariables.deleteConfirmation = false;
            this.Close();
        }
    }
}
