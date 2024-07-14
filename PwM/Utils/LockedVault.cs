using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace PwM.Utils
{
    internal class LockedVault
    {
        /// <summary>
        /// Check if vault is locked by existing lockedUser file.
        /// </summary>
        /// <param name="lockedUser"></param>
        /// <returns></returns>
        public static bool IsVaultLocked(string lockedUser)
        {
            if (File.Exists(lockedUser))
            {
                var user = File.ReadAllText(lockedUser);
                Notification.ShowNotificationInfo("orange", $" You cannot make any changes at this moment.\nThe shared vault is opened by '{user}'. Wait until this vault is closed!");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Create locked user file with current connected user.
        /// </summary>
        /// <param name="lockedUser"></param>
        /// <param name="connectedUser"></param>
        public static void LockVault(string lockedUser, string connectedUser)
        {
            if (File.Exists(lockedUser))
                return;
            File.WriteAllText(lockedUser, connectedUser);
        }
    }
}
