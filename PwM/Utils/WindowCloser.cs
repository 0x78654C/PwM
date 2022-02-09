using System.Windows;

namespace PwM.Utils
{
    public class WindowCloser
    {
        /// <summary>
        /// Close current opened WPF window.
        /// </summary>
        /// <param name="windowXmlName"></param>
        public static void CloseWindow(string windowXmlName)
        {
            foreach (Window w in Application.Current.Windows)
            {
                if (w.Name==windowXmlName)
                {
                    w.Close();
                }
            }
        }
    }
}
