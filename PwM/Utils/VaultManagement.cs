﻿using System;
using System.IO;
using System.Windows.Controls;

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
                    GlobalVariables.vaultChecks = true;
                    return;
                }

                if (vaultName.Length < 3)
                {
                    Notification.ShowNotificationInfo("orange", "Vault name should have at least 3 characters long.");
                    GlobalVariables.vaultChecks = true;
                    return;
                }

                if (!Encryption.PasswordValidator.ValidatePassword(confirmPassword))
                {
                    Notification.ShowNotificationInfo("orange", "Password must be at least 12 characters, and must include at least one upper case letter, one lower case letter, one numeric digit, one special character and no space!");
                    GlobalVariables.vaultChecks = true;
                    return;
                }

                string sealVault = Encryption.AES.Encrypt(string.Empty, confirmPassword);
                File.WriteAllText(pathToVault, sealVault);
                Notification.ShowNotificationInfo("green", $"Vault {vaultName} was created!");
                GlobalVariables.createConfirmation = "yes";
            }
            catch (Exception e)
            {
                Notification.ShowNotificationInfo("red", e.Message);
            }
        }


        /// <summary>
        /// Clear PasswordBoxes input.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="confirmPassword"></param>
        public static void ClearPBoxesInput(PasswordBox password, PasswordBox confirmPassword)
        {
            password.Clear();
            confirmPassword.Clear();
        }

        /// <summary>
        /// Delete vault by item selection on vault list.
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="vaultDirectory"></param>
        public static void DeleteVaultItem(ListView listView, string vaultDirectory)
        {
            string vault = GetVaultNameFromListView(listView);
            GlobalVariables.vaultName = vault;
            DeleteVault deleteVault = new DeleteVault();
            deleteVault.ShowDialog();
            if (GlobalVariables.deleteConfirmation == "yes")
            {
                var masterPassword = MasterPasswordLoad.LoadMasterPassword(vault);
                if (masterPassword != null)
                    DeleteVault(vault, Encryption.PasswordValidator.ConvertSecureStringToString(masterPassword), vaultDirectory, listView);
                ClearVariables.VariablesClear();
            }
        }

        /// <summary>
        /// Get application name from selected item in listview.
        /// </summary>
        /// <param name="listView"></param>
        /// <returns></returns>

        private static string GetVaultNameFromListView(ListView listView)
        {
            string application = string.Empty;
            if (listView.SelectedItem == null)
            {
                Notification.ShowNotificationInfo("orange", "You must select a vault for delete!");
                return application;
            }
            else
            {
                string selectedItem = listView.SelectedItem.ToString();
                application = selectedItem.SplitByText(", ", 0).Replace("{ Name = ", string.Empty);
            }
            return application;
        }

        /// <summary>
        /// Deletes a specific vault file.
        /// </summary>
        /// <param name="vaultName">Vault Name.</param>
        /// <param name="password">Master password.</param>
        private static void DeleteVault(string vaultName, string masterPassword, string vaultDirectory, ListView vaultsList)
        {
            string pathToVault = Path.Combine(vaultDirectory, $"{vaultName}.x");
            if (!File.Exists(pathToVault))
            {
                Notification.ShowNotificationInfo("orange", $"Vault {vaultName} does not exist!");
                return;
            }
            string readEncData = File.ReadAllText(pathToVault);
            string decryptVault = Encryption.AES.Decrypt(readEncData, masterPassword);
            if (decryptVault.Contains("Error decrypting"))
            {
                Notification.ShowNotificationInfo("red", "Something went wrong. Check master password or vault name!");
                return;
            }
            File.Delete(pathToVault);
            Notification.ShowNotificationInfo("green", $"Vault { vaultName} was deleted!");
            ListVaults(vaultDirectory, vaultsList);
        }

        /// <summary>
        /// List current vaults in listView object.
        /// </summary>
        /// <param name="vaultsDirectory">Path to vault directory.</param>
        public static void ListVaults(string vaultsDirectory, ListView listView)
        {
            GlobalVariables.vaultsCount = 0;
            listView.Items.Clear();
            if (!Directory.Exists(vaultsDirectory))
            {
                Notification.ShowNotificationInfo("red", "Vaults directory does not exist");
                return;
            }

            var getFiles = new DirectoryInfo(vaultsDirectory).GetFiles();

            foreach (var file in getFiles)
            {
                GlobalVariables.vaultsCount++;
                listView.Items.Add(new { Name = file.Name.Substring(0, file.Name.Length - 2), CreateDate = file.CreationTime });
            }
        }
    }
}