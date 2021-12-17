using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Web.Script.Serialization;
using System.Windows.Controls;

namespace PwM.Utils
{
    /* Applicaiton tab management class */
    public class AppManagement
    {
        private static JavaScriptSerializer s_serializer;
        public static SecureString vaultSecure = null;
        private static string passMask = "\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022";

        /// <summary>
        /// Decrypt vault and populate applist with applicaitons info.
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="vaultName"></param>
        /// <param name="masterPassword"></param>
        /// <returns></returns>
        public static bool DecryptAndPopulateList(ListView listView, string vaultName, SecureString masterPassword)
        {
            try
            {
                listView.Items.Clear();
                string pathToVault = Path.Combine(GlobalVariables.passwordManagerDirectory, $"{vaultName}.x");
                if (!File.Exists(pathToVault))
                {
                    Notification.ShowNotificationInfo("red", $"Vault {vaultName} does not exist!");
                    return false;
                }
                if (masterPassword == null)
                {
                    Notification.ShowNotificationInfo("red", "Something went wrong. Check master password or vault name!");
                    return false;
                }
                string readVault = File.ReadAllText(pathToVault);
                string decryptVault = Encryption.AES.Decrypt(readVault, Encryption.PasswordValidator.ConvertSecureStringToString(masterPassword));
                if (decryptVault.Contains("Error decrypting"))
                {
                    Notification.ShowNotificationInfo("red", "Something went wrong. Check master password or vault name!");
                    return false;
                }
                vaultSecure = Encryption.PasswordValidator.StringToSecureString(decryptVault);
                using (var reader = new StringReader(decryptVault))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Length > 0)
                        {
                            s_serializer = new JavaScriptSerializer();
                            var outJson = s_serializer.Deserialize<Dictionary<string, string>>(line);
                            listView.Items.Add(new { Application = outJson["site/application"], Account = outJson["account"], Password = passMask });
                        }
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Notification.ShowNotificationInfo("red", e.Message);
                return false;
            }
        }

        /// <summary>
        /// Add application to vault.
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="vaultName"></param>
        /// <param name="application"></param>
        /// <param name="accountName"></param>
        /// <param name="accountPassword"></param>
        /// <param name="masterPassword"></param>
        public static void AddApplication(ListView listView, string vaultName, string application, string accountName, string accountPassword, SecureString masterPassword)
        {
            string pathToVault = Path.Combine(Utils.GlobalVariables.passwordManagerDirectory, $"{vaultName}.x");

            foreach(var item in listView.Items)
            {
               if(item.ToString().Contains(application) && item.ToString().Contains(accountName))
                {
                    Notification.ShowNotificationInfo("orange", $"Application {application} already contins {accountName} account!");
                    return;
                }    
            }
            if (!File.Exists(pathToVault))
            {
                Notification.ShowNotificationInfo("red", $"Vault {vaultName} does not exist!");
                return;
            }
            if (masterPassword == null)
            {
                Notification.ShowNotificationInfo("red", "Something went wrong. Check master password or vault name!");
                ClearVariables.VariablesClear();
                return;
            }
            string readVault = File.ReadAllText(pathToVault);
            string decryptVault = Encryption.AES.Decrypt(readVault, Encryption.PasswordValidator.ConvertSecureStringToString(masterPassword));
            if (decryptVault.Contains("Error decrypting"))
            {
                Notification.ShowNotificationInfo("red", "Something went wrong. Check master password or vault name!");
                return;
            }

            if (accountName.Length < 3)
            {
                Notification.ShowNotificationInfo("orange", "The length of account name should be at least 3 characters!");
                return;
            }

            var keyValues = new Dictionary<string, object>
                {
                    { "site/application", application },
                    { "account", accountName },
                    { "password", accountPassword },
                };
            s_serializer = new JavaScriptSerializer();
            string encryptdata = Encryption.AES.Encrypt(decryptVault + "\n" + s_serializer.Serialize(keyValues), Encryption.PasswordValidator.ConvertSecureStringToString(masterPassword));
            vaultSecure = Encryption.PasswordValidator.StringToSecureString(decryptVault + "\n" + s_serializer.Serialize(keyValues));
            if (File.Exists(pathToVault))
            {
                File.WriteAllText(pathToVault, encryptdata);
                Notification.ShowNotificationInfo("green", $"Data for { application} is encrypted and added to vault!");
                listView.Items.Add(new { Application = application, Account = accountName, Password = passMask });
                return;
            }
            Notification.ShowNotificationInfo("red", $"Vault {vaultName} does not exist!");
        }

