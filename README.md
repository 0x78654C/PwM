<p align="center">
  <img src="https://github.com/0x78654C/PwM/blob/main/Media/logo.png" width="120">
</p>

<h1 align="center">PwM — Password Manager</h1>

<p align="center">
  A simple, fully <strong>offline</strong> password manager for Windows (WPF), Android/iOS (.NET MAUI), and Linux/Windows (CLI).<br/>
  No cloud. No telemetry. Your data stays on your machine.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white"/>
  <img src="https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20Android%20%7C%20iOS-0078D6"/>
  <img src="https://img.shields.io/badge/mobile-.NET%20MAUI-512BD4?logo=dotnet&logoColor=white"/>
  <img src="https://img.shields.io/badge/encryption-AES--256%20%2B%20Argon2id-4F46E5"/>
  <img src="https://img.shields.io/github/license/0x78654C/PwM"/>
</p>

<p align="center">
  <img src="Media/1v.png" width="820" alt="PwM desktop vault list in dark theme"/>
</p>

---

## Features

### 🗄️ Vault management
- Create and delete local encrypted vaults
- Import / export vaults (`.x` file format)
- **Shared vaults on Windows desktop** — link to a vault on a network share or file server
- Change master password for any vault

### 🔑 Accounts & applications
- Add / delete application entries inside a vault
- Add / delete account credentials per application
- Update account passwords
- **Show password** temporarily (hold right-click on the eye icon)
- **Copy to clipboard** for 15 seconds, then auto-cleared
  > If something else is copied within those 15 s, PwM will not clear the clipboard on expiry
- Search vaults, applications, and accounts on mobile

### 🛡️ Security
- **Argon2id** key derivation with fully configurable parameters (see Settings)
- **AES-256 (Rijndael)** vault encryption
- Master password **re-prompt every 30 minutes** on any write action inside an open vault
- **Auto-lock** open vault after configurable inactivity period (default 10 min)
- Lock vault automatically on Windows lock screen or system suspend
- Password breach check via [HaveIBeenPwned](https://haveibeenpwned.com/) — only a SHA-1 prefix is sent (k-anonymity model)
- Built-in **strong password generator**
- Password complexity enforced on creation (≥ 12 chars, upper + lower + digit + special, no spaces)

### 🎨 Appearance
- New **dark theme** for the Windows desktop app
- Mobile theme options: **System**, **Light**, and **Dark**

### ⚙️ Settings
Settings persist across sessions. The Windows desktop app stores them under `HKCU\SOFTWARE\PwM`; the mobile app uses platform-native app preferences.

| Setting | Default | Range | Description |
|---|---|---|---|
| Vault session timeout | **10 min** | — | Auto-lock an inactive open vault |
| Argon2 iterations | **40** | 10 – 200 | Passes over memory (time cost) |
| Argon2 memory size | **4096 KB** | 4096 – 1 048 576 KB | RAM consumed per hash |
| Argon2 parallelism | **2** | 1 – 16 | Parallel threads during hashing |
| Theme | **Light** (desktop) / **System** (mobile) | Light / Dark / System¹ | Application appearance |

¹ The Windows desktop app supports Light and Dark. The mobile app also supports following the system theme.

> ⚠️ Changing Argon2 parameters affects **all vaults**. Re-create your vaults after applying new values.

---

## Desktop app

### Vaults

| Action | How |
|---|---|
| **Open** | Double-click the vault name, then enter your master password |
| **Create** | Click **New vault** at the bottom of the vault list → enter vault name, master password, and confirm master password |
| **Delete / Remove** | Select vault → click **Remove** at the bottom, or right-click → *Delete or remove vault*<br/>Shared vault files are only unlinked from the list, not deleted from disk |
| **Import** | Click **Import** at the bottom → choose *Local* or *Shared* → select a `.x` vault file |
| **Export** | Right-click vault → *Export vault* → choose save location |
| **Change master password** | Right-click vault → *Change master password* |

### Credentials (Applications tab)

First open a vault by double-clicking it. The sidebar switches to **Applications**.

| Action | How |
|---|---|
| **Add credential** | Click **New credential** at the bottom → enter application name, account (username/email), and password.<br/>Use the ✨ wand icon to generate a strong password, or the 👁 eye icon (hold right-click) to reveal what you typed |
| **Delete account** | Select account → click **Delete** at the bottom, or right-click → *Delete account* |
| **Update password** | Right-click account → *Update account password* |
| **Show password** | Right-click account → *Show password* |
| **Copy to clipboard** | Right-click account → *Copy password for 15 seconds* |
| **Lock vault** | Click **Lock vault** (top-right of the Applications tab) |

> ⚠️ A breach warning is shown automatically when adding a credential whose password appears in a known data breach (powered by HaveIBeenPwned).

> Linked shared/network vaults are available only in the Windows desktop app. Mobile and CLI support local vaults.

---

## Mobile app

The mobile version is built with **.NET MAUI** for Android and iOS and uses the same `.x` vault format and encryption library as the desktop app.

- Create, open, delete, import, and export/share local vaults
- Add, search, update, show, copy, and delete credentials
- Change vault master passwords
- Automatic password breach checks
- Auto-lock after inactivity and whenever the app is backgrounded
- System, light, and dark themes
- Swipe vaults and credentials to reveal additional actions

> Linked shared/network vaults are not supported on mobile because Android and iOS do not provide the persistent paths and desktop file-locking behavior they require.

---

## CLI usage

```
pwm_cli COMMAND
```

| Command | Description |
|---|---|
| `-h` | Show help |
| `-createv` | Create a new vault |
| `-delv` | Delete a vault |
| `-listv` | List all vaults |
| `-addapp` | Add an application/account to a vault |
| `-dela` | Delete an account from a vault |
| `-updatea` | Update an account password |
| `-lista` | List accounts in a vault |

**Master password requirements:**
```
At least 12 characters — must include uppercase, lowercase, digit, special character, no spaces
```

---

## Encryption

| Layer | Details |
|---|---|
| Key derivation | [Argon2id](https://en.wikipedia.org/wiki/Argon2) — iterations, memory size and parallelism configurable in Settings |
| Vault encryption | Rijndael AES-256 |
| Storage | `%LOCALAPPDATA%\PwM\` on Windows; platform app-data storage on Android and iOS |

---

## Requirements

- **.NET 10** Runtime for desktop and CLI
- Windows 10 or later for the WPF desktop app
- Android 8.0 / API 26 or later
- iOS 15.0 or later
- Windows or Linux for the CLI

### Build the mobile app

Install the .NET 10 SDK and .NET MAUI workload, then build the desired target:

```bash
dotnet workload install maui
dotnet build PwM.Mobile/PwM.Mobile.csproj -f net10.0-android
dotnet build PwM.Mobile/PwM.Mobile.csproj -f net10.0-ios
```

> Building and signing the iOS app requires macOS with Xcode.

---

## Screenshots

### Desktop dark theme

![Create vault in dark theme](Media/1.png)

![Application view in dark theme](Media/2.png)

![Desktop dark theme settings](Media/settings_dark.png)

### Mobile (.NET MAUI)

<p align="center">
  <img src="Media/mobile_main.png" width="200" alt="Mobile vault list"/>
  <img src="Media/mobile_apps.png" width="200" alt="Mobile credentials list"/>
  <img src="Media/mobile_settings.png" width="200" alt="Mobile settings"/>
  <img src="Media/mobile_update.png" width="200" alt="Mobile update password dialog"/>
</p>

---

## License

[LICENSE](LICENSE) © 0x78654C

