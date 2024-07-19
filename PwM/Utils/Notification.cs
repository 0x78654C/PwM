namespace PwM.Utils
{
    /* Notificaiton class */
    public class Notification
    {
        /// <summary>
        /// Show notificaiton pop up message box with diferent case color.
        /// </summary>
        /// <param name="gridColor">red - Error| green -  Confirmation | orange - Warning</param>
        /// <param name="messageData"></param>
        public static void ShowNotificationInfo(string gridColor, string messageData)
        {
            PwMLib.GlobalVariables.gridColor = gridColor;
            PwMLib.GlobalVariables.messageData = messageData;
            PopMessage popMessage = new PopMessage();
            popMessage.ShowDialog();
        }
    }
}
