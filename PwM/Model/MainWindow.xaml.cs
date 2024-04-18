using Microsoft.Win32;
using PwM.Utils;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PwM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Regex s_regex = new Regex("[^!-*.-]+");
        private static readonly Regex s_regexNumber = new Regex("[^0-9.-]+"); //regex that matches disallowed text
        private static readonly string s_accountName = Environment.UserName;
        private static string s_passwordManagerDirectory = GlobalVariables.passwordManagerDirectory;
        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;
        private DispatcherTimer _dispatcherTimer;
        private DispatcherTimer _dispatcherTimerCloseVault;
        private DispatcherTimer _dispatcherTimerElapsed;
        private int _vaultCloseSesstion=0;
        public static DispatcherTimer s_masterPassCheckTimer;
        private string _vaultPath;
        private string _vaultName;
        Mutex MyMutex;

        public MainWindow()
        {
            Application_Startup(); // Check if PwM is already running.
            InitializeComponent();
            InitializeVaultsDirectory(s_passwordManagerDirectory);
            s_masterPassCheckTimer = new DispatcherTimer();
            versionLabel.Content = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            VaultManagement.ListVaults(s_passwordManagerDirectory, vaultList, false);
            userTXB.Text = " " + s_accountName;
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged; // Exit vault on suspend.
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch); // Exit vault on lock screen.
            ListViewSettings.SetListViewColor(vaultsListVI, false);
            ListViewSettings.SetListViewColorApp(appListVI, true);
            VaultSessionExpire.LoadExpireTime(GlobalVariables.registryPath, GlobalVariables.vaultExpireReg, "10", expirePeriodTxT);
        }

        /// <summary>
        /// Check application start instance and close if is already opened.
        /// </summary>
        private void Application_Startup()
        {
            MyMutex = new Mutex(true, "PwM", out bool aIsNewInstance);
            if (!aIsNewInstance)
            {
                Notification.ShowNotificationInfo("orange", "PwM - Password Manager is already running....");
                App.Current.Shutdown();
            }
        }


        /// <summary>
        /// Create vaults directory.
        /// </summary>
        /// <param name="directoryName">Directory name.</param>
        private void InitializeVaultsDirectory(string directoryName)
        {
            if (!Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);
        }


        //------------------------UI Settings------------------------------
        /// <summary>
        /// Column sort click on heard for applist.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppListColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            RestartTimerVaultClose();
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
        /// Minimize button(label)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void minimizeLBL_PreviewMouseDown(object sender, MouseButtonEventArgs e)
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

        private void Vault_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ListViewSettings.SetListViewColor(vaultsListVI, false);
            ListViewSettings.SetListViewColorApp(appListVI, true);
            ListViewSettings.SetListViewColorApp(settingsListVI, true);
            tabControl.SelectedIndex = 0;
        }
        private void App_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ListViewSettings.SetListViewColor(vaultsListVI, true);
            ListViewSettings.SetListViewColorApp(appListVI, false);
            ListViewSettings.SetListViewColorApp(settingsListVI, true);
            tabControl.SelectedIndex = 1;
        }

        private void Settings_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ListViewSettings.SetListViewColor(vaultsListVI, true);
            ListViewSettings.SetListViewColorApp(appListVI, true);
            ListViewSettings.SetListViewColorApp(settingsListVI, false);
            tabControl.SelectedIndex = 2;
        }
        //--------------------



        /// <summary>
        /// Accepting only custom characters
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
            return !s_regex.IsMatch(text);
        }

        /// <summary>
        /// Prevent pasting letters 
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
                VaultManagement.VaultClose(vaultsListVI, appListVI, settingsListVI, appList, tabControl, s_masterPassCheckTimer);
                string item = vaultList.SelectedItem.ToString();
                string vaultPath = VaultManagement.GetVaultPathFromList(vaultList);
                _vaultPath = vaultPath;
                string vaultName = item.Split(',')[0].Replace("{ Name = ", "");
                var masterPassword = MasterPasswordLoad.LoadMasterPassword(vaultName);
                GlobalVariables.masterPassword = masterPassword;
                if (masterPassword != null && masterPassword.Length > 0)
                {
                    if (AppManagement.DecryptAndPopulateList(appList, vaultName, masterPassword, vaultPath))
                    {
                        appListVI.IsEnabled = true;
                        appListVI.Foreground = (Brush)converter.ConvertFromString("#FFDCDCDC");
                        ListViewSettings.SetListViewColor(vaultsListVI, true);
                        ListViewSettings.SetListViewColor(settingsListVI, true);
                        ListViewSettings.SetListViewColorApp(appListVI, false);
                        tabControl.SelectedIndex = 1;
                        _vaultName = vaultName;
                        if (GlobalVariables.sharedVault)
                            appListVaultLVL.Text = $"{vaultName} (shared)";
                        else
                            appListVaultLVL.Text = vaultName;
                        GlobalVariables.sharedVault = false;
                        GlobalVariables.vaultOpen = true;
                        StartTimerVaultClose();
                        Sort("Application", appList, ListSortDirection.Ascending);
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
        /// Clear applist, and all passwords boxes and text boxes from application tab, closes it and moves to vault tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void vaultCloseLBL_Click(object sender, RoutedEventArgs e)
        {
            VaultManagement.VaultClose(vaultsListVI, appListVI, settingsListVI, appList, tabControl, s_masterPassCheckTimer);
            VaultCloseTimersStop();
        }

        /// <summary>
        /// Close vault session and elapesed timer and hide warning message and icon.
        /// </summary>
        private void VaultCloseTimersStop()
        {
            if (_dispatcherTimerCloseVault != null)
            {
                if (_dispatcherTimerCloseVault.IsEnabled)
                    _dispatcherTimerCloseVault.Stop();
                if (_dispatcherTimerElapsed.IsEnabled)
                    _dispatcherTimerElapsed.Stop();
                vaultExpireTb.Visibility = Visibility.Hidden;
                vaultElapsed.Visibility = Visibility.Hidden;
                _vaultCloseSesstion = GlobalVariables.vaultExpireInterval * 60;
            }
        }

        /// <summary>
        /// Copy password from selected account for 15 seconds in clipboard. Right click context menu event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            RestartTimerVaultClose();
            if (appList.SelectedIndex == -1)
            {
                Notification.ShowNotificationInfo("orange", "You must select a application account for Copy to Clipboard option!");
                return;
            }

            Clipboard.SetText(AppManagement.CopyPassToClipBoard(appList));
            _dispatcherTimer = new DispatcherTimer();
            _dispatcherTimer.Tick += dispatcherTimer_Tick;
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 15);
            _dispatcherTimer.Start();
        }


        /// <summary>
        /// Timer start for innactivity check. Time set until close is 2h.
        /// </summary>
        private void StartTimerVaultClose()
        {
            _vaultCloseSesstion = GlobalVariables.vaultExpireInterval * 60;

            _dispatcherTimerCloseVault = new DispatcherTimer();
            _dispatcherTimerCloseVault.Tick += VaultCloseTimer;
            _dispatcherTimerCloseVault.Interval = new TimeSpan(0, GlobalVariables.vaultExpireInterval, 0);
            _dispatcherTimerCloseVault.Start();

            _dispatcherTimerElapsed = new DispatcherTimer();
            _dispatcherTimerElapsed.Tick += DisplayElapsedTimeVaultClose;
            _dispatcherTimerElapsed.Interval = new TimeSpan(0, 0, 1);
            _dispatcherTimerElapsed.Start();

        }

        /// <summary>
        /// Show warning message and icon when interval is less than a minute.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayElapsedTimeVaultClose(object sender, EventArgs e)
        {
            _vaultCloseSesstion--;

            if (_vaultCloseSesstion < 60)
            {
                vaultExpireTb.Visibility = Visibility.Visible;
                vaultElapsed.Visibility = Visibility.Visible;
                vaultExpireTb.Text = $"Vault will close in less than a \r\n" +
                    $"minute if no action is made on it!";
                _vaultCloseSesstion = GlobalVariables.vaultExpireInterval * 60;
            }
        }

        /// <summary>
        /// Restart timer event. Used on applications actions to reset the inactivity timer.
        /// </summary>
        private void RestartTimerVaultClose()
        {
            VaultCloseTimersStop();
            StartTimerVaultClose();
        }

        /// <summary>
        /// Vault close event and opened window from PwM in that current state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VaultCloseTimer(object sender, EventArgs e)
        {
            string[] listOpenWindow = { "UpdateApplicationWPF", "AddApplicationWPF", "MasterPasswordWPF", "DelApplicationWpf" };
            foreach (var window in listOpenWindow)
            {
                WindowCloser.CloseWindow(window);
            }
            VaultManagement.VaultClose(vaultsListVI, appListVI, settingsListVI, appList, tabControl, s_masterPassCheckTimer);
            VaultCloseTimersStop();
        }

        /// <summary>
        /// Clear clipboard after timer stops.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            ClipBoardUtil.ClearClipboard(GlobalVariables.accountPassword);
            GlobalVariables.accountPassword = null;
            _dispatcherTimer.Stop();
        }

        /// <summary>
        /// Show password from selected account. Right click context menu event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowPassword_Click(object sender, RoutedEventArgs e)
        {
            RestartTimerVaultClose();
            AppManagement.ShowPassword(appList);
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
                    VaultManagement.VaultClose(vaultsListVI, appListVI, settingsListVI, appList, tabControl, s_masterPassCheckTimer);
                    VaultCloseTimersStop();
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
                VaultManagement.VaultClose(vaultsListVI, appListVI, settingsListVI, appList, tabControl, s_masterPassCheckTimer);
                VaultCloseTimersStop();
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


        /// <summary>
        /// Update account password event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateAccountPass_Click(object sender, RoutedEventArgs e)
        {
            RestartTimerVaultClose();
            if (appList.SelectedIndex == -1)
            {
                Notification.ShowNotificationInfo("orange", "You must select a application line for updateing account password!");
                return;
            }
            AppManagement.UpdateSelectedItemPassword(appList, _vaultName, _vaultPath);
        }

        /// <summary>
        /// Add new application icon event (+).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddAppIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            RestartTimerVaultClose();
            AddApplications addApplications = new AddApplications();
            addApplications.ShowDialog();
            if (GlobalVariables.closeAppConfirmation == false)
            {
                if (!GlobalVariables.masterPasswordCheck)
                {
                    var masterPassword = MasterPasswordLoad.LoadMasterPassword(_vaultName);
                    AppManagement.AddApplication(appList, _vaultName, GlobalVariables.applicationName, GlobalVariables.accountName, GlobalVariables.accountPassword, masterPassword, _vaultPath);
                    ClearVariables.VariablesClear();
                    return;
                }
                AppManagement.AddApplication(appList, _vaultName, GlobalVariables.applicationName, GlobalVariables.accountName, GlobalVariables.accountPassword, GlobalVariables.masterPassword, _vaultPath);
                ClearVariables.VariablesClear();
            }
        }

        /// <summary>
        /// Delete application icon event (-).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DelAppIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            RestartTimerVaultClose();
            AppManagement.DeleteSelectedItem(appList, _vaultName, _vaultPath);
        }

        /// <summary>
        /// Delete account from listView context menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            RestartTimerVaultClose();
            AppManagement.DeleteSelectedItem(appList, _vaultName, _vaultPath);
        }

        /// <summary>
        /// Delete vault from listView context menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteVault_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalVariables.vaultOpen)
            {
                Notification.ShowNotificationInfo("orange", "You cannot delete when a vault is open!");
                return;
            }
            VaultCloseTimersStop();
            VaultManagement.DeleteVaultItem(vaultList, VaultManagement.GetVaultPathFromList(vaultList));
        }

        /// <summary>
        /// Add vault.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddVaultIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            AddVault addVault = new AddVault();
            addVault.ShowDialog();
            if (GlobalVariables.createConfirmation)
            {
                VaultManagement.ListVaults(s_passwordManagerDirectory, vaultList, false);
            }
        }

        /// <summary>
        /// Delete vault from '-' icon.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DelVaultIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (GlobalVariables.vaultOpen)
            {
                Notification.ShowNotificationInfo("orange", "You cannot delete when a vault is open!");
                return;
            }
            VaultManagement.DeleteVaultItem(vaultList, VaultManagement.GetVaultPathFromList(vaultList));
        }

        /// <summary>
        /// Import vault file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportVaultIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (GlobalVariables.vaultOpen)
            {
                Notification.ShowNotificationInfo("orange", "You cannot import when a vault is open!");
                return;
            }
            ImportShared importShared = new ImportShared();
            importShared.ShowDialog();
            if (GlobalVariables.closeAppConfirmation == false)
                PopulateListView(vaultList);
            else
                GlobalVariables.closeAppConfirmation = true;

        }

        /// <summary>
        /// Populate listview with items form global var listview.
        /// </summary>
        /// <param name="listView"></param>
        private void PopulateListView(ListView listView)
        {
            listView.Items.Clear();
            foreach (var item in GlobalVariables.listView.Items)
            {
                listView.Items.Add(item);
            }
        }


        /// <summary>
        /// Export vault file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportVault_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalVariables.vaultOpen)
            {
                Notification.ShowNotificationInfo("orange", "You cannot export when a vault is open!");
                return;
            }
            ImportExport.Export(vaultList, s_passwordManagerDirectory);
        }

        /// <summary>
        /// Change master password for a specific vault.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeMasterPassword_Click(object sender, RoutedEventArgs e)
        {
            if (vaultList.SelectedIndex == -1)
            {
                Notification.ShowNotificationInfo("orange", "You must select a vault for changeing Master Password!");
                return;
            }
            GlobalVariables.vaultName = VaultManagement.GetVaultNameFromListView(vaultList);
            if (GlobalVariables.vaultOpen)
            {
                Notification.ShowNotificationInfo("orange", "You cannot change Master Password when a vault is open!");
                return;
            }
            MPasswordChanger mPasswordChanger = new MPasswordChanger();
            mPasswordChanger.ShowDialog();
            if (GlobalVariables.closeAppConfirmation == false)
            {
                VaultManagement.ChangeMassterPassword(vaultList);
            }
        }

        /// <summary>
        /// Clearing clipboard on app closing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Pwm_Closing(object sender, CancelEventArgs e)
        {
            ClipBoardUtil.ClearClipboard(GlobalVariables.accountPassword);
            GlobalVariables.accountPassword = string.Empty;
        }

        /// <summary>
        /// Check text if contains numbers only.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static bool IsNumberAllowed(string text)
        {
            return !s_regexNumber.IsMatch(text);
        }

        /// <summary>
        /// Apply expiration time for vault open session.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void applyExpirePeriodBTN_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (expirePeriodTxT.Text.Length > 0 && !expirePeriodTxT.Text.StartsWith("0"))
                {
                    if (IsNumberAllowed(expirePeriodTxT.Text) && !expirePeriodTxT.Text.Contains("-"))
                    {
                        int expireTime = Int32.Parse(expirePeriodTxT.Text);
                        RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, GlobalVariables.vaultExpireReg, expirePeriodTxT.Text);
                        GlobalVariables.vaultExpireInterval = expireTime;
                        Notification.ShowNotificationInfo("green", $"Vault session expire time is set to {expirePeriodTxT.Text} minutes!");
                        return;
                    }
                    Notification.ShowNotificationInfo("orange", "Numbers only are allowed!");
                    return;
                }
                Notification.ShowNotificationInfo("orange", "You must type a vaule greather than 0 !");
            }
            catch (Exception x)
            {
                Notification.ShowNotificationInfo("red", x.Message);
            }
        }
    }
}