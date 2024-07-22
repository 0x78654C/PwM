
namespace PwM.Utils
{
    public class ClearVariables
    {
        /// <summary>
        /// Clear global variables.
        /// </summary>
        public static void VariablesClear()
        {
            PwMLib.GlobalVariables.applicationName = "";
            PwMLib.GlobalVariables.accountName = "";
            PwMLib.GlobalVariables.newAccountPassword = "";
            PwMLib.GlobalVariables.closeAppConfirmation = false;
            PwMLib.GlobalVariables.deleteConfirmation = false;
        }
    }
}
