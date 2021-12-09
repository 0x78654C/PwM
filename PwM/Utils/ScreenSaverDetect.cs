using System.Runtime.InteropServices;

namespace PwM.Utils
{
    class ScreenSaverDetect
    {
        public static class NativeMethods
        {
            // Used to check if the screen saver is running
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SystemParametersInfo(uint uAction,
                                                           uint uParam,
                                                           ref bool lpvParam,
                                                           int fWinIni);

            // Check if the screensaver is busy running.
            public static bool IsScreensaverRunning()
            {
                const int SPI_GETSCREENSAVERRUNNING = 114;
                bool isRunning = false;

                if (!SystemParametersInfo(SPI_GETSCREENSAVERRUNNING, 0, ref isRunning, 0))
                {
                    // Could not detect screen saver status...
                    return false;
                }

                if (isRunning)
                {
                    // Screen saver is ON.
                    return true;
                }

                // Screen saver is OFF.
                return false;
            }
        }
    }
}
