using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if (Mkb.ClipBoardManager.GetText() == accPassword)
            {
                Mkb.ClipBoardManager.Clear();
                Utils.GlobalVariables.accountPassword = "";
            }

        }
    }
}
