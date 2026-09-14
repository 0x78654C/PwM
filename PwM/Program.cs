using System;
using System.Runtime.Versioning;

namespace PwM;

[SupportedOSPlatform("windows7.0")]
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // The temporary updater must bypass WPF, vault initialization and the single-instance mutex.
        if (args.Length > 0 && args[0] == "--apply-update")
        {
            AutoUpdater.UpdaterApplication.Run(args[1..]);
            return;
        }

        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        new App().Run();
    }
}
