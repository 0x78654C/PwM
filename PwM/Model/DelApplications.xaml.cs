using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Windows;


namespace PwM
{
    [SupportedOSPlatform("Windows")]
    /// <summary>
    /// Interaction logic for DelApplications.xaml
    /// </summary>
    public partial class DelApplications : Window
    {
        public DelApplications()
        {
            InitializeComponent();

            string application = PwMLib.GlobalVariables.applicationName;
            string account = PwMLib.GlobalVariables.accountName;
            notificationLBL.Text = $"Do you want tot delete {account} account for {application} application?";
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
                    PwMLib.GlobalVariables.applicationName = "";
                    PwMLib.GlobalVariables.accountName = "";
                    PwMLib.GlobalVariables.deleteConfirmation = false;
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
                PwMLib.GlobalVariables.applicationName = "";
                PwMLib.GlobalVariables.accountName = "";
                PwMLib.GlobalVariables.deleteConfirmation = false;
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
            PwMLib.GlobalVariables.deleteConfirmation = true;
            this.Close();
        }

        private void CancelBTN_Click(object sender, RoutedEventArgs e)
        {
            PwMLib.GlobalVariables.applicationName = "";
            PwMLib.GlobalVariables.accountName = "";
            PwMLib.GlobalVariables.deleteConfirmation = false;
            this.Close();
        }
    }
}