        /// <summary>
        /// Delete applicaiton from vault.
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="vaultName"></param>
        /// <param name="application"></param>
        /// <param name="accountName"></param>
        /// <param name="masterPassword"></param>
        public static void DeleteApplicaiton(ListView listView, string vaultName, string application, string accountName, SecureString masterPassword)
        {
            List<string> listApps = new List<string>();
            bool accountCheck = false;
            string pathToVault = Path.Combine(GlobalVariables.passwordManagerDirectory, $"{vaultName}.x");
            if (masterPassword == null)
            {
                Notification.ShowNotificationInfo("red", "Something went wrong. Check master password or vault name!");
                ClearVariables.VariablesClear();
                return;
            }
            if (!File.Exists(pathToVault))
            {
                Notification.ShowNotificationInfo("red", $"Vault {vaultName} does not exist!");
                return;
            }
            string readVault = File.ReadAllText(pathToVault);
            string decryptVault = Encryption.AES.Decrypt(readVault, Encryption.PasswordValidator.ConvertSecureStringToString(masterPassword));
            if (decryptVault.Contains("Error decrypting"))
            {
                Notification.ShowNotificationInfo("red", "Something went wrong. Check master password or vault name!");
                return;
            }
            if (accountName.Length < 3)
            {
                Notification.ShowNotificationInfo("orange", "The length of account name should be at least 3 characters!");
                return;
            }

            if (!decryptVault.Contains(application))
            {
                Notification.ShowNotificationInfo("orange", $"Application {application} does not exist!");
                return;
            }
            s_serializer = new JavaScriptSerializer();
            using (var reader = new StringReader(decryptVault))
            {
                string line;
                listView.Items.Clear();
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length > 0)
                    {
                        listApps.Add(line);
                        s_serializer = new JavaScriptSerializer();
                        var outJson = s_serializer.Deserialize<Dictionary<string, string>>(line);
                        if (outJson["site/application"] == application && outJson["account"] == accountName)
                        {
                            listApps.Remove(line);
                            accountCheck = true;
                        }
                        else
                        {
                            listView.Items.Add(new { Application = outJson["site/application"], Account = outJson["account"], Password = passMask });
                        }
                    }
                }
                string encryptdata = Encryption.AES.Encrypt(string.Join("\n", listApps), Encryption.PasswordValidator.ConvertSecureStringToString(masterPassword));
                vaultSecure = Encryption.PasswordValidator.StringToSecureString(string.Join("\n", listApps));

