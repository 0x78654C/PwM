using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace PwM
{
    [SupportedOSPlatform("Windows")]
    /// <summary>
    /// Interaction logic for PopMessage.xaml
    /// </summary>
    public partial class PopMessage : Window
    {
        public PopMessage()
        {
            InitializeComponent();
            SetUI(PwMLib.GlobalVariables.gridColor, PwMLib.GlobalVariables.messageData);
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
                    statusIconBorder.Background = ThemeColor("#F0FDF4", "#052E16");
                    statusIcon.Foreground = ThemeColor("#16A34A", "#4ADE80");
                    statusIcon.Kind = PackIconKind.CheckCircleOutline;
                    titleTxt.Text = "Notification";
                    notificationLBL.Text = messageData;
                    break;

                case "red":
                    statusIconBorder.Background = ThemeColor("#FEF2F2", "#450A0A");
                    statusIcon.Foreground = ThemeColor("#DC2626", "#F87171");
                    statusIcon.Kind = PackIconKind.AlertCircleOutline;
                    titleTxt.Text = "Error";
                    notificationLBL.Text = messageData;
                    break;

                case "orange":
                    statusIconBorder.Background = ThemeColor("#FFFBEB", "#431407");
                    statusIcon.Foreground = ThemeColor("#D97706", "#FDBA74");
                    statusIcon.Kind = PackIconKind.AlertOutline;
                    titleTxt.Text = "Warning";
                    notificationLBL.Text = messageData;
                    break;
            }
        }

        private static SolidColorBrush ThemeColor(string lightColor, string darkColor) =>
            new((Color)ColorConverter.ConvertFromString(
                Utils.ThemeManager.IsDarkTheme ? darkColor : lightColor));
    }
}
