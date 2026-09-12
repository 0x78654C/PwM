using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using PwMLib;

namespace PwM.Utils
{
    internal static class PasswordBreachCheck
    {
        // Only submit a complete password when the user chooses to save it.
        public static async Task<bool> ConfirmAsync(Window owner, string password)
        {
            string message;
            try
            {
                var count = await new HIBP(PwMLib.GlobalVariables.apiHIBP).CheckIfPwnd(password);
                if (!owner.IsVisible) return false;
                if (count == "0") return true;
                message = "This password was found in a data breach. Save it anyway?";
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException or InvalidDataException)
            {
                message = "The breach check is unavailable. Save this password without checking it?";
            }

            if (!owner.IsVisible) return false;
            bool accepted = MessageBox.Show(owner, message, "Password breach check",
                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;
            return accepted && owner.IsVisible;
        }
    }
}
