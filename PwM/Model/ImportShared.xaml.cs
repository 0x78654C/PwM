using Microsoft.Win32;
using PwM.Utils;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Input;

namespace PwM
{
    [SupportedOSPlatform("Windows")]
    /// <summary>
    /// Interaction logic for ImportShared.xaml
    /// </summary>
    public partial class ImportShared : Window
    {
        private readonly string _passwordManagerDirectory = PwMLib.GlobalVariables.passwordManagerDirectory;
        public ImportShared()
        {
            InitializeComponent();
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged; // Exit vault on suspend.
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch); // Exit vault on lock scre
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
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                PwMLib.GlobalVariables.importConfirmation = false;
                this.Close();
            }
        }

        private void SharedBtn_Click(object sender, RoutedEventArgs e)
        {
            ImportExport.Import(GlobalVariables.listView, _passwordManagerDirectory, true);
            this.Close();
        }

        /// <summary>
        /// Load vaults in default directory under user profile.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LocalBtn_Click(object sender, RoutedEventArgs e)
        {
            ImportExport.Import(GlobalVariables.listView, _passwordManagerDirectory, false);
            this.Close();
        }

        /// <summary>
        /// Closes the dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseLbl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            PwMLib.GlobalVariables.closeAppConfirmation = true;
            this.Close();
        }
    }
}
