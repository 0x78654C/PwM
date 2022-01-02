using System.Windows;
using Microsoft.Win32;
using Mkb;
using System.Windows.Threading;
using System;

namespace PwM
{
    /// <summary>
    /// Interaction logic for Support.xaml
    /// </summary>
    public partial class Support : Window
    {
        DispatcherTimer dispatcherTimer = new DispatcherTimer(); 
        public Support()
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
                    Utils.GlobalVariables.updatePwdConfirmation = "";
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
                Utils.GlobalVariables.updatePwdConfirmation = "";
                this.Close();
            }
        }

        /// <summary>
        /// Closes the current window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Copy Bitcoin to clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BitCopyLbl_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ClipBoardManager.SetText(BitCoinTextBox.Text);
            HashCopyResultLbl.Content="Bitcoin address was copied!";
            StartHashLabelClean();
        }

        /// <summary>
        /// Copy ethereum to clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EthCopyLbl_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ClipBoardManager.SetText(EthereumTextBox.Text);
            HashCopyResultLbl.Content = "Ethereum address was copied!";
            StartHashLabelClean();
        }

        /// <summary>
        /// Timer for HashCopyResultLbl clear after 2 seconds.
        /// </summary>
        private void StartHashLabelClean()
        {
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 2);
            dispatcherTimer.Start();
        }

        /// <summary>
        /// HashCopyResultLbl clear function for timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            HashCopyResultLbl.Content = " ";
        }
    }
}
