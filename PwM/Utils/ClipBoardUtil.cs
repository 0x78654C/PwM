using System.Runtime.InteropServices;
using System.Windows;

namespace PwM.Utils
{
    public class ClipBoardUtil
    {
        /// <summary>
        /// Clears the clipboard only when it still contains the copied password.
        /// </summary>
        public static void ClearClipboard(string accPassword)
        {
            try
            {
                if (Clipboard.ContainsText() && Clipboard.GetText() == accPassword)
                    Clipboard.Clear();
            }
            catch (ExternalException)
            {
                // Another process can temporarily lock the Windows clipboard.
            }
        }
    }
}
