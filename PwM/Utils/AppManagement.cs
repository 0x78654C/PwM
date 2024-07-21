using PwMLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Security;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Linq;

namespace PwM.Utils
{
    [SupportedOSPlatform("Windows")]
    /* Application tab management class */
    public class AppManagement
    {
        public static SecureString vaultSecure = null;
        private static string passMask = "\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022";
        private static string passMaskBreach = "\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022       \u24B7";
        private static Network network = new Network(PwMLib.GlobalVariables.apiHIBPMain);
        /// <summary>
        /// Decrypt vault and populate applist with applications info.
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="vaultName"></param>
        /// <param name="masterPassword"></param>
        /// <returns></returns>
        public static bool DecryptAndPopulateList(ListView listView, string vaultName, SecureString masterPassword, string vaultPath)
        {
            try
            {
                listView.Items.Clear();
                string pathToVault = string.Empty;
                if (vaultPath.StartsWith("Local"))
                {
                    pathToVault = Path.Combine(PwMLib.GlobalVariables.passwordManagerDirectory, $"{vaultName}.x");
                }
                else
                {
                    PwMLib.GlobalVariables.sharedVault = true;
                    pathToVault = Path.Combine(vaultPath, $"{vaultName}.x");
                }

                if (!File.Exists(pathToVault))
                {
                    Notification.ShowNotificationInfo("red", $"Vault {vaultName} does not exist!");
                    return false;
                }
                if (masterPassword == null)
                {
                    Notification.ShowNotificationInfo("red", "Master password must be entered!");
                    return false;
                }
                string readVault = File.ReadAllText(pathToVault);
                string decryptVault = AES.Decrypt(readVault, PasswordValidator.ConvertSecureStringToString(masterPassword));
                if (decryptVault.Contains("Error decrypting"))
                {
                    Notification.ShowNotificationInfo("red", "Something went wrong. Master password is incorrect or vault issue!");
                    PwMLib.GlobalVariables.masterPasswordCheck = false;
                    MasterPasswordTimerStart.MasterPasswordCheck_TimerStop(MainWindow.s_masterPassCheckTimer);
                    return false;
                }
                vaultSecure = PasswordValidator.StringToSecureString(decryptVault);
                using (var reader = new StringReader(decryptVault))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Length > 0)
                        {
                            var outJson = JsonSerializer.Deserialize<Dictionary<string, string>>(line);
                            SetMask(outJson["site/application"], outJson["account"], outJson["password"], ref listView);
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
        public static void AddApplication(ListView listView, string vaultName, string application, string accountName, string accountPassword, SecureString masterPassword, string vaultPath)
        {
            string pathToVault;
            if (vaultPath.StartsWith("Local"))
            {
                pathToVault = Path.Combine(PwMLib.GlobalVariables.passwordManagerDirectory, $"{vaultName}.x");
            }
            else
            {
                pathToVault = Path.Combine(vaultPath, $"{vaultName}.x");
                if (LockedVault.IsVaultLocked(pathToVault)) return;
            }
            if (!File.Exists(pathToVault))
            {
                Notification.ShowNotificationInfo("red", $"Vault {vaultName} does not exist!");
                return;
            }
            if (masterPassword == null)
            {
                ClearVariables.VariablesClear();
                return;
            }
            foreach (var item in listView.Items)
            {
                string app = item.ToString().SplitByText(", ", 0).Replace("{ Application = ", string.Empty);
                string acc = item.ToString().SplitByText(", ", 1).Replace("Account = ", string.Empty);
                if (app == (application) && acc == (accountName))
                {
                    Notification.ShowNotificationInfo("orange", $"Application {application} already contains {accountName} account!");
                    return;
                }
            }
            string readVault = File.ReadAllText(pathToVault);
            string decryptVault = AES.Decrypt(readVault, PasswordValidator.ConvertSecureStringToString(masterPassword));
            if (decryptVault.Contains("Error decrypting"))
            {
                Notification.ShowNotificationInfo("red", "Something went wrong. Master password is incorrect or vault issue!");
                PwMLib.GlobalVariables.masterPasswordCheck = false;
                MasterPasswordTimerStart.MasterPasswordCheck_TimerStop(MainWindow.s_masterPassCheckTimer);
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
            string encryptdata = AES.Encrypt(decryptVault + "\n" + JsonSerializer.Serialize(keyValues), PasswordValidator.ConvertSecureStringToString(masterPassword));
            vaultSecure = PasswordValidator.StringToSecureString(decryptVault + "\n" + JsonSerializer.Serialize(keyValues));
            if (File.Exists(pathToVault))
            {
                try
                {
                    File.WriteAllText(pathToVault, encryptdata);
                    Notification.ShowNotificationInfo("green", $"Data for {application} is encrypted and added to vault!");
                    SetMask(application, accountName, accountPassword, ref listView);
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    Notification.ShowNotificationInfo("red", $"Access denied: Vault is write protected for this user.");
                    return;
                }
            }
            Notification.ShowNotificationInfo("red", $"Vault {vaultName} does not exist!");
            ListViewSettings.ListViewSortSetting(listView, "site/application", false);
        }

        /// <summary>
        /// Delete applicaiton from vault.
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="vaultName"></param>
        /// <param name="application"></param>
        /// <param name="accountName"></param>
        /// <param name="masterPassword"></param>
        public static void DeleteApplicaiton(ListView listView, string vaultName, string application, string accountName, SecureString masterPassword, string vaultPath)
        {
            List<string> listApps = new List<string>();
            bool accountCheck = false;
            string pathToVault;
            if (vaultPath.StartsWith("Local"))
            {
                pathToVault = Path.Combine(PwMLib.GlobalVariables.passwordManagerDirectory, $"{vaultName}.x");
            }
            else
            {
                pathToVault = Path.Combine(vaultPath, $"{vaultName}.x");
                if (LockedVault.IsVaultLocked(pathToVault)) return;
            }
            if (masterPassword == null)
            {
                ClearVariables.VariablesClear();
                return;
            }
            if (!File.Exists(pathToVault))
            {
                Notification.ShowNotificationInfo("red", $"Vault {vaultName} does not exist!");
                return;
            }
            string readVault = File.ReadAllText(pathToVault);
            string decryptVault = AES.Decrypt(readVault, PasswordValidator.ConvertSecureStringToString(masterPassword));
            if (decryptVault.Contains("Error decrypting"))
            {
                Notification.ShowNotificationInfo("red", "Something went wrong. Master password is incorrect or vault issue!");
                PwMLib.GlobalVariables.masterPasswordCheck = false;
                MasterPasswordTimerStart.MasterPasswordCheck_TimerStop(MainWindow.s_masterPassCheckTimer);
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
            using (var reader = new StringReader(decryptVault))
            {
                string line;
                listView.Items.Clear();
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length > 0)
                    {
                        listApps.Add(line);
                        var outJson = JsonSerializer.Deserialize<Dictionary<string, string>>(line);
                        if (outJson["site/application"] == application && outJson["account"] == accountName)
                        {
                            listApps.Remove(line);
                            accountCheck = true;
                        }
                        else
                        {
                            SetMask(outJson["site/application"], outJson["account"], outJson["password"], ref listView);
                        }
                    }
                }
                string encryptdata = AES.Encrypt(string.Join("\n", listApps), PasswordValidator.ConvertSecureStringToString(masterPassword));
                vaultSecure = PasswordValidator.StringToSecureString(string.Join("\n", listApps));
                listApps.Clear();
                if (File.Exists(pathToVault))
                {
                    if (accountCheck)
                    {
                        try
                        {
                            File.WriteAllText(pathToVault, encryptdata);
                            Notification.ShowNotificationInfo("green", $"Account {accountName} for {application} was deleted");
                            return;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            Notification.ShowNotificationInfo("red", $"Access denied: Vault is write protected for this user.");
                            return;
                        }
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
        public static void UpdateAccount(ListView listView, string vaultName, string application, string accountName, string password, SecureString masterPassword, string vaultPath)
        {
            List<string> listApps = new List<string>();
            bool accountCheck = false;
            string pathToVault;
            if (vaultPath.StartsWith("Local"))
            {
                pathToVault = Path.Combine(PwMLib.GlobalVariables.passwordManagerDirectory, $"{vaultName}.x");
            }
            else
            {
                pathToVault = Path.Combine(vaultPath, $"{vaultName}.x");
                if (LockedVault.IsVaultLocked(pathToVault)) return;
            }
            if (!File.Exists(pathToVault))
            {
                Notification.ShowNotificationInfo("red", $"Vault {vaultName} does not exist!");
                return;
            }
            if (masterPassword == null)
            {
                ClearVariables.VariablesClear();
                return;
            }
            string readVault = File.ReadAllText(pathToVault);
            string decryptVault = AES.Decrypt(readVault, PasswordValidator.ConvertSecureStringToString(masterPassword));
            if (decryptVault.Contains("Error decrypting"))
            {
                MasterPasswordTimerStart.MasterPasswordCheck_TimerStop(MainWindow.s_masterPassCheckTimer);
                PwMLib.GlobalVariables.masterPasswordCheck = false;
                Notification.ShowNotificationInfo("red", "Something went wrong. Master password is incorrect or vault issue!");
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
            using (var reader = new StringReader(decryptVault))
            {
                string line;
                listView.Items.Clear();
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length > 0)
                    {
                        var outJson = JsonSerializer.Deserialize<Dictionary<string, string>>(line);
                        if (outJson["site/application"] == application && outJson["account"] == accountName)
                        {
                            var keyValues = new Dictionary<string, object>
                            {
                                 { "site/application", application },
                                 { "account", accountName },
                                 { "password", password },
                            };
                            accountCheck = true;
                            listApps.Add(JsonSerializer.Serialize(keyValues));
                            SetMask(outJson["site/application"], outJson["account"], password, ref listView);
                        }
                        else
                        {
                            listApps.Add(line);
                            SetMask(outJson["site/application"], outJson["account"], outJson["password"], ref listView);
                        }
                    }
                }
                string encryptdata = AES.Encrypt(string.Join("\n", listApps), PasswordValidator.ConvertSecureStringToString(masterPassword));
                vaultSecure = PasswordValidator.StringToSecureString(string.Join("\n", listApps));
                listApps.Clear();
                if (File.Exists(pathToVault))
                {
                    if (accountCheck)
                    {
                        try
                        {
                            File.WriteAllText(pathToVault, encryptdata);
                            Notification.ShowNotificationInfo("green", $"Password for account {accountName} for {application} application was updated!");
                            return;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            Notification.ShowNotificationInfo("red", $"Access denied: Vault is write protected for this user.");
                            return;
                        }
                    }
                    Notification.ShowNotificationInfo("orange", $"Account {accountName} does not exist!");
                    return;
                }
                Notification.ShowNotificationInfo("red", $"Vault {vaultName} does not exist!");
            }
            ListViewSettings.ListViewSortSetting(listView, "site/application", false);
        }

        /// <summary>
        /// Set mask depending if breached or not.
        /// </summary>
        /// <param name="application"></param>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <param name="listView"></param>
        private static void SetMask(string application, string account, string password, ref ListView listView)
        {
            var breach = "0";
            if (network.PingHost())
            {
                var hibp = new HIBP(PwMLib.GlobalVariables.apiHIBP);
                breach = Task.Run(() => hibp.CheckIfPwnd(password).Result).Result;
            }
            if (breach == "0")
                listView.Items.Add(new { Application = application, Account = account, Password = passMask });
            else
                listView.Items.Add(new { Application = application, Account = account, Password = passMaskBreach });
        }

        /// <summary>
        /// Show password function for right click context menu event on applist.
        /// </summary>
        /// <param name="listView"></param>
        public static void ShowPassword(ListView listView)
        {
            var isPasswordBreachMask = false;
            ListView tempListView = new ListView();
            if (listView.SelectedItem == null)
            {
                Notification.ShowNotificationInfo("orange", "You must select an application line to show the account password!");
                return;
            }

            string selectedItem = listView.SelectedItem.ToString();
            if (selectedItem.Contains("\u24B7"))
                selectedItem = selectedItem.Replace($", Password = {passMaskBreach} " + "}", string.Empty);
            else
                selectedItem = selectedItem.Replace($", Password = {passMask} " + "}", string.Empty);
            selectedItem = selectedItem.Replace("{ Application = ", string.Empty);
            selectedItem = selectedItem.Replace(", Account = ", "|");
            var parsedData = selectedItem.Split('|');
            if (PwMLib.GlobalVariables.listItems.Count == 1)
                isPasswordBreachMask = string.Join("", PwMLib.GlobalVariables.listItems).Contains("\u24B7");
            var vault = PasswordValidator.ConvertSecureStringToString(vaultSecure);
            if (CountLines(vault) >= 2)
            {
                var vaultToLines = PasswordValidator.ConvertSecureStringToString(vaultSecure).Split(new[] { '\r', '\n' });
                foreach (var line in vaultToLines)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        var outJson = JsonSerializer.Deserialize<Dictionary<string, string>>(line);
                        if (outJson["site/application"] == parsedData[0] && outJson["account"] == parsedData[1])
                            tempListView.Items.Add(new { Application = outJson["site/application"], Account = outJson["account"], Password = outJson["password"] });
                        else
                        {
                            var isBreachMask = false;
                            foreach (var item in PwMLib.GlobalVariables.listItems)
                            {
                                var jsonSplit = item.Split(',');
                                if (jsonSplit[0].Contains(outJson["site/application"]) && jsonSplit[1].Contains(outJson["account"]) && jsonSplit[2].Contains("\u24B7"))
                                {
                                    isBreachMask = true;
                                    break;
                                }
                            }
                            if (isBreachMask)
                                tempListView.Items.Add(new { Application = outJson["site/application"], Account = outJson["account"], Password = passMaskBreach });
                            else
                                tempListView.Items.Add(new { Application = outJson["site/application"], Account = outJson["account"], Password = passMask });
                        }
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(vault))
                {
                    var outJson = JsonSerializer.Deserialize<Dictionary<string, string>>(vault);
                    if (outJson["site/application"] == parsedData[0] && outJson["account"] == parsedData[1])
                        tempListView.Items.Add(new { Application = outJson["site/application"], Account = outJson["account"], Password = outJson["password"] });
                    else
                    {
                        if (isPasswordBreachMask)
                            tempListView.Items.Add(new { Application = outJson["site/application"], Account = outJson["account"], Password = passMaskBreach });
                        else
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
            ListViewSettings.ListViewSortSetting(listView, "site/application", false);
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
            string application = GetApplicationFromListView(listView);
            string account = GetAccountFromListView(listView);
            if (account.Length > 0 && application.Length > 0)
            {
                var vaultToLines = PasswordValidator.ConvertSecureStringToString(vaultSecure).Split(new[] { '\r', '\n' });
                foreach (var line in vaultToLines)
                {
                    if (line.Length > 0)
                    {
                        var outJson = JsonSerializer.Deserialize<Dictionary<string, string>>(line);
                        if (outJson["site/application"] == application && outJson["account"] == account)
                        {
                            outPass = outJson["password"];
                            PwMLib.GlobalVariables.accountPassword = outJson["password"];
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
        public static void DeleteSelectedItem(ListView listView, string vaultName, string vaultPath, ListView vaultList)
        {
            var pathToVault = Path.Combine(vaultPath, $"{vaultName}.x");
            if (LockedVault.IsVaultLocked(pathToVault)) return;
            string application = GetApplicationFromListView(listView);
            if (application.Length > 0)
            {
                string account = GetAccountFromListView(listView);
                if (account.Length > 0 && application.Length > 0)
                {
                    PwMLib.GlobalVariables.accountName = account;
                    PwMLib.GlobalVariables.applicationName = application;
                    DelApplications delApplications = new DelApplications();
                    delApplications.ShowDialog();
                    if (PwMLib.GlobalVariables.deleteConfirmation)
                    {
                        if (!PwMLib.GlobalVariables.masterPasswordCheck)
                        {
                            var masterPassword = MasterPasswordLoad.LoadMasterPassword(vaultName);
                            DeleteApplicaiton(listView, vaultName, application, account, masterPassword, vaultPath);
                            ClearVariables.VariablesClear();
                            return;
                        }
                        DeleteApplicaiton(listView, vaultName, application, account, PwMLib.GlobalVariables.masterPassword, vaultPath);
                        ClearVariables.VariablesClear();
                    }
                }
            }
            else
            {
                Notification.ShowNotificationInfo("orange", "You must select an application to delete!");
            }
            ListViewSettings.ListViewSortSetting(listView, "site/application", false);
        }

        /// <summary>
        /// Get account name from selected item in listview.
        /// </summary>
        /// <param name="listView"></param>
        /// <returns></returns>
        private static string GetAccountFromListView(ListView listView)
        {
            string account = string.Empty;
            if (listView.SelectedItem == null)
            {
                return account;
            }
            string selectedItem = listView.SelectedItem.ToString();
            account = selectedItem.SplitByText(", ", 1).Replace("Account = ", string.Empty);
            return account;
        }

        /// <summary>
        /// Get application name from selected item in listview.
        /// </summary>
        /// <param name="listView"></param>
        /// <returns></returns>
        private static string GetApplicationFromListView(ListView listView)
        {
            string application = string.Empty;
            if (listView.SelectedItem == null)
            {
                return application;
            }
            string selectedItem = listView.SelectedItem.ToString();
            application = selectedItem.SplitByText(", ", 0).Replace("{ Application = ", string.Empty);
            return application;
        }

        /// <summary>
        /// Update applicaiton account password via context menu(right click).
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="vaultName"></param>
        public static void UpdateSelectedItemPassword(ListView listView, string vaultName, string vaultPath)
        {
            var pathToVault = Path.Combine(vaultPath, $"{vaultName}.x");
            if (LockedVault.IsVaultLocked(pathToVault)) return;
            string application = GetApplicationFromListView(listView);
            string account = GetAccountFromListView(listView);
            if (account.Length > 0 && application.Length > 0)
            {
                PwMLib.GlobalVariables.accountName = account;
                PwMLib.GlobalVariables.applicationName = application;
                UpdateApplication updateApplication = new UpdateApplication();
                updateApplication.ShowDialog();
                string newPassword = PwMLib.GlobalVariables.newAccountPassword;
                if (!string.IsNullOrEmpty(newPassword))
                {
                    if (!PwMLib.GlobalVariables.masterPasswordCheck)
                    {
                        var masterPassword = MasterPasswordLoad.LoadMasterPassword(vaultName);
                        UpdateAccount(listView, vaultName, application, account, newPassword, masterPassword, vaultPath);
                        ClearVariables.VariablesClear();
                        return;
                    }
                    UpdateAccount(listView, vaultName, application, account, newPassword, PwMLib.GlobalVariables.masterPassword, vaultPath);
                    ClearVariables.VariablesClear();
                }
            }
            ListViewSettings.ListViewSortSetting(listView, "site/application", false);
        }

        /// <summary>
        /// Set apps to temporary list.
        /// </summary>
        /// <param name="listView"></param>
        public static void AddAppsToTempList(ListView listView)
        {
            PwMLib.GlobalVariables.listItems.Clear();
            for (int i = 0; i <= listView.Items.Count - 1; i++)
                PwMLib.GlobalVariables.listItems.Add(listView.Items[i].ToString());
        }
    }
}