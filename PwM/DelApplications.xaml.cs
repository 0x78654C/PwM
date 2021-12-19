using System.Windows;


namespace PwM
{
    /// <summary>
    /// Interaction logic for DelApplications.xaml
    /// </summary>
    public partial class DelApplications : Window
    {
        public DelApplications()
        {
            InitializeComponent();

            string application = Utils.GlobalVariables.applicationName;
            string account = Utils.GlobalVariables.accountName;
            notificationLBL.Text = $"Do you want tot delete {account} account for {application} application?";
        }

        /// <summary>
        /// Close button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void confirmBTN_Click(object sender, RoutedEventArgs e)
        {
            Utils.GlobalVariables.deleteConfirmation = "yes";
            this.Close();
        }

        private void CancelBTN_Click(object sender, RoutedEventArgs e)
        {
            Utils.GlobalVariables.applicationName = "";
            Utils.GlobalVariables.accountName = "";
            Utils.GlobalVariables.deleteConfirmation = "";
            this.Close();
        }
    }
}
