<p align="center">
  <img src="https://github.com/0x78654C/PwM/blob/main/Media/logo.png" width=150>
</p>

# PwM
Simple offline password manager for Windows in C# WPF  to store locally sensitive authentication data for a specific application. 
The ideea of creation for this Password Manager came from the simple fact to use something simple and fast.   

# Features

 - Create/Delete vaults
 - Import/Export vaults
 - Change Master Password for vaults
 - Add/delete account for applications in vault
 - Update application account passwords
 - Exit logged vault on entering lock screen event or suspend.
 - Generate strong passwords.
 - Show password temporary when entered on password boxes. Right click on eye symbol.
 - Copy password from applications to clipboard for a duration of 15 seconds.
   (If in the 15 seconds interval is copied something else on clipboard, PwM will not clear the clipboard whe time expired or app is closed) 
 - After log in vault, master password required window will be prompted every 30 minutes if a action is made. Example: update password, delete account , etc.

# How it works

  Vaults:
  - Creating a vault: press on '+' sign down bellow and you will be prompted for vault name and master password. Every vault is created in Windows user profile.
  - Delete a vault: select the vault that you want to delete and press on '-' sign down bellow or right click on vault and choose 'Delete vault'
  - Import vault: press on 'I' letter down bellow and a file dialog will be opened for selecting the vault file. Usually the file extensions is '.x'
  - Export vault: right click on the vault name from list that you want to export and choose 'Export vault'. You will be prompted with a file save dialog.
  - Change Master Password: right click on the vault name from list that you want to change the password and choose 'Change Master Password'.
 
  Applications:
  - Add application: press on '+' sign down bellow and you will be prompted with a window for adding application name, account name and password.
  - Delete account: select the account from specific application that you want to delete and press on '-' sign down bellow or right click on application/account and choose 'Delete Account'
  - Update account password: right click on account and choose update 'Update account password'. You will be prompted with a pop window to enter new password for account.
  - Show password: right click on account and choose update 'Show password'. Password will be visible on application list.
  - Copy to Clipboard: right click on account and choose update 'Copy to clipboard (15 seconds available)'. Password will be copied on clipboard for 15 seconds.

# Encryption

For password hash is used Argon2 (argon2id) https://en.wikipedia.org/wiki/Argon2. And for encryption is used Rijndael AES-256.
The Passwowrd Manager generates a vault file for every user logged in system. You cannot see the vaults from other user on that machine.

## Requirements:

.NET Framework 4.7.2

## Samples

![alt text](https://github.com/0x78654C/PwM/blob/main/Media/1v.jpg?raw=true)


![alt text](https://github.com/0x78654C/PwM/blob/main/Media/1.jpg?raw=true)


![alt text](https://github.com/0x78654C/PwM/blob/main/Media/2.jpg?raw=true)
