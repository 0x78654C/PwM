using PwM.Utils;
using System.Runtime.Versioning;
using System.Windows;

namespace PwM
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    [SupportedOSPlatform("Windows")]
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            ThemeManager.Initialize();
        }
    }
}
