# Security review — 2026-09-12

Reviewed the shared encryption, password generation, vault paths and reads, desktop and mobile session handling, clipboard handling, breach requests, and resolved NuGet dependencies. This is a source review with regression testing; it is not a penetration test or a guarantee that no other vulnerabilities exist.

## Fixed findings

| Severity | Finding | Change |
| --- | --- | --- |
| High | Mobile inactivity locking waited for acknowledgement, retained searchable credentials, and allowed delayed unlock/password-change work to revive a session after backgrounding. | Lock immediately, invalidate pending work using session versions, clear cached credentials and input fields, cancel outstanding breach checks, and require an active session for credential actions. Password display/prompt pages clear and dismiss on lock. |
| High | Desktop and mobile queried breach hashes during password entry. A sequence of partial-password prefixes can reveal the password. | Desktop checks on save; mobile new-password checks require the explicit button. Removed incremental requests and the mobile cache of complete, unsalted password hashes. This follows [HIBP's incremental-search guidance](https://haveibeenpwned.com/API/v3#PwnedPasswordsIncrementalSearching). |
| Medium | Desktop system-event callbacks accessed WPF controls from a different thread; static event subscriptions also kept closed password windows alive. | Dispatch callbacks onto the UI thread and unsubscribe when windows close. Dispose cached secure strings and close revealed-password notices when locking. |
| Medium | Locking left copied passwords on the clipboard until the timer ran. | Attempt immediate clearing on desktop and mobile lock, only if the clipboard still contains the copied password; retain timed clearing. |
| Medium | Mobile treated padded, zero-count HIBP entries as breaches and failed requests as clean results. | Validate response lines/counts, ignore zero-count matches, and show an unavailable status for failures. Requests have response/time limits and do not follow redirects. See [HIBP padding](https://haveibeenpwned.com/API/v3#PwnedPasswordsPadding). |
| Medium | Vault files and mobile import streams were loaded without enforcing the encryption layer's size limit first. | Bound file reads and imports to 128 MiB, including streaming reads, and reject declared oversized input before reading its contents. |
| Medium | Mobile silently skipped malformed credential records, so later updates/deletions could permanently discard them. | Reject malformed vault contents before allowing edits. Regression tests verify the encrypted file remains unchanged. |
| Medium | Android permitted screenshots and screen-capture previews of sensitive pages. | Set the activity's secure-window flag and disable cleartext traffic in the manifest. See [Android's secure-activity guidance](https://developer.android.com/security/fraud-prevention/activities). |

## Validation

- 69 offline tests passed, including 21 new regression cases for stale sessions, cleared credentials, HIBP responses/cancellation, oversized imports, and malformed records.
- Desktop and CLI builds passed. Existing desktop/test warnings remain unrelated to these changes.
- Shared mobile C# compiled against real MAUI 10.0.0 APIs in a temporary project under `PwM.Mobile/obj/SecurityCompile`. That project supplies generated field declarations for XAML pages; it does not validate XAML behavior or native platform code.
- NuGet reported no known vulnerable packages in the audited desktop/test graphs and the temporary mobile reference graph, including transitive dependencies. The mobile reference graph uses MAUI 10.0.0 explicitly; the native application's `$(MauiVersion)` depends on the installed workload and was not resolved here.
- Native mobile builds could not run because the required .NET 10 MAUI workloads are absent. Android/iOS device checks, screenshot protection, lifecycle navigation, and OS clipboard behavior still need validation on devices. Existing live-network and OS-clipboard tests were excluded from the offline run.

To repeat the offline regression run:

```powershell
dotnet test PwM.Tests/PwM.Tests.csproj --filter "FullyQualifiedName!~PwM.Tests.HIPB.HIBPTest&FullyQualifiedName!~NetworkTest&FullyQualifiedName!~ClipboardClearTest"
```

## Compatibility and limits

Current vaults already use AES-256-GCM with random salts/nonces and authenticated Argon2 parameters. This review preserves that format and legacy read support. Legacy CBC vaults contain a password-keyed MAC that permits cheaper offline guessing than the Argon2 encryption key. Saving a legacy vault or changing its master password writes the current format; old copies/backups retain their original protection. No personal vault files were opened or migrated during this review.

Clearing application references and disposing `SecureString` reduces retention; immutable .NET strings cannot be reliably wiped. Mobile operating systems may deny clipboard access while backgrounded. Native lifecycle behavior and device protections require the device validation described above.
