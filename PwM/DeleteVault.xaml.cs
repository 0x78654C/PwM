using System.Windows;

namespace PwM
{
    /// <summary>
    /// Interaction logic for DeleteVault.xaml
    /// </summary>
    public partial class DeleteVault : Window
    {
        public DeleteVault()
        {
            InitializeComponent();
            string vault = Utils.GlobalVariables.vaultName;
            notificationLBL.Text = $"Do you want tot delete {vault} vault?";
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
            Utils.GlobalVariables.deleteConfirmation = "";
            this.Close();
        }
    }
}
