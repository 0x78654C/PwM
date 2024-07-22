using System;
using System.Runtime.Versioning;
using System.Windows.Controls;

namespace PwM.Utils
{
    [SupportedOSPlatform("Windows")]
    public class VaultSessionExpire
    {
        /// <summary>
        /// Load and set vault session expiration time. Default: 10 minutes.
        /// </summary>
        /// <param name="registryPath"></param>
        /// <param name="key"></param>
        /// <param name="keyValue"></param>
        /// <param name="periodBox"></param>
        public static void LoadExpireTime(string registryPath, string key, string keyValue, TextBox periodBox)
        {
            int expireTime = Int32.Parse(keyValue);
            try
            {
                string value = RegistryManagement.RegKey_Read("HKEY_CURRENT_USER\\"+registryPath, key);
                if (!string.IsNullOrEmpty(value))
                {
                    expireTime = Int32.Parse(value);
                    if (expireTime >= 1)
                    {
                        periodBox.Text = value;
                        PwMLib.GlobalVariables.vaultExpireInterval = expireTime;
                        return;
                    }
                    return;
                }
                RegistryManagement.RegKey_CreateKey(registryPath, key, keyValue);
                PwMLib.GlobalVariables.vaultExpireInterval = expireTime;
                periodBox.Text = keyValue;
                return;
            }catch
            {
                RegistryManagement.RegKey_CreateKey(registryPath, key, keyValue);
                PwMLib.GlobalVariables.vaultExpireInterval = expireTime;
                periodBox.Text = keyValue;
                Notification.ShowNotificationInfo("red", "The vault session expire time could not be read due to an error. Expire time was set to default value of 10 minutes!");
            }
        }
    }
}
