<p align="center">
  <img src="https://github.com/0x78654C/PwM/blob/main/Media/logo.png" width=150>
</p>

# PwM
Simple password manager in C# WPF  to store locally sensitive authentication data for a specific application. 
The ideea of creation for this Password Manager came from the simple fact to use something simple and fast.   
PwM is still a work in progress and will be more features added to it.


# How it works
For password hash is used Argon2 (argon2id) https://en.wikipedia.org/wiki/Argon2. And for encryption is used Rijndael AES-256.
The Passwowrd Manager generates a vault file for every user logged in system. You cannot see the vaults from other user on that machine.

## Requirements:

.NET Core 3.1

.NET Standard 2.1

.NET Framework 4.7.2

## Samples

![alt text](https://github.com/0x78654C/PwM/blob/main/Media/1v.jpg?raw=true)


![alt text](https://github.com/0x78654C/PwM/blob/main/Media/1.jpg?raw=true)


![alt text](https://github.com/0x78654C/PwM/blob/main/Media/2.jpg?raw=true)

```diff
Disclaimer: Use it at your own risk. I don't take any responsibility if password was leaked, cracked or any other form of extraction.
```
