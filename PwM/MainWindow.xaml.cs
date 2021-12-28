using Microsoft.Win32;
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
using PwM.Utils;

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
        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;
        private System.Windows.Threading.DispatcherTimer _dispatcherTimer;
        public static  System.Windows.Threading.DispatcherTimer s_masterPassCheckTimer;
        Mutex MyMutex;

        public MainWindow()
        {
            Application_Startup(); // Check if PwM is already running.
            InitializeComponent();
            InitializeVaultsDirectory(s_passwordManagerDirectory);
            versionLabel.Content = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Utils.VaultManagement.ListVaults(s_passwordManagerDirectory, vaultList);
            userTXB.Text = " " + s_accountName;
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged; // Exit vault on suspend.
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch); // Exit vault on lock screen.
            Utils.ListViewSettings.SetListViewColor(vaultsListVI, false);
            Utils.ListViewSettings.SetListViewColorApp(appListVI, true);
        }

        /// <summary>
        /// Check aplication start instace and close if is already opened.
        /// </summary>
        private void Application_Startup()
        {
            MyMutex = new Mutex(true, "PwM", out bool aIsNewInstance);
            if (!aIsNewInstance)
            {
                Utils.Notification.ShowNotificationInfo("orange", "PwM - Password Manager is already running....");
                App.Current.Shutdown();
            }
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

        private void Vault_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Utils.ListViewSettings.SetListViewColor(vaultsListVI, false);
            Utils.ListViewSettings.SetListViewColorApp(appListVI, true);
            tabControl.SelectedIndex = 0;
        }
        private void App_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Utils.ListViewSettings.SetListViewColor(vaultsListVI, true);
            Utils.ListViewSettings.SetListViewColorApp(appListVI, false);
            tabControl.SelectedIndex = 1;
        }
        //--------------------



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
                Utils.VaultManagement.VaultClose(vaultsListVI, appListVI, appList, tabControl, s_masterPassCheckTimer);
                string vaultName = vaultList.SelectedItem.ToString();
                vaultName = vaultName.Split(',')[0].Replace("{ Name = ", "");
                var masterPassword = Utils.MasterPasswordLoad.LoadMasterPassword(vaultName);
                Utils.GlobalVariables.masterPassword = masterPassword;
                if (masterPassword != null && masterPassword.Length > 0)
                {
                    if (Utils.AppManagement.DecryptAndPopulateList(appList, vaultName, masterPassword))
                    {
                        appListVI.IsEnabled = true;
                        appListVI.Foreground = (Brush)converter.ConvertFromString("#FFDCDCDC");
                        Utils.ListViewSettings.SetListViewColor(vaultsListVI, true);
                        Utils.ListViewSettings.SetListViewColorApp(appListVI, false);
                        tabControl.SelectedIndex = 1;
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
        /// Clear applist, and all passwords boxes and text boxes from applicaiton tab, closes it and moves to vault tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void vaultCloseLBL_Click(object sender, RoutedEventArgs e)
        {
            Utils.VaultManagement.VaultClose(vaultsListVI, appListVI, appList, tabControl, s_masterPassCheckTimer);
        }

        /// <summary>
        /// Copy password from selected account for 15 seconds in clipboard. Right click context menu event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(Utils.AppManagement.CopyPassToClipBoard(appList));
            _dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            _dispatcherTimer.Tick += dispatcherTimer_Tick;
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 15);
            _dispatcherTimer.Start();
        }

        /// <summary>
        /// Clear clipboard after timer stops.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            ClipBoardUtil.ClearClipboard(Utils.GlobalVariables.accountPassword);
            _dispatcherTimer.Stop();
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
                    Utils.VaultManagement.VaultClose(vaultsListVI, appListVI, appList, tabControl, s_masterPassCheckTimer);
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
                Utils.VaultManagement.VaultClose(vaultsListVI, appListVI, appList, tabControl, s_masterPassCheckTimer);
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
            Utils.AppManagement.UpdateSelectedItemPassword(appList, appListVaultLVL.Text);
        }

        /// <summary>
        /// Add new applicaiton icon event (+).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddAppIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            AddApplications addApplications = new AddApplications();
            addApplications.ShowDialog();
            if (Utils.GlobalVariables.closeAppConfirmation != "yes")
            {
                if (!Utils.GlobalVariables.masterPasswordCheck)
                {
                    var masterPassword = Utils.MasterPasswordLoad.LoadMasterPassword(appListVaultLVL.Text);
                    Utils.AppManagement.AddApplication(appList, appListVaultLVL.Text, Utils.GlobalVariables.applicationName, Utils.GlobalVariables.accountName, Utils.GlobalVariables.accountPassword, masterPassword);
                    Utils.ClearVariables.VariablesClear();
                    return;
                }
                Utils.AppManagement.AddApplication(appList, appListVaultLVL.Text, Utils.GlobalVariables.applicationName, Utils.GlobalVariables.accountName, Utils.GlobalVariables.accountPassword, Utils.GlobalVariables.masterPassword);
                Utils.ClearVariables.VariablesClear();
            }
        }

        /// <summary>
        /// Delete applicaiton icon event (-).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DelAppIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Utils.AppManagement.DeleteSelectedItem(appList, appListVaultLVL.Text);
        }

        /// <summary>
        /// Delete account from listView context menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            Utils.AppManagement.DeleteSelectedItem(appList, appListVaultLVL.Text);
        }

        /// <summary>
        /// Delete vault from listView context menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteVault_Click(object sender, RoutedEventArgs e)
        {
            Utils.VaultManagement.VaultClose(vaultsListVI, appListVI, appList, tabControl, s_masterPassCheckTimer);
            Utils.VaultManagement.DeleteVaultItem(vaultList, s_passwordManagerDirectory);
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
            if (Utils.GlobalVariables.createConfirmation == "yes")
            {
                Utils.VaultManagement.ListVaults(s_passwordManagerDirectory, vaultList);
            }
        }

        /// <summary>
        /// Delete vault from '-' icon.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DelVaultIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Utils.VaultManagement.VaultClose(vaultsListVI, appListVI, appList, tabControl, s_masterPassCheckTimer);
            Utils.VaultManagement.DeleteVaultItem(vaultList, s_passwordManagerDirectory);
        }

        /// <summary>
        /// Import vault file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportVaultIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Utils.ImportExport.Import(vaultList, s_passwordManagerDirectory);
        }


        /// <summary>
        /// Export vault file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportVault_Click(object sender, RoutedEventArgs e)
        {
            Utils.ImportExport.Export(vaultList, s_passwordManagerDirectory);
        }

        /// <summary>
        /// Clearing clipboard on app closing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Pwm_Closing(object sender, CancelEventArgs e)
        {
            ClipBoardUtil.ClearClipboard(Utils.GlobalVariables.accountPassword);
        }


    }
}