using PwMLib;
using System;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
namespace PwM.Utils
{
    public class VaultManagement
    {
        /// <summary>
        /// Create initial vaults files.
        /// </summary>
        /// <param name="vaultName">Vault name.</param>
        /// <param name="password">Password</param>
        /// <param name="confirmPassword">Confirm password.</param>
        public static void CreateVault(string vaultName, string password, string confirmPassword, string vaultDirectory)
        {
            try
            {
                string pathToVault = Path.Combine(vaultDirectory, $"{vaultName}.x");
                if (File.Exists(pathToVault))
                {
                    Notification.ShowNotificationInfo("orange", $"Vault {vaultName} already exist!");
                    PwMLib.GlobalVariables.vaultChecks = true;
                    return;
                }

                if (vaultName.Length < 3)
                {
                    Notification.ShowNotificationInfo("orange", "Vault name must be at least 3 characters long.");
                    PwMLib.GlobalVariables.vaultChecks = true;
                    return;
                }

                if (!PasswordValidator.ValidatePassword(confirmPassword))
                {
                    Notification.ShowNotificationInfo("orange", "Password must be at least 12 characters, and must include at least one upper case letter, one lower case letter, one numeric digit, one special character and no space!");
                    PwMLib.GlobalVariables.vaultChecks = true;
                    return;
                }

                string sealVault = AES.Encrypt(string.Empty, confirmPassword);
                File.WriteAllText(pathToVault, sealVault);
                Notification.ShowNotificationInfo("green", $"Vault {vaultName} was created!");
                PwMLib.GlobalVariables.createConfirmation = true;
            }
            catch (Exception e)
            {
                Notification.ShowNotificationInfo("red", e.Message);
            }
        }



        /// <summary>
        /// Delete vault by item selection on vault list.
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="vaultDirectory"></param>
        public static void DeleteVaultItem(ListView listView, string vaultDirectory)
        {
            string vault = GetVaultNameFromListView(listView);
            if (vault.Length > 0)
            {
                PwMLib.GlobalVariables.vaultName = vault;
                DeleteVault deleteVault = new DeleteVault();
                deleteVault.ShowDialog();
                if (PwMLib.GlobalVariables.deleteConfirmation)
                {
                    DeleteVault(vault, vaultDirectory, listView);
                    ClearVariables.VariablesClear();
                }
            }
        }

        /// <summary>
        /// Get application name from selected item in listview.
        /// </summary>
        /// <param name="listView"></param>
        /// <returns></returns>

        public static string GetVaultNameFromListView(ListView listView)
        {
            string application = string.Empty;
            if (listView.SelectedItem == null)
            {
                Notification.ShowNotificationInfo("orange", "You must select a vault to delete!");
                return application;
            }
            string selectedItem = listView.SelectedItem.ToString();
            application = selectedItem.SplitByText(", ", 0).Replace("{ Name = ", string.Empty);
            return application;
        }

        /// <summary>
        /// Deletes a specific vault file.
        /// </summary>
        /// <param name="vaultName">Vault Name.</param>
        /// <param name="password">Master password.</param>
        private static void DeleteVault(string vaultName, string vaultDirectory, ListView vaultsList)
        {
            try
            {
                if (vaultDirectory.StartsWith("Local"))
                {
                    string pathToVault = Path.Combine(PwMLib.GlobalVariables.passwordManagerDirectory, $"{vaultName}.x");
                    if (!File.Exists(pathToVault))
                    {
                        Notification.ShowNotificationInfo("orange", $"Vault {vaultName} does not exist!");
                        return;
                    }
                    File.Delete(pathToVault);
                    Notification.ShowNotificationInfo("green", $"Vault { vaultName} was deleted!");
                    ListVaults(PwMLib.GlobalVariables.passwordManagerDirectory, vaultsList, false);
                    return;
                }
                JsonManage.DeleteJsonData<VaultDetails>(PwMLib.GlobalVariables.jsonSharedVaults, f => f.Where(t => t.VaultName == vaultName + ".x" && t.SharedPath == vaultDirectory));
                Notification.ShowNotificationInfo("green", $"Shared vault { vaultName} was removed from list!");
                ListVaults(vaultDirectory, vaultsList, true);
            }
            catch (Exception e)
            {
                Notification.ShowNotificationInfo("red", e.Message);
            }
        }

