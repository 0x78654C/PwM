using System.Drawing;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PwM.AutoUpdater;
using PwM.Updating;

internal static class Program
{
    private static int _checks;
    private static string _root;

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--wait-parent")
        {
            using var exit = EventWaitHandle.OpenExisting(args[1]);
            if (EventWaitHandle.TryOpenExisting(args[1] + ".started", out var started))
                using (started) started.Set();
            return exit.WaitOne(TimeSpan.FromSeconds(40)) ? 0 : 2;
        }
        if (File.Exists(Path.Combine(AppContext.BaseDirectory, ".updater-fixture")))
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, ".restarted"), "yes");
            return 0;
        }
        _root = Path.GetFullPath(Path.Combine("artifacts", "updater-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(_root);
        try
        {
            if (args.Length == 2 && args[0] == "--package")
            {
                ValidateReleaseZip(args[1]);
                Console.WriteLine($"PASS: {_checks} release ZIP checks. Artifacts: {_root}");
                return 0;
            }
            if (args.Length == 1 && args[0] == "--cleanup")
            {
                CleanupRoot();
                DeferredCleanup().GetAwaiter().GetResult();
                CompletedCleanup().GetAwaiter().GetResult();
                MismatchedRuntimeCleanup().GetAwaiter().GetResult();
                Console.WriteLine($"PASS: {_checks} cleanup checks. Artifacts: {_root}");
                return 0;
            }
            if (args.Length == 2 && args[0] == "--cleanup-package")
            {
                MismatchedRuntimeCleanup(args[1]).GetAwaiter().GetResult();
                Console.WriteLine($"PASS: {_checks} legacy release cleanup checks. Artifacts: {_root}");
                return 0;
            }
            if (args.Length == 2 && args[0] == "--cleanup-application")
            {
                CompletedCleanup(args[1]).GetAwaiter().GetResult();
                Console.WriteLine($"PASS: {_checks} installed cleanup checks. Artifacts: {_root}");
                return 0;
            }
            Catalog();
            Network().GetAwaiter().GetResult();
            LocalCopy().GetAwaiter().GetResult();
            CleanupRoot();
            DeferredCleanup().GetAwaiter().GetResult();
            CompletedCleanup().GetAwaiter().GetResult();
            MismatchedRuntimeCleanup().GetAwaiter().GetResult();
            Packages();
            RenderWindow();
            Handoff(cancel: true);
            Handoff(cancel: false);
            Handoff(cancel: false, packageVersion: "3.2.3.1");
            if (args.Length == 2 && args[0] == "--application")
            {
                BundledStartup(args[1]);
                BundledStartup(args[1], terminate: true);
            }
            Console.WriteLine($"PASS: {_checks} updater checks. Artifacts: {_root}");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        _checks++;
    }

    private static void ValidateReleaseZip(string path)
    {
        Assert(ReleaseCatalog.TryParsePackage(Path.GetFileName(path), out var version, out var architecture), "Release ZIP filename accepted");
        string install = Installation("release-package");
        using var installer = new PackageInstaller(install);
        string payload = installer.Extract(path, CancellationToken.None);
        PackageInstaller.ValidatePayload(payload, new UpdateOptions(install, 1, 1, "unused", new Version(0, 0, 0, 0), version, architecture));
        Assert(true, "Release ZIP layout, executable architecture and assembly version accepted");
        installer.Install(payload, null);
        Assert(File.Exists(Path.Combine(install, "PwM.exe")) && File.Exists(Path.Combine(install, "PwM.dll")),
            "Real release package installs into a disposable application folder");
    }

    private static void Reject(Action action, string message)
    {
        bool rejected = false;
        try { action(); } catch (Exception ex) when (ex is IOException or InvalidDataException) { rejected = true; }
        Assert(rejected, message);
    }

    private static ReleaseAsset Asset(string name, byte[] content = null)
    {
        content ??= Encoding.UTF8.GetBytes("release bytes");
        return new() { Name = name, DownloadUrl = "https://github.com/0x78654C/PwM/releases/download/test/" + name,
            Digest = "sha256:" + Convert.ToHexString(SHA256.HashData(content)), Size = content.Length, State = "uploaded" };
    }

    private static GitHubRelease Release(params string[] names) => new() { Assets = names.Select(n => Asset(n)).ToList() };

    private static void Catalog()
    {
        Assert(ReleaseCatalog.TryParsePackage("PwM-v1.2.3_x64_R2R.zip", out var releaseVersion, out var releaseArchitecture)
            && releaseVersion == new Version(1, 2, 3, 0) && releaseArchitecture == "x64", "Actual PwM v1.2.3 ZIP naming");
        foreach (string name in new[] { "PwM_1.3_Andorid_arm64-Signed.apk", "PwM_1.3_Andorid_x64-Signed.apk",
            "PwM-v1.2.4_x86_R2R.zip", "PwM-v1.2.4_arm64_R2R.zip", "PwM-v1.2.4_x64_R2R.exe", "Source code.zip" })
            Assert(!ReleaseCatalog.TryParsePackage(name, out _, out _), "Non-desktop ZIP asset rejected: " + name);
        foreach (string architecture in new[] { "x86", "arm64" })
        {
            bool unsupported = false;
            try { ReleaseCatalog.SelectUpdate(Array.Empty<GitHubRelease>(), new Version(1, 2, 3), architecture); }
            catch (NotSupportedException) { unsupported = true; }
            Assert(unsupported, "Unsupported process architecture rejected: " + architecture);
        }
        foreach (string name in new[] { "PwM-v3.2.4_x64_R2R.zip", "PwM-v3.2.4.1_x64_R2R.zip", "PWM-V3.2.4_X64_R2R.ZIP" })
            Assert(ReleaseCatalog.TryParsePackage(name, out _, out _), "Valid package: " + name);
        foreach (string name in new[] { "PwM-v3.2_x64_R2R.zip", "PwM-v3.2.4-arm64.zip", "PwM-v3.2.4-beta_x64_R2R.zip", "xPwM-v3.2.4_x64_R2R.zip", "PwM-v3.2.4_x64_R2R.zip.exe", "PwM-v3.2.4_x86.zip", "PwM-v999999999999.2.4_x64_R2R.zip" })
            Assert(!ReleaseCatalog.TryParsePackage(name, out _, out _), "Invalid package: " + name);
        var releases = new[] { Release("PwM-v3.9.0_x64_R2R.zip"), Release("PwM-v3.10.0_x64_R2R.zip", "PwM-v3.10.0_x86_R2R.zip", "unrelated.exe"),
            new GitHubRelease { Prerelease = true, Assets = new() { Asset("PwM-v9.0.0_x64_R2R.zip") } },
            new GitHubRelease { Draft = true, Assets = new() { Asset("PwM-v10.0.0_x64_R2R.zip") } } };
        var chosen = ReleaseCatalog.SelectUpdate(releases, new Version(3, 2, 3, 1), "x64");
        Assert(chosen.Version == new Version(3, 10, 0, 0), "Numeric order across stable releases");
        Assert(chosen.Package.Name == "PwM-v3.10.0_x64_R2R.zip", "Only the matching release ZIP is selected");
        Assert(ReleaseCatalog.SelectUpdate(new[] { Release("PwM-v3.10.0_x86_R2R.zip") }, new Version(3, 2, 3), "x64") == null, "x86 packages are ignored");
        Assert(ReleaseCatalog.SelectUpdate(releases, new Version(3, 10, 0), "x64") == null, "Equal version is not an update");
        Assert(ReleaseCatalog.SelectUpdate(releases, new Version(4, 0, 0), "x64") == null, "Never downgrade");
        Assert(ReleaseCatalog.SelectUpdate(new[] { Release("PwM-v3.2.3_x64_R2R.zip") }, new Version(3, 2, 3, 1), "x64") == null, "Revision does not trigger an older ZIP");
        Assert(ReleaseCatalog.SelectUpdate(new[] { Release("PwM-v3.2.3.2_x64_R2R.zip") }, new Version(3, 2, 3, 1), "x64") != null, "Revision update supported");
        var mixedVersions = new[] { Release("PwM-v3.2.3_x64_R2R.zip", "PwM-v3.2.3.1_x64_R2R.zip") };
        Assert(ReleaseCatalog.SelectUpdate(mixedVersions, new Version(3, 2, 2, 1), "x64").Package.Name == "PwM-v3.2.3.1_x64_R2R.zip",
            "Four-part release supersedes its three-part base version");
        Assert(ReleaseCatalog.SelectUpdate(mixedVersions, new Version(3, 2, 3), "x64").Version == new Version(3, 2, 3, 1),
            "A nonzero revision updates an installed three-part version");
        Assert(ReleaseCatalog.SelectUpdate(mixedVersions, new Version(3, 2, 3, 1), "x64") == null,
            "Neither ZIP replaces an equal or newer installed version");
        Assert(ReleaseCatalog.SelectUpdate(releases, new Version(3, 0, 0), "x64", new Version(3, 9, 0)).Version == new Version(3, 9, 0, 0), "Updater stays on approved release");
        Assert(ReleaseCatalog.SelectUpdate(new[] { releases[0] }, new Version(3, 0, 0), "x64") != null, "A release containing only the application ZIP can update");
        Assert(ReleaseCatalog.SelectUpdate(new[] { Release("unrelated.exe") }, new Version(3, 0, 0), "x64") == null, "Executable release assets are ignored");
        var unfinished = Release("PwM-v99.0.0_x64_R2R.zip");
        unfinished.Assets[0].State = "starter";
        Assert(ReleaseCatalog.SelectUpdate(new[] { unfinished }, new Version(3, 0, 0), "x64") == null, "Unfinished uploads skipped");
        foreach (string url in new[] { "http://github.com/0x78654C/PwM/releases/download/test/tool.exe", "https://evil.test/0x78654C/PwM/releases/download/test/tool.exe", "https://github.com/other/PwM/releases/download/test/tool.exe", "https://github.com/0x78654C/PwM/releases/download/test/tool.exe?x=1" })
            Reject(() => ReleaseCatalog.GetDownloadUri(new() { Name = "tool.exe", DownloadUrl = url }), "Reject untrusted asset URL");
    }

    private static async Task Network()
    {
        int requests = 0;
        using (var client = new HttpClient(new Handler(request =>
        {
            requests++;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(
                requests == 1 ? Enumerable.Range(0, 100).Select(_ => Release()).ToArray() : new[] { Release("PwM-v3.4.0_x64_R2R.zip") })) };
        })))
        {
            var pages = await ReleaseCatalog.ReadAsync(client, CancellationToken.None);
            Assert(requests == 2 && pages.Count == 101, "Release pagination");
            Assert(ReleaseCatalog.SelectUpdate(pages, new Version(3, 0, 0), "x64") != null, "Packages on later pages are considered");
        }
        using (var limited = new HttpClient(new Handler(_ => new(HttpStatusCode.Forbidden))))
        {
            bool caught = false;
            try { await ReleaseCatalog.ReadAsync(limited, CancellationToken.None); } catch (HttpRequestException ex) { caught = ex.Message.Contains("limiting"); }
            Assert(caught, "Useful rate limit error");
        }
        byte[] bytes = Encoding.UTF8.GetBytes("verified download");
        using var download = new HttpClient(new Handler(_ => new(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) }));
        string path = Path.Combine(_root, "download.exe");
        await AssetDownloader.DownloadAsync(download, Asset("tool.exe", bytes), path, null, CancellationToken.None);
        Assert(File.ReadAllBytes(path).SequenceEqual(bytes), "Verified download saved");
        var corrupt = Asset("bad.exe", bytes);
        corrupt.Digest = "sha256:" + new string('0', 64);
        string bad = Path.Combine(_root, "bad.exe");
        bool rejected = false;
        try { await AssetDownloader.DownloadAsync(download, corrupt, bad, null, CancellationToken.None); } catch (InvalidDataException) { rejected = true; }
        Assert(rejected && !File.Exists(bad) && !File.Exists(bad + ".partial"), "Corrupt download never becomes executable");
        corrupt.Digest = null;
        rejected = false;
        try { await AssetDownloader.DownloadAsync(download, corrupt, bad, null, CancellationToken.None); } catch (InvalidDataException) { rejected = true; }
        Assert(rejected, "Missing checksum rejected");
        var wrongSize = Asset("tool.exe", bytes);
        wrongSize.Size++;
        rejected = false;
        try { await AssetDownloader.DownloadAsync(download, wrongSize, bad, null, CancellationToken.None); } catch (InvalidDataException) { rejected = true; }
        Assert(rejected, "Size mismatch rejected");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        rejected = false;
        try { await AssetDownloader.DownloadAsync(download, Asset("tool.exe", bytes), bad, null, cancellation.Token); } catch (OperationCanceledException) { rejected = true; }
        Assert(rejected && !File.Exists(bad), "Cancelled download leaves installed files untouched");
    }

    private static async Task LocalCopy()
    {
        string source = Installation("local-copy");
        File.WriteAllText(Path.Combine(source, "PwM.dll"), "app assembly");
        File.WriteAllText(Path.Combine(source, "PwM.Updater.dll"), "bundled installer");
        File.WriteAllText(Path.Combine(source, "PwM.runtimeconfig.json"), "runtime configuration");
        File.WriteAllText(Path.Combine(source, "personal.x"), "encrypted vault");
        File.WriteAllText(Path.Combine(source, "PwM.Json"), "personal vault locations");
        Directory.CreateDirectory(Path.Combine(source, "runtimes", "win-x64", "native"));
        File.WriteAllText(Path.Combine(source, "runtimes", "win-x64", "native", "native.dll"), "native dependency");
        Directory.CreateDirectory(Path.Combine(source, "fr"));
        File.WriteAllText(Path.Combine(source, "fr", "app.resources.dll"), "localized dependency");
        string executable = await LocalUpdater.PrepareAsync(source, CancellationToken.None);
        string copy = Path.GetDirectoryName(executable);
        Assert(!copy.StartsWith(source, StringComparison.OrdinalIgnoreCase), "Updater copy runs outside the installation");
        Assert(File.Exists(executable) && File.ReadAllText(Path.Combine(copy, "PwM.Updater.dll")) == "bundled installer", "Updater code comes from the installed app");
        Assert(File.Exists(Path.Combine(copy, "PwM.runtimeconfig.json")), "Runtime configuration accompanies the private copy");
        Assert(File.Exists(Path.Combine(copy, "runtimes", "win-x64", "native", "native.dll")) && File.Exists(Path.Combine(copy, "fr", "app.resources.dll")), "Native and localized runtime dependencies are copied");
        Assert(!File.Exists(Path.Combine(copy, "personal.x")) && File.ReadAllText(Path.Combine(source, "personal.x")) == "encrypted vault", "Local updater preparation preserves user documents");
        Assert(!File.Exists(Path.Combine(copy, "PwM.Json")), "Vault settings are not copied into the updater runtime");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        bool cancelled = false;
        try { await LocalUpdater.PrepareAsync(source, cancellation.Token); } catch (OperationCanceledException) { cancelled = true; }
        Assert(cancelled, "Local updater preparation can be cancelled");
        Reject(() => UpdateCleanup.RemoveCopy(source), "Cleanup refuses the installed application directory");
        Reject(() => UpdateCleanup.RemoveCopy(Path.Combine(Path.GetTempPath(), "PwM-Updates")), "Cleanup refuses the shared updates root");
        UpdateCleanup.RemoveCopy(copy);
        Assert(!Directory.Exists(copy) && File.Exists(Path.Combine(source, "PwM.exe")), "An unlaunched updater copy is cleaned without touching the installation");
    }

    private static void CleanupRoot()
    {
        string originalTmp = Environment.GetEnvironmentVariable("TMP");
        string originalTemp = Environment.GetEnvironmentVariable("TEMP");
        string isolatedTemp = Path.Combine(_root, "cleanup-temp");
        Directory.CreateDirectory(isolatedTemp);
        try
        {
            Environment.SetEnvironmentVariable("TMP", isolatedTemp);
            Environment.SetEnvironmentVariable("TEMP", isolatedTemp);
            string root = Path.Combine(Path.GetTempPath(), "PwM-Updates");
            Assert(Path.GetDirectoryName(root).Equals(isolatedTemp, StringComparison.OrdinalIgnoreCase), "Root cleanup test uses an isolated temp folder");
            string first = Path.Combine(root, Guid.NewGuid().ToString("N"));
            string second = Path.Combine(root, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(first);
            Directory.CreateDirectory(second);
            File.WriteAllText(Path.Combine(first, "package.zip"), "finished download");
            File.WriteAllText(Path.Combine(second, "PwM.exe"), "another update session");
            UpdateCleanup.RemoveCopy(first);
            Assert(!Directory.Exists(first) && File.Exists(Path.Combine(second, "PwM.exe")), "Cleanup preserves another update session and the shared root");
            UpdateCleanup.RemoveCopy(second);
            Assert(!Directory.Exists(root), "The last update session also removes the empty PwM-Updates root");
            Directory.CreateDirectory(root);
            UpdateCleanup.RemoveCopy(first);
            Assert(!Directory.Exists(root), "Cleanup removes an empty root even if the session folder was already deleted");
        }
        finally
        {
            Environment.SetEnvironmentVariable("TMP", originalTmp);
            Environment.SetEnvironmentVariable("TEMP", originalTemp);
        }
    }

    private static async Task DeferredCleanup()
    {
        string executable = await LocalUpdater.PrepareAsync(Installation("cleanup"), CancellationToken.None);
        string copy = Path.GetDirectoryName(executable);
        string zip = Path.Combine(copy, "package.zip");
        File.WriteAllText(zip, "download");
        File.SetAttributes(zip, FileAttributes.ReadOnly);
        File.WriteAllText(zip + ".partial", "incomplete download");
        string session = "Local\\PwM.CleanupTest." + Guid.NewGuid().ToString("N");
        using var exit = new EventWaitHandle(false, EventResetMode.ManualReset, session);
        var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "Tests.Updater.exe"))
        { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
        start.ArgumentList.Add("--wait-parent");
        start.ArgumentList.Add(session);
        using var owner = Process.Start(start);
        using var cleanup = UpdateCleanup.ScheduleAfterExit(copy, owner, AppContext.BaseDirectory);
        try
        {
            using (var locked = new FileStream(executable, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                await Task.Delay(1500);
                Assert(!owner.HasExited && File.Exists(zip), "Deferred cleanup preserves every file while the updater is running");
                exit.Set();
                Assert(owner.WaitForExit(5000), "Cleanup fixture process exits");
                await Task.Delay(1500);
                Assert(Directory.Exists(copy), "A locked file is retained for a cleanup retry");
            }
            Assert(SpinWait.SpinUntil(() => !Directory.Exists(copy), TimeSpan.FromSeconds(15)),
                "Cleanup retries after exit and removes runtime files, read-only ZIPs and partial downloads");
            Assert(cleanup.WaitForExit(5000) && cleanup.ExitCode == 0, "Cleanup process exits successfully after deleting the temporary folder");
        }
        finally
        {
            exit.Set();
            owner.WaitForExit(5000);
            UpdateCleanup.RemoveCopy(copy);
        }
    }

    private static async Task CompletedCleanup(string workerDirectory = null)
    {
        workerDirectory ??= AppContext.BaseDirectory;
        using var owner = Process.GetCurrentProcess();
        string executable = await LocalUpdater.PrepareAsync(Installation("already-cleaned"), CancellationToken.None);
        string copy = Path.GetDirectoryName(executable);
        UpdateCleanup.RemoveCopy(copy);
        using (var cleanup = UpdateCleanup.ScheduleAfterExit(copy, owner, workerDirectory))
            Assert(cleanup.WaitForExit(5000) && cleanup.ExitCode == 0,
                "An already-removed folder does not keep the cleanup process waiting for a live owner");

        executable = await LocalUpdater.PrepareAsync(Installation("cleaned-while-waiting"), CancellationToken.None);
        copy = Path.GetDirectoryName(executable);
        using var waiting = UpdateCleanup.ScheduleAfterExit(copy, owner, workerDirectory);
        try
        {
            await Task.Delay(1000);
            Assert(!waiting.HasExited && Directory.Exists(copy), "Cleanup waits without deleting a live updater's files");
            UpdateCleanup.RemoveCopy(copy);
            Assert(waiting.WaitForExit(5000) && waiting.ExitCode == 0,
                "Cleanup exits when another cleaner removes the folder while its owner is still alive");
        }
        finally
        {
            UpdateCleanup.RemoveCopy(copy);
            if (!waiting.WaitForExit(5000)) { waiting.Kill(); waiting.WaitForExit(5000); }
        }
    }

    private static async Task MismatchedRuntimeCleanup(string package = null)
    {
        string installation = Installation("legacy-runtime");
        foreach (string extension in new[] { ".exe", ".dll", ".deps.json", ".runtimeconfig.json" })
            File.Copy(Path.Combine(AppContext.BaseDirectory, "PwM.UpdateCleanup" + extension),
                Path.Combine(installation, "PwM.UpdateCleanup" + extension));
        if (package != null)
        {
            using var installer = new PackageInstaller(installation);
            installer.Install(installer.Extract(package, CancellationToken.None), null);
        }
        else
        {
            string hostfxr = Path.GetFullPath(Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(),
                "..", "..", "..", "host", "fxr", Environment.Version.ToString(), "hostfxr.dll"));
            File.Copy(hostfxr, Path.Combine(installation, "hostfxr.dll"));
        }
        string executable = await LocalUpdater.PrepareAsync(Installation("legacy-cleanup-copy"), CancellationToken.None);
        string copy = Path.GetDirectoryName(executable);
        string session = "Local\\PwM.CleanupTest." + Guid.NewGuid().ToString("N");
        using var exit = new EventWaitHandle(false, EventResetMode.ManualReset, session);
        var parentStart = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "Tests.Updater.exe"))
        { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
        parentStart.ArgumentList.Add("--wait-parent");
        parentStart.ArgumentList.Add(session);
        using var parent = Process.Start(parentStart);
        try
        {
            // Reproduce the original failure without allowing the native host to open an error dialog.
            var direct = new ProcessStartInfo(Path.Combine(installation, "PwM.UpdateCleanup.exe"))
            { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardError = true };
            direct.Environment["DOTNET_DISABLE_GUI_ERRORS"] = "1";
            foreach (string argument in new[] { copy, parent.Id.ToString(), parent.StartTime.ToUniversalTime().Ticks.ToString() })
                direct.ArgumentList.Add(argument);
            using (var failed = Process.Start(direct))
            {
                Task<string> error = failed.StandardError.ReadToEndAsync();
                bool exited = failed.WaitForExit(5000);
                if (!exited) { failed.Kill(); failed.WaitForExit(5000); }
                string detail = await error;
                Assert(exited && failed.ExitCode != 0 && detail.Contains("No frameworks were found"),
                    "Reproduced the legacy ZIP's framework-dependent cleanup startup failure");
            }
            using var cleanup = UpdateCleanup.ScheduleAfterExit(copy, parent, installation);
            await Task.Delay(500);
            Assert(!cleanup.HasExited && Directory.Exists(copy), "Cleanup starts successfully beside a bundled runtime and waits for its owner");
            exit.Set();
            Assert(parent.WaitForExit(5000), "Legacy cleanup fixture parent exits");
            Assert(cleanup.WaitForExit(10000) && cleanup.ExitCode == 0, "Cleanup exits successfully with the legacy runtime layout");
            Assert(!Directory.Exists(copy), "Cleanup removes the private updater copy after the legacy ZIP install");
        }
        finally
        {
            exit.Set();
            parent.WaitForExit(5000);
            UpdateCleanup.RemoveCopy(copy);
        }
    }

    private static string Installation(string name)
    {
        string path = Path.Combine(_root, name);
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "PwM.exe"), "old app");
        return path;
    }

    private static string Zip(params (string Name, string Content)[] entries)
    {
        string path = Path.Combine(_root, Guid.NewGuid() + ".zip");
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var entry in entries)
        {
            using var writer = new StreamWriter(zip.CreateEntry(entry.Name).Open());
            writer.Write(entry.Content);
        }
        return path;
    }

    private static void Packages()
    {
        foreach (string protectedFile in new[] { "personal.x", "PwM.Json", "lockedUser", "nested/vault.X" })
        {
            string protectedInstall = Installation("vault-protection-" + Guid.NewGuid());
            string personal = Path.Combine(protectedInstall, protectedFile);
            Directory.CreateDirectory(Path.GetDirectoryName(personal));
            File.WriteAllText(personal, "personal data");
            using var installer = new PackageInstaller(protectedInstall);
            string payload = installer.Extract(Zip(("PwM.exe", "replacement"), (protectedFile, "release data")), CancellationToken.None);
            Reject(() => installer.Install(payload, null), "Release cannot replace vault data: " + protectedFile);
            Assert(File.ReadAllText(personal) == "personal data" && File.ReadAllText(Path.Combine(protectedInstall, "PwM.exe")) == "old app",
                "Vault data rejected before modifying application files");
        }
        string install = Installation("success");
        File.WriteAllText(Path.Combine(install, "my-vault.x"), "encrypted vault");
        using (var installer = new PackageInstaller(install))
        {
            string payload = installer.Extract(Zip(("PwM.exe", "new app"), ("lib/dependency.dll", "new library")), CancellationToken.None);
            installer.Install(payload, null);
            Assert(File.ReadAllText(Path.Combine(install, "PwM.exe")) == "new app", "Application replaced");
            Assert(File.ReadAllText(Path.Combine(install, "lib/dependency.dll")) == "new library", "Dependencies installed");
            Assert(File.ReadAllText(Path.Combine(install, "my-vault.x")) == "encrypted vault", "Personal files preserved");
        }
        Assert(Directory.GetDirectories(install, ".pwm-update-*").Length == 0, "Successful staging and backup cleaned");
        using (var installer = new PackageInstaller(Installation("wrapped")))
            Assert(Path.GetFileName(installer.Extract(Zip(("PwM-v3.4.0/PwM.exe", "app")), CancellationToken.None)) == "PwM-v3.4.0", "Single wrapper folder supported");
        foreach (string entry in new[] { "../escape.exe", "nested/../../escape.exe", "C:/escape.exe", "/escape.exe", "lib/file:stream", "CON.txt", "lib./file", "nested\\..\\escape.exe" })
        {
            using var installer = new PackageInstaller(Installation("unsafe-" + Guid.NewGuid()));
            Reject(() => installer.Extract(Zip(("PwM.exe", "app"), (entry, "bad")), CancellationToken.None), "Unsafe ZIP path rejected: " + entry);
        }
        using (var installer = new PackageInstaller(Installation("duplicate")))
            Reject(() => installer.Extract(Zip(("PwM.exe", "app"), ("pwm.EXE", "other")), CancellationToken.None), "Case-insensitive duplicate rejected");
        using (var installer = new PackageInstaller(Installation("missing")))
            Reject(() => installer.Extract(Zip(("readme.txt", "no executable")), CancellationToken.None), "Missing application rejected");
        string payloadCheck = Installation("payload-check");
        File.Copy(typeof(Program).Assembly.Location, Path.Combine(payloadCheck, "PwM.dll"));
        File.WriteAllText(Path.Combine(payloadCheck, "PwM.runtimeconfig.json"), "{}");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Tests.Updater.exe"), Path.Combine(payloadCheck, "PwM.exe"), true);
        string architecture = "x64";
        var options = new UpdateOptions(payloadCheck, 1, 1, "unused", new Version(3, 2, 2, 1), new Version(3, 2, 3, 1), architecture);
        PackageInstaller.ValidatePayload(payloadCheck, options);
        Assert(true, "Application payload version and architecture accepted");
        Reject(() => PackageInstaller.ValidatePayload(payloadCheck, options with { Architecture = architecture == "x64" ? "x86" : "x64" }), "Wrong executable architecture rejected");
        string payloadExe = Path.Combine(payloadCheck, "PwM.exe");
        byte[] header = File.ReadAllBytes(payloadExe);
        int machineOffset = BitConverter.ToInt32(header, 0x3c) + 4;
        header[machineOffset] = 0x4c;
        header[machineOffset + 1] = 0x01;
        File.WriteAllBytes(payloadExe, header);
        Reject(() => PackageInstaller.ValidatePayload(payloadCheck, options), "An x86 executable disguised in an x64 ZIP is rejected");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Tests.Updater.exe"), payloadExe, true);
        Reject(() => PackageInstaller.ValidatePayload(payloadCheck, options with { Version = new Version(3, 2, 4, 0) }), "Mislabeled application version rejected");
        File.Delete(Path.Combine(payloadCheck, "PwM.runtimeconfig.json"));
        Reject(() => PackageInstaller.ValidatePayload(payloadCheck, options), "Incomplete runtime payload rejected");
        string rollback = Installation("rollback");
        File.WriteAllText(Path.Combine(rollback, "z-locked.dll"), "original library");
        using (var installer = new PackageInstaller(rollback))
        {
            string payload = installer.Extract(Zip(("PwM.exe", "replacement"), ("a-new.dll", "new"), ("z-locked.dll", "replacement")), CancellationToken.None);
            using (var locked = new FileStream(Path.Combine(rollback, "z-locked.dll"), FileMode.Open, FileAccess.Read, FileShare.None))
                Reject(() => installer.Install(payload, null), "Locked file fails installation");
            Assert(File.ReadAllText(Path.Combine(rollback, "PwM.exe")) == "old app", "Original app restored after later-file failure");
            Assert(File.ReadAllText(Path.Combine(rollback, "z-locked.dll")) == "original library", "Locked original preserved");
            Assert(!File.Exists(Path.Combine(rollback, "a-new.dll")), "Newly added file removed during rollback");
        }
        string blocked = Installation("blocked");
        Directory.CreateDirectory(Path.Combine(blocked, "dependency.dll"));
        using (var installer = new PackageInstaller(blocked))
        {
            string payload = installer.Extract(Zip(("PwM.exe", "new"), ("dependency.dll", "new")), CancellationToken.None);
            Reject(() => installer.Install(payload, null), "Folder/file conflict rejected before installation");
            Assert(File.ReadAllText(Path.Combine(blocked, "PwM.exe")) == "old app", "Preflight leaves application untouched");
        }
    }

    private static void RenderWindow()
    {
        ApplicationConfiguration.Initialize();
        using var window = new UpdateWindow();
        window.ShowRelease(new Version(3, 2, 3, 1), new(new Version(3, 2, 4, 0), "x64", null,
            "What’s new in PwM\r\n\r\n• Faster vault loading\r\n• Improvements to credential management\r\n• Stability fixes and a smoother experience"));
        window.Status.Text = "Ready when you are";
        window.StartPosition = FormStartPosition.Manual;
        window.Location = new Point(-10000, -10000);
        window.ShowInTaskbar = false;
        window.Show();
        Application.DoEvents();
        window.PerformLayout();
        using var bitmap = new Bitmap(window.Width, window.Height);
        window.DrawToBitmap(bitmap, new Rectangle(Point.Empty, window.Size));
        bitmap.Save(Path.Combine(_root, "update-prompt.png"));
        Assert(window.Primary.Bounds.Width > 100 && window.Notes.Bounds.Height > 60, "Update window has usable button and release note bounds");
        window.Heading.Text = "Updating PwM";
        window.Primary.Visible = false;
        window.Secondary.Text = "Cancel";
        window.ShowProgress("2 / 4   Downloading PwM", 64, "12.4 MB of 19.4 MB");
        window.DrawToBitmap(bitmap, new Rectangle(Point.Empty, window.Size));
        bitmap.Save(Path.Combine(_root, "update-progress.png"));
        window.ClientSize = new Size(614, 531);
        window.PerformLayout();
        Assert(window.Notes.Height > 60 && window.Secondary.Bottom <= window.Secondary.Parent.ClientSize.Height,
            "Notes and actions fit the minimum window size");
    }

    private static void Handoff(bool cancel, string packageVersion = "3.2.3")
    {
        string install = Installation((cancel ? "handoff-cancel-" : "handoff-install-") + packageVersion);
        foreach (string extension in new[] { ".exe", ".dll", ".deps.json", ".runtimeconfig.json" })
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Tests.Updater" + extension), Path.Combine(install, "Tests.Updater" + extension));
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Tests.Updater.exe"), Path.Combine(install, "PwM.exe"), true);
        File.WriteAllText(Path.Combine(install, ".updater-fixture"), "Disposable test installation");
        string zipPath = Path.Combine(_root, Guid.NewGuid() + ".zip");
        using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            zip.CreateEntryFromFile(Path.Combine(install, "PwM.exe"), "PwM.exe");
            zip.CreateEntryFromFile(typeof(Program).Assembly.Location, "PwM.dll");
            zip.CreateEntryFromFile(Path.Combine(install, "Tests.Updater.runtimeconfig.json"), "PwM.runtimeconfig.json");
            using var writer = new StreamWriter(zip.CreateEntry("updated.txt").Open());
            writer.Write("installed");
        }
        byte[] bytes = File.ReadAllBytes(zipPath);
        string architecture = "x64";
        var release = new GitHubRelease { Assets = new() { Asset($"PwM-v{packageVersion}_{architecture}_R2R.zip", bytes) } };
        string session = "Local\\PwM.Update." + Guid.NewGuid().ToString("N");
        using var ready = new EventWaitHandle(false, EventResetMode.ManualReset, session + ".ready");
        using var cancelled = new EventWaitHandle(false, EventResetMode.ManualReset, session + ".cancel");
        using var exit = new EventWaitHandle(false, EventResetMode.ManualReset, session + ".exit");
        var start = new ProcessStartInfo(Path.Combine(install, "PwM.exe"))
        { WorkingDirectory = install, UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true };
        start.ArgumentList.Add("--wait-parent");
        start.ArgumentList.Add(session + ".exit");
        using var parent = Process.Start(start);
        var options = new UpdateOptions(install, parent.Id, parent.StartTime.ToUniversalTime().Ticks,
            session, new Version(3, 2, 2, 1), ReleaseCatalog.Normalize(Version.Parse(packageVersion)), architecture);
        using var window = new UpdaterForm(options, () => new HttpClient(new Handler(request =>
            new(HttpStatusCode.OK) { Content = request.RequestUri.Host == "api.github.com"
                ? new StringContent(JsonSerializer.Serialize(new[] { release })) : new ByteArrayContent(bytes) })))
        { StartPosition = FormStartPosition.Manual, Location = new Point(-10000, -10000), ShowInTaskbar = false };
        var clock = Stopwatch.StartNew();
        bool released = false;
        Exception failure = null;
        using var monitor = new System.Windows.Forms.Timer { Interval = 50 };
        monitor.Tick += (_, _) =>
        {
            try
            {
                if (clock.Elapsed > TimeSpan.FromSeconds(25)) throw new Exception("Updater handoff timed out: " + window.Notes.Text);
                if (!released && window.Status.Text == "Waiting for PwM to close")
                {
                    Assert(ready.WaitOne(0), "Updater reports readiness to the parent");
                    Assert(!parent.HasExited && !File.Exists(Path.Combine(install, "updated.txt")), "No installed files change while the parent is running");
                    released = true;
                    if (cancel) cancelled.Set(); else exit.Set();
                }
                if (window.Heading.Text == "Couldn’t finish the update") throw new Exception(window.Notes.Text);
                if (cancel && window.Heading.Text == "Update cancelled") window.Close();
            }
            catch (Exception ex) { failure = ex; cancelled.Set(); exit.Set(); window.Close(); }
        };
        try
        {
            monitor.Start();
            Application.Run(window);
            if (failure != null) throw failure;
            Assert(released, "The updater reached the parent handoff");
            if (cancel)
                Assert(!parent.HasExited && !File.Exists(Path.Combine(install, "updated.txt")), "Cancelling the close leaves the parent and installation intact");
            else
            {
                Assert(parent.HasExited && File.ReadAllText(Path.Combine(install, "updated.txt")) == "installed", "Update installed only after the parent exited");
                Assert(SpinWait.SpinUntil(() => File.Exists(Path.Combine(install, ".restarted")), TimeSpan.FromSeconds(5)), "PwM restarts after a successful update");
            }
        }
        finally
        {
            monitor.Stop();
            exit.Set();
            Assert(parent.WaitForExit(5000), "Disposable parent exits cleanly");
        }
    }

    private static void BundledStartup(string applicationDirectory, bool terminate = false)
    {
        string executable = LocalUpdater.PrepareAsync(applicationDirectory, CancellationToken.None).GetAwaiter().GetResult();
        string copy = Path.GetDirectoryName(executable);
        string install = Installation(terminate ? "bundled-terminated-parent" : "bundled-parent");
        foreach (string extension in new[] { ".exe", ".dll", ".deps.json", ".runtimeconfig.json" })
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Tests.Updater" + extension), Path.Combine(install, "Tests.Updater" + extension));
        File.Copy(Path.Combine(install, "Tests.Updater.exe"), Path.Combine(install, "PwM.exe"), true);
        string session = "Local\\PwM.Update." + Guid.NewGuid().ToString("N");
        using var ready = new EventWaitHandle(false, EventResetMode.ManualReset, session + ".ready");
        using var cancelled = new EventWaitHandle(true, EventResetMode.ManualReset, session + ".cancel");
        using var exit = new EventWaitHandle(false, EventResetMode.ManualReset, session + ".exit");
        using var parentReady = new EventWaitHandle(false, EventResetMode.ManualReset, session + ".exit.started");
        var parentStart = new ProcessStartInfo(Path.Combine(install, "PwM.exe"))
        { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
        parentStart.ArgumentList.Add("--wait-parent");
        parentStart.ArgumentList.Add(session + ".exit");
        using var parent = Process.Start(parentStart);
        Assert(parentReady.WaitOne(TimeSpan.FromSeconds(10)), "Disposable parent starts using its installed framework");
        // Stage the self-contained cleaner's runtime after the framework-dependent fixture has started.
        // Otherwise local hostfxr would redirect that fixture away from the installed shared framework.
        foreach (string file in Directory.EnumerateFiles(applicationDirectory)
            .Where(file => Path.GetFileName(file) != "PwM.exe" && Path.GetFileName(file) != "PwM.dll"))
            File.Copy(file, Path.Combine(install, Path.GetFileName(file)));
        var start = new ProcessStartInfo(executable)
        { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(executable), WindowStyle = ProcessWindowStyle.Hidden };
        foreach (string argument in new[] { "--apply-update", "--install-dir", install, "--parent-pid", parent.Id.ToString(),
            "--parent-start", parent.StartTime.ToUniversalTime().Ticks.ToString(), "--session", session,
            "--current-version", "3.2.2.1", "--version", "3.2.3", "--arch", "x64" })
            start.ArgumentList.Add(argument);
        using var updater = Process.Start(start);
        // Match the app's C# exit observer for abnormal termination while the parent stays open.
        if (terminate) _ = UpdateCleanup.ObserveExitAsync(copy, updater.Id);
        try
        {
            Assert(ready.WaitOne(TimeSpan.FromSeconds(20)), "The copied PwM executable starts its bundled installer before vault startup");
            Assert(Directory.Exists(copy), "The real updater retains its private copy while running");
            File.WriteAllText(Path.Combine(copy, "cleanup-test.zip.partial"), "interrupted download fixture");
            if (terminate) { updater.Kill(); updater.WaitForExit(5000); }
            var timer = Stopwatch.StartNew();
            while (!updater.HasExited && timer.Elapsed < TimeSpan.FromSeconds(5))
            {
                // The smoke test starts hidden, so Process.MainWindowHandle can be zero.
                EnumWindows((handle, _) =>
                {
                    GetWindowThreadProcessId(handle, out uint processId);
                    if (processId == updater.Id) PostMessage(handle, 0x0010, IntPtr.Zero, IntPtr.Zero);
                    return true;
                }, IntPtr.Zero);
                Thread.Sleep(100);
            }
            Assert(updater.HasExited && (terminate || updater.ExitCode == 0),
                "The real bundled installer closes cleanly after cancellation: " + (updater.HasExited ? updater.ExitCode.ToString() : "still running"));
            Assert(!parent.HasExited && !File.Exists(Path.Combine(install, "PwM.dll")), "Bundled installer cancellation preserves the running installation");
            Assert(SpinWait.SpinUntil(() => !Directory.Exists(copy), TimeSpan.FromSeconds(15)),
                terminate ? "The real updater copy and partial download are cleaned after process termination"
                    : "The real updater copy and partial download are cleaned after cancellation and exit");
        }
        finally
        {
            exit.Set();
            parent.WaitForExit(5000);
            if (!updater.HasExited) { updater.Kill(); updater.WaitForExit(5000); }
            UpdateCleanup.RemoveCopy(copy);
        }
    }

    private delegate bool WindowCallback(IntPtr handle, IntPtr parameter);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool EnumWindows(WindowCallback callback, IntPtr parameter);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);
    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "PostMessageW")]
    private static extern bool PostMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> handle) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(handle(request));
        }
    }
}
