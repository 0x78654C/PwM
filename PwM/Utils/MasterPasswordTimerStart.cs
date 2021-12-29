using System;
using System.Windows.Threading;

namespace PwM.Utils
{
    public class MasterPasswordTimerStart
    {

        /// <summary>
        /// Master password timer check for promt every 30 minutes the master password pop window.
        /// </summary>
        public static void MasterPasswordCheck_TimerStart(DispatcherTimer masterPaswordTimer)
        {
            masterPaswordTimer.Tick += MasterPasswordCheck_Tick;
            masterPaswordTimer.Interval = new TimeSpan(0, 30, 0);
            masterPaswordTimer.Start();
        }

        /// <summary>
        /// Stop master password timer check for promt every 30 minutes the master password pop window.
        /// </summary>
        /// <param name="masterPaswordTimer"></param>
        public static void MasterPasswordCheck_TimerStop(DispatcherTimer masterPaswordTimer)
        {
            if (masterPaswordTimer.IsEnabled)
                masterPaswordTimer.Stop();
        }

        /// <summary>
        /// Set flag for master password timer check.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void MasterPasswordCheck_Tick(object sender, EventArgs e)
        {
            GlobalVariables.masterPasswordCheck = false;
        }
    }
}
