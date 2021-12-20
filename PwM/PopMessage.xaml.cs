using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;

namespace PwM
{
    /// <summary>
    /// Interaction logic for PopMessage.xaml
    /// </summary>
    public partial class PopMessage : Window
    {
        public PopMessage()
        {
            InitializeComponent();
            SetUI(Utils.GlobalVariables.gridColor, Utils.GlobalVariables.messageData);
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
                this.Close();
        }

        /// <summary>
        /// Close button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void confirmBTN_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        /// <summary>
        /// Setting the proper color for a specific message (error, notification, warning)
        /// </summary>
        /// <param name="gridColor"></param>
        /// <param name="messageData"></param>
        private void SetUI(string gridColor, string messageData)
        {
            switch (gridColor)
            {
                case "green":
                    popGrid.Background = Brushes.Green;
                    titleTxt.Text = "Notification";
                    notificationLBL.Text = messageData;
                    break;

                case "red":
                    popGrid.Background = Brushes.Red;
                    titleTxt.Text = "ERROR";
                    notificationLBL.Text = messageData;
                    break;

                case "orange":
                    popGrid.Background = Brushes.Orange;
                    titleTxt.Text = "WARNING";
                    notificationLBL.Text = messageData;
                    break;
            }
        }
    }
}