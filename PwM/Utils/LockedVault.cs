using System.IO;
using System.Runtime.Versioning;

namespace PwM.Utils
{
    internal class LockedVault
    {
        [SupportedOSPlatform("Windows")]
        /// <summary>
        /// Check if vault is locked by existing lockedUser file.
        /// </summary>
        /// <param name="lockedUser"></param>
        /// <returns></returns>
        public static bool IsVaultLocked( string vaultFullPath)
        {
            if (File.Exists(vaultFullPath))
            {
                FileInfo fileInfo = new FileInfo(vaultFullPath);
                if (fileInfo.IsLocked())
                {
                    Notification.ShowNotificationInfo("orange", "You cannot make any changes at this moment.\nThe shared vault is opened by other user.\nWait until this vault is closed!");
                    return true;
                }
            }
            return false;
        }
    }
}
