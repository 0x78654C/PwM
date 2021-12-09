using System.Windows;
using System.Windows.Media;

namespace PwM
{
    /// <summary>
    /// Interaction logic for PopMessage.xaml
    /// </summary>
    public partial class PopMessage : Window
    {
        public PopMessage()
        {
            InitializeComponent();
            SetUI(Utils.GlobalVariables.gridColor, Utils.GlobalVariables.messageData);
        }


        /// <summary>
        /// Close button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void confirmBTN_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        /// <summary>
        /// Setting the proper color for a specific message (error, notification, warning)
        /// </summary>
        /// <param name="gridColor"></param>
        /// <param name="messageData"></param>
        private void SetUI(string gridColor, string messageData)
        {
            switch (gridColor)
            {
                case "green":
                    popGrid.Background = Brushes.Green;
                    titleTxt.Text = "Notification";
                    notificationLBL.Text = messageData;
                    break;

                case "red":
                    popGrid.Background = Brushes.Red;
                    titleTxt.Text = "ERROR";
                    notificationLBL.Text = messageData;
                    break;

                case "orange":
                    popGrid.Background = Brushes.Orange;
                    titleTxt.Text = "WARNING";
                    notificationLBL.Text = messageData;
                    break;
            }
        }
    }
}