                listApps.Clear();
                if (File.Exists(pathToVault))
                {
                    if (accountCheck)
                    {
                        File.WriteAllText(pathToVault, encryptdata);
                        Notification.ShowNotificationInfo("green", $"Account {accountName} for {application} was deleted");
                        return;
                    }
                    Notification.ShowNotificationInfo("orange", $"Account {accountName} does not exist!");
                    return;
                }
                Notification.ShowNotificationInfo("red", $"Vault {vaultName} does not exist!");
            }
        }

        /// <summary>
        /// Update applicaiton account password.
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="vaultName"></param>
        /// <param name="application"></param>
        /// <param name="accountName"></param>
        /// <param name="password"></param>
        /// <param name="masterPassword"></param>
        public static void UpdateAccount(ListView listView, string vaultName, string application, string accountName, string password, SecureString masterPassword)
        {
            List<string> listApps = new List<string>();
            bool accountCheck = false;
            string pathToVault = Path.Combine(GlobalVariables.passwordManagerDirectory, $"{vaultName}.x");
            if (!File.Exists(pathToVault))
            {
                Notification.ShowNotificationInfo("red", $"Vault {vaultName} does not exist!");
                return;
            }
            if (masterPassword == null)
            {
                Notification.ShowNotificationInfo("red", "Something went wrong. Check master password or vault name!");
                ClearVariables.VariablesClear();
                return;
            }
            string readVault = File.ReadAllText(pathToVault);
            string decryptVault = Encryption.AES.Decrypt(readVault, Encryption.PasswordValidator.ConvertSecureStringToString(masterPassword));
            if (decryptVault.Contains("Error decrypting"))
            {
                Notification.ShowNotificationInfo("red", "Something went wrong. Check master password or vault name!");
                return;
            }
            if (accountName.Length < 3)
            {
                Notification.ShowNotificationInfo("orange", "The length of account name should be at least 3 characters!");
                return;
            }
            if (password.Length < 3)
            {
                Notification.ShowNotificationInfo("orange", "New password field should not be empty!");
                return;
            }

            if (!decryptVault.Contains(application))
            {
                Notification.ShowNotificationInfo("orange", $"Application {application} does not exist!");
                return;
            }
            using (var reader = new StringReader(decryptVault))
            {
                string line;
                listView.Items.Clear();
                while ((line = reader.ReadLine()) != null)
                {
                    s_serializer = new JavaScriptSerializer();
                    if (line.Length > 0)
                    {
                        var outJson = s_serializer.Deserialize<Dictionary<string, string>>(line);
                        if (outJson["site/application"] == application && outJson["account"] == accountName)
                        {
                            var keyValues = new Dictionary<string, object>
                            {
                                 { "site/application", application },
                                 { "account", accountName },
                                 { "password", password },
                            };
                            accountCheck = true;
                            listApps.Add(s_serializer.Serialize(keyValues));
                            listView.Items.Add(new { Application = outJson["site/application"], Account = outJson["account"], Password = passMask });
                        }
                        else
                        {
                            listApps.Add(line);
                            listView.Items.Add(new { Application = outJson["site/application"], Account = outJson["account"], Password = passMask });
                        }
                    }
                }
                string encryptdata = Encryption.AES.Encrypt(string.Join("\n", listApps), Encryption.PasswordValidator.ConvertSecureStringToString(masterPassword));
                vaultSecure = Encryption.PasswordValidator.StringToSecureString(string.Join("\n", listApps));
                listApps.Clear();
                if (File.Exists(pathToVault))
                {
                    if (accountCheck)
                    {
                        File.WriteAllText(pathToVault, encryptdata);
                        Notification.ShowNotificationInfo("green", $"Password for account {accountName} for {application} application was updated!");
                        return;
                    }
                    Notification.ShowNotificationInfo("orange", $"Account {accountName} does not exist!");
                    return;
                }
                Notification.ShowNotificationInfo("red", $"Vault {vaultName} does not exist!");
            }
        }

        /// <summary>
        /// Show password function for right click context menu event on applist.
        /// </summary>
        /// <param name="listView"></param>
        public static void ShowPassword(ListView listView)
        {
            ListView tempListView = new ListView();
            if (listView.SelectedItem == null)
            {
                Notification.ShowNotificationInfo("orange", "You must select an application line to show the account password!");
                return;
            }
            string selectedItem = listView.SelectedItem.ToString();
            selectedItem = selectedItem.Replace($", Password = {passMask} " + "}", string.Empty);
            selectedItem = selectedItem.Replace("{ Application = ", string.Empty);
            selectedItem = selectedItem.Replace(", Account = ", "|");
            var parsedData = selectedItem.Split('|');
            var vault = Encryption.PasswordValidator.ConvertSecureStringToString(vaultSecure);
            if (CountLines(vault) >= 2)
            {
                var vaultToLines = Encryption.PasswordValidator.ConvertSecureStringToString(vaultSecure).Split(new[] { '\r', '\n' });
                foreach (var line in vaultToLines)
                {
                    s_serializer = new JavaScriptSerializer();
                    if (!string.IsNullOrEmpty(line))
                    {
                        var outJson = s_serializer.Deserialize<Dictionary<string, string>>(line);
                        if (outJson["site/application"] == parsedData[0] && outJson["account"] == parsedData[1])
                        {
                            tempListView.Items.Add(new { Application = outJson["site/application"], Account = outJson["account"], Password = outJson["password"] });
                        }
                        else
                        {
                            tempListView.Items.Add(new { Application = outJson["site/application"], Account = outJson["account"], Password = passMask });
                        }
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(vault))
                {
                    s_serializer = new JavaScriptSerializer();
                    var outJson = s_serializer.Deserialize<Dictionary<string, string>>(vault);
                    if (outJson["site/application"] == parsedData[0] && outJson["account"] == parsedData[1])
                    {
                        tempListView.Items.Add(new { Application = outJson["site/application"], Account = outJson["account"], Password = outJson["password"] });
                    }
                    else
                    {
                        tempListView.Items.Add(new { Application = outJson["site/application"], Account = outJson["account"], Password = passMask });
                    }
                }
            }
            listView.Items.Clear();
            foreach (var item in tempListView.Items)
            {
                listView.Items.Add(item);
            }
            tempListView.Items.Clear();
        }

        /// <summary>
        /// Count lines in vault file.
        /// </summary>
        /// <param name="vaultData"></param>
        /// <returns></returns>
        private static int CountLines(string vaultData)
        {
            var vaultToLines = vaultData.Split(new[] { '\r', '\n' });
            int count = 0;
            foreach (var line in vaultToLines)
            {
                if (!string.IsNullOrEmpty(line))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Copy password to clipboard function for right click context menu event.
        /// </summary>
        /// <param name="listView"></param>
        /// <returns></returns>
        public static string CopyPassToClipBoard(ListView listView)
        {
            string outPass = string.Empty;
            if (listView.SelectedItem == null)
            {
                Notification.ShowNotificationInfo("orange", "You must select an application line to copy account password!");
                outPass = "itemerror";
            }
            else
            {
                string selectedItem = listView.SelectedItem.ToString();
                string application = selectedItem.SplitByText(", ", 0).Replace("{ Application = ", string.Empty);
                string account = selectedItem.SplitByText(", ", 1).Replace("Account = ", string.Empty);
                var vaultToLines = Encryption.PasswordValidator.ConvertSecureStringToString(vaultSecure).Split(new[] { '\r', '\n' });
                foreach (var line in vaultToLines)
                {
                    s_serializer = new JavaScriptSerializer();
                    if (line.Length > 0)
                    {
                        var outJson = s_serializer.Deserialize<Dictionary<string, string>>(line);
                        if (outJson["site/application"] == application && outJson["account"] == account)
                        {
                            outPass = outJson["password"];
                            Notification.ShowNotificationInfo("green", $"Password for {account} is copied to clipboard!");
                        }
                    }
                }
            }
            return outPass;
        }

        /// <summary>
        /// Delete vault from selected item.
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="vaultName"></param>
        public static void DeleteSelectedItem(ListView listView, string vaultName)
        {
            if (listView.SelectedItem == null)
            {
                Notification.ShowNotificationInfo("orange", "You must select an application for delete!");
                return;
            }
            else
            {
                string selectedItem = listView.SelectedItem.ToString();
                string application = selectedItem.SplitByText(", ", 0).Replace("{ Application = ", string.Empty);
                string account = selectedItem.SplitByText(", ", 1).Replace("Account = ", string.Empty);
                Utils.ClearVariables.VariablesClear();
                DelApplications delApplications = new DelApplications();
                delApplications.ShowDialog();
                if (GlobalVariables.deleteConfirmation == "yes")
                {
                    var masterPassword = MasterPasswordLoad.LoadMasterPassword(vaultName);
                    DeleteApplicaiton(listView, vaultName, application, account, masterPassword);
                    Utils.ClearVariables.VariablesClear();
                }
            }
        }
    }
}