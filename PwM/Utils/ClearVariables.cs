
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
            GlobalVariables.accountPassword = "";
            GlobalVariables.closeAppConfirmation = "";
            GlobalVariables.deleteConfirmation = "";
        }
    }
}
