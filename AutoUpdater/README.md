# PwM ZIP updates

PwM checks six seconds after its main window appears and through **⋮ → Check for updates…**. Automatic network errors are silent; manual checks report errors. Installing always requires clicking **Update & restart**.

Only uploaded stable-release assets from <https://github.com/0x78654C/PwM/releases> matching `PwM-v<version>_x64_R2R.zip` are accepted, for example `PwM-v1.2.3_x64_R2R.zip` or `PwM-v1.2.3.1_x64_R2R.zip`. The highest numeric filename version newer than the installed assembly wins. Three-part versions have a zero revision. Android APKs, installers, source archives, other architectures, drafts and prereleases are ignored. The release tag does not determine the desktop version.

After confirmation, PwM copies its runtime to `%TEMP%\PwM-Updates\<unique-id>` and launches that copy with `--apply-update`, before WPF, vault or single-instance initialization. The installer is bundled as `PwM.Updater.dll`; no separate updater release asset is needed. It downloads the selected ZIP, checks GitHub's SHA-256 digest and byte count, extracts safely, and validates the contained assembly version and x64 executable. The installed app closes through its normal close path, which clears its clipboard password, and restarts after a successful update.

Files replaced by the package are backed up and restored on caught installation errors. Vaults and registry/AppData settings are preserved. Packages containing `.x` vaults, `PwM.Json` or `lockedUser` are rejected before replacement. The install directory must be writable and other processes using its files must be closed. Cancelling before installation preserves the existing files. A failed rollback retains its `.pwm-update-<unique-id>\backup` directory and reports that path; a power loss during replacement may require manual recovery.

Staging and successful rollback files are removed from the installation. The bundled `PwM.UpdateCleanup.exe` waits up to one minute for the updater to exit, then deletes its private temporary runtime and downloads, retrying locked files for up to one minute. It exits immediately after cleanup, or if the folder has already been removed. If the updater remains running past the wait limit, the worker exits and leaves its files in place. The running app also cleans an updater copy after abnormal exit. The cleanup worker and its dependencies belong inside the application ZIP.

The cleanup worker inherits the desktop app's self-contained/runtime settings when publishing. When a legacy ZIP without a cleanup worker leaves an existing framework-dependent worker beside a bundled runtime, the updater launches that worker through its current shared .NET host. This avoids a native .NET startup error dialog keeping the cleanup process alive.

After the last temporary update copy is removed, cleanup also removes the empty `%TEMP%\PwM-Updates` folder. Other update sessions are preserved. Copies left by older failed cleanup attempts are not automatically deleted by a later session.

## Build a release

On Windows with the .NET 10 SDK:

```powershell
pwsh -NoProfile -File PublishRelease.ps1
```

This publishes a self-contained Windows x64 ReadyToRun application and creates `artifacts/releases/PwM-v<version>_x64_R2R.zip`, including the installer and cleanup worker. The version comes from `PwM/Properties/AssemblyInfo.cs`; a zero revision is omitted from the ZIP name. Set the assembly and file version for each new release before publishing.

When packaging manually, include the complete publish output at the ZIP root or inside one enclosing directory. The assembly's major/minor/build must match the ZIP version; a nonzero fourth filename component must also match. GitHub must provide a positive size and SHA-256 digest after upload. Existing builds without this updater need one manual upgrade to a build containing it.

## Validation

```powershell
dotnet run --project Tests.Updater/Tests.Updater.csproj -- --application PwM/bin/Debug/net10.0-windows
dotnet test PwM.Tests/PwM.Tests.csproj
```

Updater tests use fake HTTP responses and disposable installation folders/processes. They cover release selection, checksums, cancellation, ZIP paths, personal-file protection, payload version and architecture, rollback, restart, and deferred cleanup. The optional `--application` argument also exercises the actual PwM executable's updater entry point. UI renders and test fixtures are written under `artifacts/updater-tests`.
