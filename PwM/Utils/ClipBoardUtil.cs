using TextCopy;

namespace PwM.Utils
{
    public class ClipBoardUtil
    {
        /// <summary>
        /// Check if password is copied on clipboard and clear if true only. 
        /// </summary>
        /// <param name="accPassword"></param>
        public static void ClearClipboard(string accPassword)
        {
            if (ClipboardService.GetText() == accPassword)
            {
                ClipboardService.SetText("");
                GlobalVariables.accountPassword = "";
            }
        }
    }
}
