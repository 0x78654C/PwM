using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PwM.Updating;

internal static class UpdateCleanup
{
    public static bool IsPrivateDirectory(string directory)
    {
        string path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        string root = Path.Combine(Path.GetTempPath(), "PwM-Updates");
        return string.Equals(Path.GetDirectoryName(path), Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)), StringComparison.OrdinalIgnoreCase)
            && Guid.TryParseExact(Path.GetFileName(path), "N", out _);
    }

    private static void ValidateDirectory(string directory)
    {
        if (!IsPrivateDirectory(directory)) throw new IOException("Refusing to clean a folder outside PwM's temporary updater directory.");
        foreach (string path in new[] { Path.GetDirectoryName(directory), directory })
            if (Directory.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Refusing to clean an updater folder through a symbolic link or junction.");
    }

    // Only call once the copy is no longer running (or was never launched).
    public static void RemoveCopy(string directory)
    {
        directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        ValidateDirectory(directory);
        try { DeleteTree(directory); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        // Remove the shared root only when it is empty. Other update sessions keep it in place.
        try { Directory.Delete(Path.GetDirectoryName(directory), recursive: false); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void DeleteTree(string directory)
    {
        if (!Directory.Exists(directory)) return;
        foreach (string entry in Directory.EnumerateFileSystemEntries(directory))
        {
            var attributes = File.GetAttributes(entry);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Refusing to follow a link in the temporary updater copy.");
            if ((attributes & FileAttributes.Directory) != 0) DeleteTree(entry);
            else
            {
                File.SetAttributes(entry, attributes & ~FileAttributes.ReadOnly);
                File.Delete(entry);
            }
        }
        Directory.Delete(directory);
    }

    internal static Process ScheduleAfterExit(string directory, Process owner, string installation)
    {
        directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        ValidateDirectory(directory);
        string executable = Path.GetFullPath(Path.Combine(installation, "PwM.UpdateCleanup.exe"));
        if (executable.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("The cleanup worker must run outside the temporary updater copy.");
        var start = CreateWorkerStartInfo(executable, installation);
        foreach (string argument in new[] { directory, owner.Id.ToString(CultureInfo.InvariantCulture),
            owner.StartTime.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture) })
            start.ArgumentList.Add(argument);
        return Process.Start(start) ?? throw new IOException("Could not start temporary updater cleanup.");
    }

    private static ProcessStartInfo CreateWorkerStartInfo(string executable, string installation)
    {
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = installation
        };
        // A native .NET startup error must never leave a hidden modal dialog running.
        start.Environment["DOTNET_DISABLE_GUI_ERRORS"] = "1";
        using var configuration = JsonDocument.Parse(File.ReadAllText(Path.ChangeExtension(executable, ".runtimeconfig.json")));
        var runtime = configuration.RootElement.GetProperty("runtimeOptions");
        if (runtime.TryGetProperty("framework", out _) || runtime.TryGetProperty("frameworks", out _))
        {
            // Legacy release ZIPs do not include this worker. Updating a framework-dependent
            // installation to a self-contained ZIP leaves the old worker next to hostfxr.dll.
            // Its apphost would use that local runtime as a shared-framework root and fail.
            // Use the host belonging to the shared runtime that is already running this updater.
            var runtimeDirectory = new DirectoryInfo(RuntimeEnvironment.GetRuntimeDirectory());
            if (runtimeDirectory.Parent?.Name == "Microsoft.NETCore.App"
                && runtimeDirectory.Parent.Parent?.Name == "shared")
            {
                string host = Path.Combine(runtimeDirectory.Parent.Parent.Parent.FullName, "dotnet.exe");
                if (!File.Exists(host)) throw new FileNotFoundException("The running .NET installation is missing dotnet.exe.", host);
                start.FileName = host;
                start.ArgumentList.Add(Path.ChangeExtension(executable, ".dll"));
            }
        }
        return start;
    }

    // Called in the installed C# cleanup worker after the updater has finished replacing files.
    public static void RunWorker(string directory, int ownerId, long ownerStart)
    {
        directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        ValidateDirectory(directory);
        if (ownerId <= 0 || ownerStart <= 0) throw new IOException("Invalid cleanup process identity.");
        if (!Directory.Exists(directory)) { RemoveCopy(directory); return; }
        using var owner = FindProcess(ownerId);
        if (owner != null)
        {
            try
            {
                if (!owner.HasExited && owner.StartTime.ToUniversalTime().Ticks == ownerStart)
                {
                    var waiting = Stopwatch.StartNew();
                    while (!owner.WaitForExit(500))
                    {
                        // The app's exit observer may already have removed the copy.
                        if (!Directory.Exists(directory)) { RemoveCopy(directory); return; }
                        // A stuck updater shutdown must not leave a cleanup process running forever.
                        if (waiting.Elapsed >= TimeSpan.FromMinutes(1))
                            throw new IOException("The updater has not exited. Temporary files were left in place.");
                    }
                }
            }
            catch (InvalidOperationException) when (owner.HasExited) { }
        }
        for (int attempt = 0; attempt < 120; attempt++)
        {
            RemoveCopy(directory);
            if (!Directory.Exists(directory)) return;
            Thread.Sleep(500);
        }
        throw new IOException("Temporary updater files are still locked.");
    }

    private static Process FindProcess(int id)
    {
        try { return Process.GetProcessById(id); }
        catch (ArgumentException) { return null; }
    }

    // The app can clean an aborted/crashed updater while it remains open.
    public static async Task ObserveExitAsync(string directory, int ownerId)
    {
        try
        {
            using var owner = FindProcess(ownerId);
            if (owner != null) await owner.WaitForExitAsync().ConfigureAwait(false);
            for (int attempt = 0; attempt < 120; attempt++)
            {
                RemoveCopy(directory);
                if (!Directory.Exists(directory)) return;
                await Task.Delay(500).ConfigureAwait(false);
            }
        }
        catch (Exception ex) { Trace.TraceWarning("PwM updater cleanup: {0}", ex); }
    }
}
