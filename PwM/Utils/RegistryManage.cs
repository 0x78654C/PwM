using System.Collections.Generic;
using Microsoft.Win32;

namespace PwM.Utils
{
    public class RegistryManage
    {

        /// <summary>
        /// Registry key wirte
        /// </summary>
        /// <param name="keyPath"></param>
        /// <param name="keyName"></param>
        /// <param name="keyValue"></param>
        public static void RegKey_WriteSubkey(string keyName, string subKeyName, string subKeyValue)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
            (keyName, true);
            rk.SetValue(subKeyName, subKeyValue);
        }

        /// <summary>
        /// Registry key reader from HKEY_CURRENT_USER
        /// </summary>
        /// <param name="keyPath"></param>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public static string RegKey_Read(string keyName, string subKeyName)
        {
            string key = string.Empty;

            string InstallPath = (string)Registry.GetValue(@"HKEY_CURRENT_USER\" + keyName, subKeyName, null);
            if (InstallPath != null)
            {
                key = InstallPath;
            }
            return key;
        }

        /// <summary>
        /// Registry key reader from HKEY_LOCAL_MACHINE.
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="subKeyName"></param>
        /// <returns></returns>
        public static string RegKey_ReadMachine(string keyName, string subKeyName)
        {
            string key = string.Empty;

            string InstallPath = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\" + keyName, subKeyName, null);
            if (InstallPath != null)
            {
                key = InstallPath;
            }
            return key;
        }

        /// <summary>
        ///  Registry key create.
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="subKeyName"></param>
        /// <param name="subKeyValue"></param>

        public static void RegKey_CreateKey(string keyName, string subKeyName, string subKeyValue)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey
            (keyName);
            key.SetValue(subKeyName, subKeyValue);
            key.Close();
        }

        /// <summary>
        /// Delete a subkey by name.
        /// </summary>
        /// <param name="keyName">Main key name.</param>
        /// <param name="subKeyName">Sub key name.</param>
        public static void RegKey_Delete(string keyName, string subKeyName)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(keyName, true))
            {
                key.DeleteValue(subKeyName);
            }
        }

        /// <summary>
        /// Check regsitry if eqals a value and if not create with 0 value.
        /// </summary>
        /// <param name="regKeyList"></param>
        /// <param name="keyName"></param>
        /// <param name="subKeyValue"></param>
        public static void CheckRegKeysStart(List<string> regKeyList, string keyName, string subKeyValue, bool zero)
        {
            foreach (var key in regKeyList)
            {
                if (zero)
                {
                    if (RegKey_Read(keyName, key) == subKeyValue)
                        RegKey_CreateKey(keyName, key, "0");
                }

                if (RegKey_Read(keyName, key) == subKeyValue)
                    RegKey_CreateKey(keyName, key, subKeyValue);
            }
        }
    }
}
