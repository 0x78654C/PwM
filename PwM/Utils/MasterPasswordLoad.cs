using System.Runtime.Versioning;
using System.Security;

namespace PwM.Utils
{
    [SupportedOSPlatform("Windows")]
    public class MasterPasswordLoad
    {
        /// <summary>
        /// Load master password form MasterPassword form after confirmation.
        /// </summary>
        /// <param name="vaultName"></param>
        /// <returns></returns>
        public static SecureString LoadMasterPassword(string vaultName)
        {
            SecureString password;
            PwMLib.GlobalVariables.vaultName = vaultName;
            MasterPassword masterPassword = new MasterPassword();
            masterPassword.ShowDialog();
            password = masterPassword.masterPassword;
            masterPassword.masterPasswordPWD.Clear();
            masterPassword.masterPassword = null;
            return password;
        }
    }
}