        /// <summary>
        /// List current vaults in listView object.
        /// </summary>
        /// <param name="vaultsDirectory">Path to vault directory.</param>
        public static void ListVaults(string vaultsDirectory, ListView listView, bool enableShare)
        {
            PwMLib.GlobalVariables.vaultsCount = 0;
            listView.Items.Clear();

            if (enableShare)
                vaultsDirectory = PwMLib.GlobalVariables.passwordManagerDirectory;

            if (!Directory.Exists(vaultsDirectory))
            {
                Notification.ShowNotificationInfo("red", "Vaults directory does not exist");
                return;
            }

            var getFiles = new DirectoryInfo(vaultsDirectory).GetFiles();
            foreach (var file in getFiles)
            {
                PwMLib.GlobalVariables.vaultsCount++;
                if (file.Name.EndsWith(".x"))
                {
                    listView.Items.Add(new { Name = file.Name.Substring(0, file.Name.Length - 2), CreateDate = file.CreationTime, SharePoint = "Local Stored" });
                }
            }
            if (File.Exists(PwMLib.GlobalVariables.jsonSharedVaults))
            {
                VaultDetails[] items;
                try
                {
                    items = JsonManage.ReadJsonFromFile<VaultDetails[]>(PwMLib.GlobalVariables.jsonSharedVaults);
                    FileInfo fileInfo;
                    foreach (var item in items)
                    {
                        string vaultPathFile = Path.Combine(item.SharedPath, item.VaultName);
                        fileInfo = new FileInfo(vaultPathFile);
                        listView.Items.Add(new { Name = item.VaultName.Substring(0, item.VaultName.Length - 2), CreateDate = fileInfo.CreationTime, SharePoint = item.SharedPath });
                    }
                }
                catch
                {
                    Notification.ShowNotificationInfo("red", "Shared vault list is corrupted. Try import again the shared vaults!");
                    File.Delete(PwMLib.GlobalVariables.jsonSharedVaults);
                    return;
                }
            }
        }

        /// <summary>
        /// Function for clear applist, and all passwords boxes and text boxes from application tab, closes it and moves to vault tab.
        /// </summary>
        /// <param name="homeListView"></param>
        /// <param name="vaultListView"></param>
        /// <param name="appListView"></param>
        /// <param name="appList"></param>
        /// <param name="tabControl"></param>
        public static void VaultClose(ListViewItem vaultListView, ListViewItem appListView, ListViewItem settingsListView,
            ListView appList, TabControl tabControl, DispatcherTimer masterPasswordTimer)
        {
            ListViewSettings.SetListViewColor(vaultListView, false);
            ListViewSettings.SetListViewColorApp(appListView, true);
            ListViewSettings.SetListViewColor(settingsListView, true);
            appList.Items.Clear();
            tabControl.SelectedIndex = 0;
            appListView.Foreground = Brushes.Red;
            appListView.IsEnabled = false;
            PwMLib.GlobalVariables.masterPassword = null;
            PwMLib.GlobalVariables.vaultOpen = false;
            AppManagement.vaultSecure = null;
            MasterPasswordTimerStart.MasterPasswordCheck_TimerStop(masterPasswordTimer);
            GC.Collect();
        }

        /// <summary>
        /// Get vault path from vault list.
        /// </summary>
        /// <param name="listView"></param>
        /// <returns></returns>
        public static string GetVaultPathFromList(ListView listView)
        {
            string vaultPath=string.Empty;
            if (listView.SelectedItem != null)
            {
                string item = listView.SelectedItem.ToString();
                vaultPath = item.Split(',')[2].Replace(" SharePoint = ", "");
                vaultPath = vaultPath.Replace(" }", "");
            }
            return vaultPath;
        }
        /// <summary>
        /// Change Master Password for a vault!
        /// </summary>
        /// <param name="vaultList"></param>
        /// <param name="oldMasterPassword"></param>
        /// <param name="newMasterPassword"></param>
        public static void ChangeMassterPassword(ListView vaultList)
        {
            string oldMasterPassword = PasswordValidator.ConvertSecureStringToString(PwMLib.GlobalVariables.masterPassword);
            string newMasterPassword = PasswordValidator.ConvertSecureStringToString(PwMLib.GlobalVariables.newMasterPassword);
            if (vaultList.SelectedItem == null)
            {
                Notification.ShowNotificationInfo("orange", "You must select a vault for changeing the Master Password!");
                return;
            }
            string vaultName = GetVaultNameFromListView(vaultList);
            string pathToVault;
            string vaultPath = GetVaultPathFromList(vaultList);
            if (vaultPath.StartsWith("Local"))
            {
                pathToVault = Path.Combine(PwMLib.GlobalVariables.passwordManagerDirectory, $"{vaultName}.x");
            }
            else
            {
                pathToVault = Path.Combine(vaultPath, $"{vaultName}.x");
            }
            if (!File.Exists(pathToVault))
            {
                Notification.ShowNotificationInfo("red", $"Vault {vaultName} does not exist!");
                return;
            }
            string readVault = File.ReadAllText(pathToVault);
            string decryptVault = AES.Decrypt(readVault, oldMasterPassword);
            if (decryptVault.Contains("Error decrypting"))
            {
                Notification.ShowNotificationInfo("red", "Something went wrong. Master password is incorrect or vault issue!");
                return;
            }
            string encryptdata = AES.Encrypt(decryptVault, newMasterPassword);
            if (!File.Exists(pathToVault))
            {
                Notification.ShowNotificationInfo("red", $"Vault {vaultName} does not exist!");
                return;
            }
            try
            {
                File.WriteAllText(pathToVault, encryptdata);
            }
            catch (UnauthorizedAccessException)
            {
                Notification.ShowNotificationInfo("red", $"Access denied: Vault is write protected for this user.");
                return;
            }
            Notification.ShowNotificationInfo("green", $"New Master Password was set for {vaultName} vault!");
        }
    }
}
