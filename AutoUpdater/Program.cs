namespace PwM.AutoUpdater;

public static class UpdaterApplication
{
    public static void Run(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        UpdateOptions options = null;
        try
        {
            options = UpdateOptions.Parse(args);
            // Keep one updater per installation, including when several PwM processes are open.
            string key = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(options.InstallDirectory.ToUpperInvariant())));
            using var mutex = new Mutex(true, "Local\\PwM.Updater." + key, out bool acquired);
            if (!acquired) throw new InvalidOperationException("An update is already running for this PwM installation.");
            try { Application.Run(new UpdaterForm(options)); }
            finally { mutex.ReleaseMutex(); }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "PwM Update", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            // Run the bundled C# cleaner from the installation only after all file replacement is done.
            if (options != null && PwM.Updating.UpdateCleanup.IsPrivateDirectory(AppContext.BaseDirectory))
            {
                try
                {
                    using var current = System.Diagnostics.Process.GetCurrentProcess();
                    PwM.Updating.UpdateCleanup.ScheduleAfterExit(AppContext.BaseDirectory, current, options.InstallDirectory);
                }
                catch (Exception ex) { System.Diagnostics.Trace.TraceWarning("PwM updater cleanup: {0}", ex); }
            }
        }
    }
}
