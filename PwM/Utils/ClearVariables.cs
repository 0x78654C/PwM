
namespace PwM.Utils
{
    public class ClearVariables
    {
        /// <summary>
        /// Clear global variables.
        /// </summary>
        public static void VariablesClear()
        {
            GlobalVariables.applicationName = "";
            GlobalVariables.accountName = "";
            GlobalVariables.newAccountPassword = "";
            GlobalVariables.closeAppConfirmation = false;
            GlobalVariables.deleteConfirmation = false;
        }
    }
}
