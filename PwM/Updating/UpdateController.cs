using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PwM.Updating;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class UpdateController
{
    private readonly MainWindow _mainWindow;

    internal UpdateController(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }
    private bool _checkingForUpdate;
    private bool _closed;
    private bool _automaticCheckScheduled;
    private readonly CancellationTokenSource _updateLifetime = new();

    // Called by the production entry point so designer and isolated window tests never contact GitHub.
    internal void Initialize()
    {
        _mainWindow.ContentRendered += async (_, _) =>
        {
            if (_automaticCheckScheduled) return;
            _automaticCheckScheduled = true;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(6), _updateLifetime.Token);
                await CheckForUpdatesAsync(false);
            }
            catch (OperationCanceledException) { }
        };
        _mainWindow.Closed += (_, _) => { _closed = true; _updateLifetime.Cancel(); };
    }

    internal async Task CheckForUpdatesAsync(bool manual)
    {
        if (_checkingForUpdate || _closed) return;
        _checkingForUpdate = true;
        bool reportErrors = manual;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_updateLifetime.Token);
            timeout.CancelAfter(TimeSpan.FromSeconds(45));
            using var client = ReleaseCatalog.CreateClient();
            var installed = ReleaseCatalog.Normalize(Assembly.GetExecutingAssembly().GetName().Version);
            string architecture = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x64",
                _ => throw new NotSupportedException("Automatic updates support Windows x64 installations.")
            };
            var releases = await ReleaseCatalog.ReadAsync(client, timeout.Token);
            var update = ReleaseCatalog.SelectUpdate(releases, installed, architecture);
            // Wait for credential dialogs to finish before presenting an automatic update prompt.
            while (!manual && System.Windows.Interop.ComponentDispatcher.IsThreadModal)
                await Task.Delay(250, _updateLifetime.Token);
            if (_closed) return;
            if (update == null)
            {
                if (manual) MessageBox.Show(Owner, $"PwM {installed} ({architecture}) is up to date.", "PwM Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var window = new UpdateWindow { StartPosition = FormStartPosition.CenterParent };
            window.ShowRelease(installed, update);
            window.Summary.Text = $"Install {update.Package.Name}. PwM will close and restart; your vaults and settings stay in place.";
            window.Status.Text = "Ready when you are";
            using var downloadCancellation = CancellationTokenSource.CreateLinkedTokenSource(_updateLifetime.Token);
            var preparationToken = downloadCancellation.Token;
            string updaterPath = null;
            window.Secondary.Click += (_, _) => window.Close();
            window.FormClosing += (_, _) => downloadCancellation.Cancel();
            window.Primary.Click += async (_, _) =>
            {
                reportErrors = true;
                window.Primary.Enabled = false;
                window.Secondary.Text = "Cancel";
                string preparedPath = null;
                try
                {
                    window.Heading.Text = "Preparing your update";
                    window.Status.Text = "Preparing to install the release ZIP…";
                    preparedPath = await LocalUpdater.PrepareAsync(AppContext.BaseDirectory, preparationToken);
                    preparationToken.ThrowIfCancellationRequested();
                    updaterPath = preparedPath;
                    preparedPath = null;
                    window.DialogResult = DialogResult.OK;
                    window.Close();
                }
                catch (OperationCanceledException) when (preparationToken.IsCancellationRequested) { }
                catch (Exception ex)
                {
                    if (window.IsDisposed) return;
                    window.Heading.Text = "Couldn’t prepare the update";
                    window.Summary.Text = "Review the details below, then try again.";
                    window.Notes.Text = ex.Message;
                    window.Status.Text = "PwM is still open. You can try again later.";
                    window.Primary.Text = "Try again";
                    window.Primary.Enabled = true;
                    window.Secondary.Text = "Close";
                }
                finally
                {
                    if (preparedPath != null) UpdateCleanup.RemoveCopy(Path.GetDirectoryName(preparedPath));
                }
            };
            try
            {
                if (window.ShowDialog(Owner) == DialogResult.OK && updaterPath != null)
                {
                    string executable = updaterPath;
                    updaterPath = null; // LaunchUpdaterAsync now owns cleanup, including a failed launch.
                    await LaunchUpdaterAsync(executable, installed, update);
                }
            }
            finally
            {
                if (updaterPath != null) UpdateCleanup.RemoveCopy(Path.GetDirectoryName(updaterPath));
            }
        }
        catch (OperationCanceledException) when (_updateLifetime.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Trace.TraceWarning("PwM update check: {0}", ex);
            if (reportErrors && !_closed)
                MessageBox.Show(Owner, "Couldn’t check for updates. Please try again later.\n\n" + ex.Message,
                    "PwM Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally { _checkingForUpdate = false; }
    }

    private IWin32Window Owner => new WindowOwner(new System.Windows.Interop.WindowInteropHelper(_mainWindow).Handle);
    private sealed record WindowOwner(IntPtr Handle) : IWin32Window;

    private async Task LaunchUpdaterAsync(string executable, Version installed, UpdateRelease release)
    {
        Process updater = null;
        try
        {
            string session = "Local\\PwM.Update." + Guid.NewGuid().ToString("N");
            using var ready = new EventWaitHandle(false, EventResetMode.ManualReset, session + ".ready");
            using var cancelled = new EventWaitHandle(false, EventResetMode.ManualReset, session + ".cancel");
            using var parent = Process.GetCurrentProcess();
            var start = new ProcessStartInfo(executable) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(executable) };
            start.ArgumentList.Add("--apply-update");
            foreach (string argument in new[] { "--install-dir", AppContext.BaseDirectory, "--parent-pid", parent.Id.ToString(),
                "--parent-start", parent.StartTime.ToUniversalTime().Ticks.ToString(), "--session", session,
                "--current-version", installed.ToString(), "--version", release.Version.ToString(), "--arch", release.Architecture })
                start.ArgumentList.Add(argument);
            updater = Process.Start(start) ?? throw new IOException("The updater could not be started.");
            _ = UpdateCleanup.ObserveExitAsync(Path.GetDirectoryName(executable), updater.Id);
            bool handoff = false;
            try
            {
                var started = Stopwatch.StartNew();
                while (!ready.WaitOne(0))
                {
                    if (updater.HasExited || started.Elapsed > TimeSpan.FromSeconds(45))
                        throw new IOException("The updater did not become ready. PwM has been kept open.");
                    await Task.Delay(100, _updateLifetime.Token);
                }
                if (_closed) return;
                _mainWindow.Close(); // Use the normal close path to clear the clipboard and end the vault session.
                handoff = _closed;
            }
            finally
            {
                if (!handoff) cancelled.Set();
            }
        }
        finally
        {
            // The updater starts its C# cleanup worker when it closes.
            // Also cover failures before its entry point could run (including Process.Start).
            if (updater == null || updater.HasExited) UpdateCleanup.RemoveCopy(Path.GetDirectoryName(executable));
            updater?.Dispose();
        }
    }
}
