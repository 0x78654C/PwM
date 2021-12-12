using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Security;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace PwM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Regex _regex = new Regex("[^!-*.-]+");
        private static string s_rootPath = Path.GetPathRoot(Environment.SystemDirectory);
        private static readonly string s_accountName = Environment.UserName;
        private static string s_passwordManagerDirectory = $"{s_rootPath}Users\\{s_accountName}\\AppData\\Local\\PwM\\";
        private static int s_vaultsCount = 0;
        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;
        System.Windows.Threading.DispatcherTimer dispatcherTimer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeVaultsDirectory(s_passwordManagerDirectory);
            ListVaults(s_passwordManagerDirectory);
            userTXB.Text = s_accountName;
            vaultsCountLBL.Text = s_vaultsCount.ToString();
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged; // Exit vault on suspend.
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch); // Exit vault on lock screen.
        }



        /// <summary>
        /// Create vautls directory.
        /// </summary>
        /// <param name="directoryName">Directory name.</param>
        private void InitializeVaultsDirectory(string directoryName)
        {
            if (!Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);
        }


        /// <summary>
        /// Create initial vaults files.
        /// </summary>
        /// <param name="vaultName">Vault name.</param>
        /// <param name="password">Password</param>
        /// <param name="confirmPassword">Confirm password.</param>
        private void CreateVault(TextBox vaultName, PasswordBox password, PasswordBox confirmPassword)
        {
            try
            {
                string pathToVault = Path.Combine(s_passwordManagerDirectory, $"{vaultName.Text}.x");
                if (File.Exists(pathToVault))
                {
                    Utils.Notification.ShowNotificationInfo("orange", $"Vault {vaultName.Text} already exist!");
                    ClearPBoxesInput(addVPassword, confirmVPassword);
                    vaultName.Clear();
                    return;
                }

                if (vaultName.Text.Length < 3)
                {
                    Utils.Notification.ShowNotificationInfo("orange", "Vault name should have at least 3 characters long.");
                    ClearPBoxesInput(addVPassword, confirmVPassword);
                    vaultName.Clear();
                    return;
                }

                if (!Encryption.PasswordValidator.ValidatePassword(confirmPassword.Password))
                {
                    Utils.Notification.ShowNotificationInfo("orange", "Password must be at least 12 characters, and must include at least one upper case letter, one lower case letter, one numeric digit, one special character and no space!");
                    ClearPBoxesInput(addVPassword, confirmVPassword);
                    return;
                }

                string sealVault = Encryption.AES.Encrypt(string.Empty, confirmPassword.Password);
                ClearPBoxesInput(addVPassword, confirmVPassword);
                File.WriteAllText(pathToVault, sealVault);
                Utils.Notification.ShowNotificationInfo("green", $"Vault {vaultName.Text} was created!");
                vaultName.Clear();
                ListVaults(s_passwordManagerDirectory);
            }
            catch (Exception e)
            {
                Utils.Notification.ShowNotificationInfo("red", e.Message);
            }
        }

        /// <summary>
        /// Deletes a specific vault file.
        /// </summary>
        /// <param name="vaultName">Vault Name.</param>
        /// <param name="password">Master password.</param>
        private void DeleteVault(TextBox vaultName, PasswordBox password)
        {
            string pathToVault = Path.Combine(s_passwordManagerDirectory, $"{vaultName.Text}.x");
            if (!File.Exists(pathToVault))
            {
                Utils.Notification.ShowNotificationInfo("orange", $"Vault {vaultName.Text} does not exist!");
                password.Clear();
                return;
            }
            string readEncData = File.ReadAllText(pathToVault);
            // string decryptVault = Encryption.AES.Decrypt(readEncData, password.Password);
            string decryptVault = Encryption.AES.Decrypt(readEncData, password.Password);
            if (decryptVault.Contains("Error decrypting"))
            {
                Utils.Notification.ShowNotificationInfo("red", "Something went wrong. Check master password or vault name!");
                password.Clear();
                return;
            }
            File.Delete(pathToVault);
            Utils.Notification.ShowNotificationInfo("green", $"Vault { vaultName.Text} was deleted!");
            vaultName.Clear();
            password.Clear();
            ListVaults(s_passwordManagerDirectory);
        }


        /// <summary>
        /// List current vaults in listView object.
        /// </summary>
        /// <param name="vaultsDirectory">Path to vault directory.</param>
        private void ListVaults(string vaultsDirectory)
        {
            s_vaultsCount = 0;
            vaultList.Items.Clear();
            if (!Directory.Exists(vaultsDirectory))
            {
                Utils.Notification.ShowNotificationInfo("red", "Vaults directory does not exist");
                return;
            }

            var getFiles = new DirectoryInfo(vaultsDirectory).GetFiles();

            foreach (var file in getFiles)
            {
                s_vaultsCount++;
                vaultList.Items.Add(new { Name = file.Name.Substring(0, file.Name.Length - 2), CreateDate = file.CreationTime });
            }
        }

        /// <summary>
        /// Clear PasswordBoxes input.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="confirmPassword"></param>
        private void ClearPBoxesInput(PasswordBox password, PasswordBox confirmPassword)
        {
            password.Clear();
            confirmPassword.Clear();
        }


        /// <summary>
        /// Create vault button action.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveBTN_Click(object sender, RoutedEventArgs e)
        {
            CreateVault(vaultNameTXT, addVPassword, confirmVPassword);
        }

        /// <summary>
        /// Delete vault button action.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void delBTN_Click(object sender, RoutedEventArgs e)
        {
            DeleteVault(vaultNameTXTDelete, delVPassword);
        }


        //------------------------UI Settings------------------------------

        /// <summary>
        /// Column sort click on heard for applist.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppListColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    string header = headerClicked.Column.Header as string;
                    Sort(header, appList, direction);
                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }

        /// <summary>
        /// Column sort click on heard for Vault List.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VaultListColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    string header = headerClicked.Column.Header as string;
                    Sort(header, vaultList, direction);

                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }

        /// <summary>
        /// Sort function for column header click on listview.
        /// </summary>
        /// <param name="sortBy"></param>
        /// <param name="listView"></param>
        /// <param name="direction"></param>
        private void Sort(string sortBy, ListView listView, ListSortDirection direction)
        {
            ICollectionView dataView =
              CollectionViewSource.GetDefaultView(listView.Items);
            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }
        /// <summary>
        /// Drag window on mouse click left
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();

        }
        /// <summary>
        /// Close wpf form button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeBTN_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();//close the app
        }


        /// <summary>
        /// Minimizr button(label)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miniMizeLBL_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// About window open button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void aboutBTN_Click(object sender, RoutedEventArgs e)
        {
            var aB = new about();
            aB.ShowDialog();
        }


        // Tab Switch
        private void Home_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SetListViewColor(homeListVI, false);
            SetListViewColor(vaultsListVI, true);
            SetListViewColorApp(appListVI, true);
            tabControl.SelectedIndex = 0;
        }
        private void Vault_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SetListViewColor(homeListVI, true);
            SetListViewColor(vaultsListVI, false);
            SetListViewColorApp(appListVI, true);
            tabControl.SelectedIndex = 1;
        }
        private void App_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SetListViewColor(homeListVI, true);
            SetListViewColor(vaultsListVI, true);
            SetListViewColorApp(appListVI, false);
            tabControl.SelectedIndex = 2;
        }
        //--------------------

        /// <summary>
        /// Set Color of listView on click. 
        /// </summary>
        /// <param name="listViewItem"></param>
        /// <param name="reset"></param>
        private void SetListViewColor(ListViewItem listViewItem, bool reset)
        {
            var converter = new BrushConverter();
            if (reset)
            {
                listViewItem.Background = Brushes.Transparent;
                listViewItem.Foreground = (Brush)converter.ConvertFromString("#FFDCDCDC");
                return;
            }
            listViewItem.Background = (Brush)converter.ConvertFromString("#6f2be3");
        }

        /// <summary>
        /// Set Color of listView on click. 
        /// </summary>
        /// <param name="listViewItem"></param>
        /// <param name="reset"></param>
        private void SetListViewColorApp(ListViewItem listViewItem, bool reset)
        {
            if (listViewItem.IsEnabled)
            {
                var converter = new BrushConverter();
                if (reset)
                {
                    listViewItem.Background = Brushes.Transparent;
                    listViewItem.Foreground = (Brush)converter.ConvertFromString("#FFDCDCDC");
                    return;
                }
                listViewItem.Background = (Brush)converter.ConvertFromString("#6f2be3");
            }
        }


        /// <summary>
        /// Acceptin only custom characters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdPrefixTXT_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }
        /// <summary>
        /// check for regex match
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }

        /// <summary>
        /// Prevent pasting letterts 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                String text = (String)e.DataObject.GetData(typeof(String));
                if (!IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        /// <summary>
        /// Check password length and enable create button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void confirmVPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            createBTN.IsEnabled = (confirmVPassword.Password == addVPassword.Password && confirmVPassword.Password.Length >= 12);
        }

        /// <summary>
        /// Check password length and enable delete vault button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void delVPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            delBTN.IsEnabled = (delVPassword.Password.Length >= 12);
        }

        /// <summary>
        /// Load master password form MasterPassword form after confirmation.
        /// </summary>
        /// <param name="vaultName"></param>
        /// <returns></returns>
        private SecureString LoadMasterPassword(string vaultName)
        {
            SecureString password;
            Utils.GlobalVariables.vaultName = vaultName;
            MasterPassword masterPassword = new MasterPassword();
            masterPassword.ShowDialog();
            password = masterPassword.masterPassword;
            masterPassword.masterPasswordPWD.Clear();
            masterPassword.masterPassword = null;
            return password;
        }

        /// <summary>
        /// Decrypt vault buy double click on it and populate appList on Application tab and switch to it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void vaultList_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenVault();
        }


        /// <summary>
        /// Open selected vault from vault list.
        /// </summary>
        private void OpenVault()
        {
            var converter = new BrushConverter();
            if (vaultList.SelectedItem != null)
            {
                string vaultName = vaultList.SelectedItem.ToString();
                vaultName = vaultName.Split(',')[0].Replace("{ Name = ", "");
                var masterPassword = LoadMasterPassword(vaultName);
                if (masterPassword != null && masterPassword.Length > 0)
                {
                    if (Utils.AppManagement.DecryptAndPopulateList(appList, vaultName, masterPassword))
                    {
                        appListVI.IsEnabled = true;
                        appListVI.Foreground = (Brush)converter.ConvertFromString("#FFDCDCDC");
                        SetListViewColor(homeListVI, true);
                        SetListViewColor(vaultsListVI, true);
                        SetListViewColorApp(appListVI, false);
                        tabControl.SelectedIndex = 2;
                        appListVaultLVL.Text = vaultName;
                    }
                }
            }
            else
            {
                appListVI.Foreground = Brushes.Red;
                appListVI.IsEnabled = false;
            }
        }

        /// <summary>
        /// Check password length and enable create vault button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addVPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            createBTN.IsEnabled = (confirmVPassword.Password == addVPassword.Password && addVPassword.Password.Length >= 12);
        }


        /// <summary>
        /// Clear applist, and all passwords boxes and text boxes from applicaiton tab, closes it and moves to vault tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void vaultCloseLBL_Click(object sender, RoutedEventArgs e)
        {
            VaultClose();
        }

        // Function for clear applist, and all passwords boxes and text boxes from applicaiton tab, closes it and moves to vault tab.
        public void VaultClose()
        {
            SetListViewColor(homeListVI, true);
            SetListViewColor(vaultsListVI, false);
            SetListViewColorApp(appListVI, true);
            appList.Items.Clear();
            tabControl.SelectedIndex = 1;
            appListVI.Foreground = Brushes.Red;
            appListVI.IsEnabled = false;
            Utils.TextPassBoxChanges.ClearTextPassBox(appDeleteTXT, accDeleteTXT);
            Utils.TextPassBoxChanges.ClearTextPassBox(appNameTXT, accountNameTXT, accPasswordBox);
            Utils.TextPassBoxChanges.ClearTextPassBox(appNameUTXT, accNameUTXT, newPassAccBox);
            Utils.AppManagement.vaultSecure = null;
            GC.Collect();
        }

        /// <summary>
        /// Add application button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addAppBTN_Click(object sender, RoutedEventArgs e)
        {
            var masterPassword = LoadMasterPassword(appListVaultLVL.Text);
            if (masterPassword != null)
            {
                Utils.AppManagement.AddApplication(appList, appListVaultLVL.Text, appNameTXT.Text, accountNameTXT.Text, accPasswordBox.Password, masterPassword);
                Utils.TextPassBoxChanges.ClearTextPassBox(appNameTXT, accountNameTXT, accPasswordBox);
            }
        }

        // Password, text boxes length check and add application button enable .
        private void appNameTXT_TextChanged(object sender, TextChangedEventArgs e)
        {
            Utils.TextPassBoxChanges.TextPassBoxChanged(appNameTXT, accountNameTXT, accPasswordBox, addAppBTN);
        }

        private void accountNameTXT_TextChanged(object sender, TextChangedEventArgs e)
        {
            Utils.TextPassBoxChanges.TextPassBoxChanged(appNameTXT, accountNameTXT, accPasswordBox, addAppBTN);
        }

        private void accPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Utils.TextPassBoxChanges.TextPassBoxChanged(appNameTXT, accountNameTXT, accPasswordBox, addAppBTN);
        }
        //-----------------------------
        private void delAppBTN_Click(object sender, RoutedEventArgs e)
        {
            var masterPassword = LoadMasterPassword(appListVaultLVL.Text);
            if (masterPassword != null)
            {
                Utils.AppManagement.DeleteApplicaiton(appList, appListVaultLVL.Text, appDeleteTXT.Text, accDeleteTXT.Text, masterPassword);
                Utils.TextPassBoxChanges.ClearTextPassBox(appDeleteTXT, accDeleteTXT);
            }
        }
        // Password, text boxes length check and delete application button enable.
        private void appDeleteTXT_TextChanged(object sender, TextChangedEventArgs e)
        {
            Utils.TextPassBoxChanges.TextPassBoxChanged(accDeleteTXT, appDeleteTXT, delAppBTN);
        }

        private void accDeleteTXT_TextChanged(object sender, TextChangedEventArgs e)
        {
            Utils.TextPassBoxChanges.TextPassBoxChanged(accDeleteTXT, appDeleteTXT, delAppBTN);
        }
        //----------------------------------------------

        /// <summary>
        /// Update account password from a applicaiton.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void updateAccPassBTN_Click(object sender, RoutedEventArgs e)
        {
            var masterPassword = LoadMasterPassword(appListVaultLVL.Text);
            if (masterPassword != null)
            {
                Utils.AppManagement.UpdateAccount(appList, appListVaultLVL.Text, appNameUTXT.Text, accNameUTXT.Text, newPassAccBox.Password, masterPassword);
                Utils.TextPassBoxChanges.ClearTextPassBox(appNameUTXT, accNameUTXT, newPassAccBox);
            }
        }

        // Password, text boxes length check and add application button enable for 
        private void appNameUTXT_TextChanged(object sender, TextChangedEventArgs e)
        {
            Utils.TextPassBoxChanges.TextPassBoxChanged(appNameUTXT, accNameUTXT, newPassAccBox, updateAccPassBTN);
        }

        private void accNameUTXT_TextChanged(object sender, TextChangedEventArgs e)
        {
            Utils.TextPassBoxChanges.TextPassBoxChanged(appNameUTXT, accNameUTXT, newPassAccBox, updateAccPassBTN);
        }

        private void newPassAccBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Utils.TextPassBoxChanges.TextPassBoxChanged(appNameUTXT, accNameUTXT, newPassAccBox, updateAccPassBTN);
        }
        //----------------------------------------

        /// <summary>
        /// Copy password from selected account for 15 seconds in clipboard. Right click context menu event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(Utils.AppManagement.CopyPassToClipBoard(appList));
            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 15);
            dispatcherTimer.Start();
        }

        /// <summary>
        /// Clear clipboard after timer stops.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Clipboard.Clear();
            dispatcherTimer.Stop();
        }

        /// <summary>
        /// Show password from selected account. Right click context menu event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowPassword_Click(object sender, RoutedEventArgs e)
        {
            Utils.AppManagement.ShowPassword(appList);
        }


        /// <summary>
        /// Check if PC enters sleep or hibernate mode and lock vault.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Suspend:
                    VaultClose();
                    break;
            }
        }

        /// <summary>
        /// Check if lock screen and close vault.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                VaultClose();
            }
        }


        /// <summary>
        /// Enter key event for open vault.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void vaultList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OpenVault();
            }
        }
    }
}