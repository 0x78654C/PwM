using PwMLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using static PwM.Utils.UI;
using PasswordValidator = PwMLib.PasswordValidator;

namespace PwM
{
    static class MainClass
    {
        /* PwM - Command line interface*/

        private static int s_tries = 0;
        private static string s_vaultsDir;
        private static readonly string s_helpMessage = $@"PwM Copyright @ 2020-2022 0x078654c
PwM - A simple password manager to store localy the authentification data encrypted for a application using Rijndael AES-256 and Argon2 for password hash.
Contact: xcoding.dev@gmail.com

[x] Usage of Password Manager commands:
  -h       : Display this message.
  -createv : Create a new vault.
  -delv    : Deletes an existing vault.
  -listv   : Displays the current vaults.
  -addapp  : Adds a new application to vault.
  -dela    : Deletes an existing application in a vault.
  -updatea : Updates account's password for an application in a vault.
  -lista   : Displays the existing applications in a vault.

[x] Support:
If you like this application and want to support the project you can always buy me a crypto coffee :D. Thank you very much for your support, I appreciate it!
    
    Bitcoin:   bc1qe6z79u5f62c3v5lh6nt3hfcj683etyx65s5nch
    Ethereum:  0x7Aedd83d94b350624f4FDb4dF7eC3a2A7caA9952
";

        /// <summary>
        /// The entry point of the program, where the program control starts and ends.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        public static void Main(string[] args)
        {
            s_vaultsDir = CheckOSType();

            if (!Directory.Exists(s_vaultsDir))
            {
                Directory.CreateDirectory(s_vaultsDir);
            }
            try
            {
                if (args.Contains("-h"))
                    Console.WriteLine(s_helpMessage);
                if (args.Contains("-createv"))
                    CreateVault();
                if (args.Contains("-delv"))
                    DeleteVault();
                if (args.Contains("-dela"))
                    DeleteAppUserData();
                if (args.Contains("-listv"))
                    ListVaults();
                if (args.Contains("-addapp"))
                    AddPasswords();
                if (args.Contains("-lista"))
                    ReadPass();
                if (args.Contains("-updatea"))
                    UpdateAppUserData();
            }
            catch (FileNotFoundException)
            {
                ErrorWriteLine("Vault was not found. Check command!");
            }
            catch (Exception e)
            {
                ErrorWriteLine(e.Message + ". Check command!");
            }
        }

        /// <summary>
        /// Check OS type
        /// </summary>
        private static string CheckOSType()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string rootPath = Path.GetPathRoot(Environment.SystemDirectory);
                string accountName = Environment.UserName;
                return $"{rootPath}Users\\{accountName}\\AppData\\Local\\PwM\\";
            }
            var vaultPath = AppDomain.CurrentDomain.BaseDirectory + "Vaults";
            return vaultPath;
        }

        /// <summary>
        /// Check maximum of tries. Used in while loops for exit them at a certain count.
        /// </summary>
        /// <returns></returns>
        private static bool CheckMaxTries()
        {
            s_tries++;
            if (s_tries >= 3)
            {
                ColorConsoleTextLine(ConsoleColor.Red, "You have exceeded the number of tries!");
                s_tries = 0;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates a new vault.
        /// </summary>
        private static void CreateVault()
        {
            string vaultName;
            string masterPassword1;
            string masterPassword2;
            bool userNameIsValid = false;
            bool passValidation = false;
            do
            {
                Console.WriteLine("Vault Name: ");
                vaultName = Console.ReadLine();
                vaultName = vaultName.ToLower();
                var vaultFiles = Directory.GetFiles(s_vaultsDir);
                if (vaultName.Length < 3)
                {
                    ColorConsoleTextLine(ConsoleColor.Yellow, "Vault name must be at least 3 characters long!");
                }
                else if (string.Join("\n", vaultFiles).Contains($"{vaultName}.x"))
                {
                    ColorConsoleTextLine(ConsoleColor.Yellow, $"Vault {vaultName} already exist!");
                }
                else
                {
                    userNameIsValid = true;
                }
                if (CheckMaxTries())
                    return;
            } while (userNameIsValid == false);
            s_tries = 0;
            do
            {
                Console.WriteLine("Master Password: ");
                masterPassword1 = PasswordValidator.GetHiddenConsoleInput().ConvertSecureStringToString();
                Console.WriteLine();
                Console.WriteLine("Confirm Master Password: ");
                masterPassword2 = PasswordValidator.GetHiddenConsoleInput().ConvertSecureStringToString();
                Console.WriteLine();
                if (masterPassword1 != masterPassword2)
                {
                    ColorConsoleTextLine(ConsoleColor.Yellow, "Passwords are not the same!");
                }
                else
                {
                    passValidation = true;
                }
                if (!PasswordValidator.ValidatePassword(masterPassword2))
                    ColorConsoleTextLine(ConsoleColor.Yellow, "Password must be at least 12 characters, and must include at least one upper case letter, one lower case letter, one numeric digit, one special character and no space!");
                if (CheckMaxTries())
                    return;
            } while ((masterPassword1 != masterPassword2) || !PasswordValidator.ValidatePassword(masterPassword2));
            s_tries = 0;
            if (passValidation)
            {
                string sealVault = AES.Encrypt(string.Empty, masterPassword1);
                if (!sealVault.Contains("Error encrypting"))
                {
                    File.WriteAllText(s_vaultsDir + $"//{vaultName}.x", sealVault);
                    WordColorInLine("\n[+] Vault ", vaultName, " was created!\n", ConsoleColor.Cyan);
                    return;
                }
                ErrorWriteLine(sealVault + ". Check command!");
            }
        }

        /// <summary>
        /// Deletes an existing vault.
        /// </summary>
        private static void DeleteVault()
        {
            Console.WriteLine("Enter vault name: ");
            string vaultName = Console.ReadLine();
            vaultName = vaultName.ToLower();
            var vaultFiles = Directory.GetFiles(s_vaultsDir);
            while (!string.Join("\n", vaultFiles).Contains($"{vaultName}.x"))
            {
                if (CheckMaxTries())
                    return;
                ColorConsoleTextLine(ConsoleColor.Yellow, $"Vault {vaultName} does not exist!");
                Console.WriteLine("Enter vault name: ");
                vaultName = Console.ReadLine();
            }
            s_tries = 0;
            string encryptedData = File.ReadAllText(s_vaultsDir + $"//{vaultName}.x");
            WordColorInLine("Enter master password for ", vaultName, " vault:", ConsoleColor.Cyan);
            string masterPassword = PasswordValidator.GetHiddenConsoleInput().ConvertSecureStringToString();
            Console.WriteLine();
            string decryptVault = AES.Decrypt(encryptedData, masterPassword);

            if (decryptVault.Contains("Error decrypting"))
            {
                ErrorWriteLine("Something went wrong. Check master password or vault name!");
                return;
            }

            if (string.Join("\n", vaultFiles).Contains(vaultName))
            {
                File.Delete(s_vaultsDir + $"//{vaultName}.x");
                WordColorInLine("\n[-] Vault ", vaultName, " was deleted!\n", ConsoleColor.Cyan);
            }
            else
            {
                ColorConsoleTextLine(ConsoleColor.Yellow, $"Vault {vaultName} does not exist anymore!");
            }
        }


        /// <summary>
        /// List current vaults.
        /// </summary>
        private static void ListVaults()
        {
            string outFiles = string.Empty;
            var getFiles = Directory.GetFiles(s_vaultsDir);
            int filesCount = getFiles.Count();
            foreach (var file in getFiles)
            {
                if (file.EndsWith(".x"))
                {
                    FileInfo fileInfo = new FileInfo(file);
                    string vaultName = fileInfo.Name.Substring(0, fileInfo.Name.Length - 2);
                    outFiles += "----------------\n";
                    outFiles += vaultName + Environment.NewLine;
                }
            }
            if (filesCount == 0)
            {
                ColorConsoleTextLine(ConsoleColor.Yellow, "There are no vaults created!");
                return;
            }
            Console.WriteLine("List of current vaults:");
            Console.WriteLine(outFiles + "----------------");
        }


        /// <summary>
        /// Check if vault exists.
        /// </summary>
        /// <param name="vaultName"></param>
        /// <returns></returns>
        private static bool CheckVaultExist(string vaultName)
        {
            var getFiles = Directory.GetFiles(s_vaultsDir);
            foreach (var file in getFiles)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.Name.Contains(vaultName))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Add new application to a current vault.
        /// </summary>
        private static void AddPasswords()
        {
            Console.WriteLine("Enter vault name:");
            string vault = Console.ReadLine();
            vault = vault.ToLower();
            while (!CheckVaultExist(vault))
            {
                if (CheckMaxTries())
                    return;
                ColorConsoleTextLine(ConsoleColor.Yellow, "Vault does not exist!");
                Console.WriteLine("Enter vault name:");
                vault = Console.ReadLine();
            }
            s_tries = 0;
            string encryptedData = File.ReadAllText(s_vaultsDir + $"//{vault}.x");
            WordColorInLine("Enter master password for ", vault, " vault:", ConsoleColor.Cyan);
            string masterPassword = PasswordValidator.GetHiddenConsoleInput().ConvertSecureStringToString();
            Console.WriteLine();
            string decryptVault = AES.Decrypt(encryptedData, masterPassword);
            if (decryptVault.Contains("Error decrypting"))
            {
                ErrorWriteLine("Something went wrong. Check master password or vault name!");
                return;
            }
            Console.WriteLine("Enter application name:");
            string application = Console.ReadLine();
            while (application.Length < 3)
            {
                if (CheckMaxTries())
                    return;
                ColorConsoleTextLine(ConsoleColor.Yellow, "The length of application name should be at least 3 characters!");
                Console.WriteLine("Enter application name:");
                application = Console.ReadLine();
            }
            s_tries = 0;
            WordColorInLine("Enter account name for ", application, ":", ConsoleColor.Magenta);
            string account = Console.ReadLine();
            while (account.Length < 3)
            {
                if (CheckMaxTries())
                    return;
                ColorConsoleTextLine(ConsoleColor.Yellow, "The length of account name should be at least 3 characters!");
                WordColorInLine("Enter account name for ", application, ":", ConsoleColor.Magenta);
                account = Console.ReadLine();
            }
            s_tries = 0;
            WordColorInLine("Enter password for ", account, ":", ConsoleColor.Green);
            string password = PasswordValidator.GetHiddenConsoleInput().ConvertSecureStringToString();
            Console.WriteLine();
            var keyValues = new Dictionary<string, object>
                {
                    { "site/application", application },
                    { "account", account },
                    { "password", password },
                };
            string encryptdata = AES.Encrypt(decryptVault + "\n" + JsonSerializer.Serialize(keyValues), masterPassword);
            if (File.Exists(s_vaultsDir + $"//{vault}.x"))
            {
                File.WriteAllText(s_vaultsDir + $"//{vault}.x", encryptdata);
                WordColorInLine("\n[+] Data for ", application, " is encrypted and added to vault!\n", ConsoleColor.Magenta);
                return;
            }
            else
            {
                ColorConsoleTextLine(ConsoleColor.Yellow, $"Vault {vault} does not exist!");
            }
        }

        /// <summary>
        /// Displays applicaitons from an existing vault.
        /// </summary>
        private static void ReadPass()
        {
            string decryptVault = DecryptData();
            if (decryptVault.Contains("Error decrypting"))
            {
                ErrorWriteLine("Something went wrong. Check master password or vault name!");
                return;
            }
            if (string.IsNullOrEmpty(decryptVault))
            {
                ColorConsoleTextLine(ConsoleColor.Yellow, "There is no data saved in this vault!");
                return;
            }
            Console.WriteLine("Enter application name (leave blank for all applications):");
            string application = Console.ReadLine();
            if (application.Length > 0)
            {
                WordColorInLine("This is your decrypted data for ", application, ":", ConsoleColor.Magenta);
            }
            else
            {
                Console.WriteLine("This is your decrypted data for the entire vault:");
            }
            using (var reader = new StringReader(decryptVault))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains(application) && line.Length > 0)
                    {
                        var outJson = JsonSerializer.Deserialize<Dictionary<string, string>>(line);
                        if (outJson["site/application"].Contains(application))
                        {
                            Console.WriteLine("-------------------------");
                            Console.WriteLine($"Application Name: ".PadRight(20, ' ') + outJson["site/application"]);
                            Console.WriteLine($"Account Name: ".PadRight(20, ' ') + outJson["account"]);
                            Console.WriteLine($"Password: ".PadRight(20, ' ') + outJson["password"]);
                        }
                    }
                }
                Console.WriteLine("-------------------------");
            }
        }

        /// <summary>
        /// Decrypts vault data.
        /// </summary>
        /// <returns></returns>
        private static string DecryptData()
        {
            Console.WriteLine("Enter vault name:");
            string vault = Console.ReadLine();
            vault = vault.ToLower();
            while (!CheckVaultExist(vault))
            {
                if (CheckMaxTries())
                    return string.Empty;
                ColorConsoleTextLine(ConsoleColor.Yellow, "Vault does not exist!");
                Console.WriteLine("Enter vault name:");
                vault = Console.ReadLine();
            }
            s_tries = 0;
            string encryptedData = File.ReadAllText(s_vaultsDir + $"//{vault}.x");
            WordColorInLine("Enter master password for ", vault, " vault:", ConsoleColor.Cyan);
            string masterPassword = PasswordValidator.GetHiddenConsoleInput().ConvertSecureStringToString();
            Console.WriteLine();
            string decryptVault = AES.Decrypt(encryptedData, masterPassword);
            return decryptVault;
        }

        /// <summary>
        /// Delete an application from vault.
        /// </summary>
        private static void DeleteAppUserData()
        {
            List<string> listApps = new List<string>();
            bool accountCheck = false;
            Console.WriteLine("Enter vault name:");
            string vault = Console.ReadLine();
            vault = vault.ToLower();
            while (!CheckVaultExist(vault))
            {
                if (CheckMaxTries())
                    return;
                ColorConsoleTextLine(ConsoleColor.Yellow, "Vault does not exist!");
                Console.WriteLine("Enter vault name:");
                vault = Console.ReadLine();
            }
            s_tries = 0;
            string encryptedData = File.ReadAllText(s_vaultsDir + $"//{vault}.x");
            WordColorInLine("Enter master password for ", vault, " vault:", ConsoleColor.Cyan);
            string masterPassword = PasswordValidator.GetHiddenConsoleInput().ConvertSecureStringToString();
            Console.WriteLine();
            string decryptVault = AES.Decrypt(encryptedData, masterPassword);
            if (decryptVault.Contains("Error decrypting"))
            {
                ErrorWriteLine("Something went wrong. Check master password or vault name!");
                return;
            }
            Console.WriteLine("Enter application name:");
            string application = Console.ReadLine();
            while (string.IsNullOrEmpty(application))
            {
                if (CheckMaxTries())
                    return;
                ColorConsoleTextLine(ConsoleColor.Yellow, "Application name should not be empty!");
                Console.WriteLine("Enter application name:");
                application = Console.ReadLine();
            }
            s_tries = 0;
            while (!decryptVault.Contains(application))
            {
                if (CheckMaxTries())
                    return;
                ColorConsoleTextLine(ConsoleColor.Yellow, $"Application {application} does not exist!");
                Console.WriteLine("Enter application name:");
                application = Console.ReadLine();
            }
            s_tries = 0;
            WordColorInLine("Enter account name for ", application, ":", ConsoleColor.Magenta);
            string accountName = Console.ReadLine();
            while (string.IsNullOrEmpty(accountName))
            {
                if (CheckMaxTries())
                    return;
                ColorConsoleTextLine(ConsoleColor.Yellow, "Account name should not be empty!");
                WordColorInLine("Enter account name for ", application, ":", ConsoleColor.Magenta);
                accountName = Console.ReadLine();
            }
            s_tries = 0;
            using (var reader = new StringReader(decryptVault))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length > 0)
                    {
                        listApps.Add(line);
                        var outJson = JsonSerializer.Deserialize<Dictionary<string, string>>(line);
                        if (outJson["site/application"] == application && outJson["account"] == accountName)
                        {
                            listApps.Remove(line);
                            accountCheck = true;
                        }
                    }
                }
                string encryptdata = AES.Encrypt(string.Join("\n", listApps), masterPassword);
                listApps.Clear();
                if (File.Exists(s_vaultsDir + $"//{vault}.x"))
                {
                    if (accountCheck)
                    {
                        File.WriteAllText(s_vaultsDir + $"//{vault}.x", encryptdata);
                        Console.Write("\n[-]Account ");
                        ColorConsoleText(ConsoleColor.Green, accountName);
                        Console.Write(" for ");
                        ColorConsoleText(ConsoleColor.Magenta, application);
                        Console.Write(" was deleted!" + Environment.NewLine);
                        return;
                    }
                    ColorConsoleTextLine(ConsoleColor.Yellow, $"Account {accountName} does not exist!");
                    return;
                }
                ColorConsoleTextLine(ConsoleColor.Yellow, $"Vault {vault} does not exist!");
            }
        }


        /// <summary>
        /// Update account's password from an application.
        /// </summary>
        private static void UpdateAppUserData()
        {
            List<string> listApps = new List<string>();
            bool accountCheck = false;
            Console.WriteLine("Enter vault name:");
            string vault = Console.ReadLine();
            vault = vault.ToLower();
            while (!CheckVaultExist(vault))
            {
                if (CheckMaxTries())
                    return;
                ColorConsoleTextLine(ConsoleColor.Yellow, "Vault does not exist!");
                Console.WriteLine("Enter vault name:");
                vault = Console.ReadLine();
            }
            s_tries = 0;
            string encryptedData = File.ReadAllText(s_vaultsDir + $"//{vault}.x");
            WordColorInLine("Enter master password for ", vault, " vault:", ConsoleColor.Cyan);
            string masterPassword = PasswordValidator.GetHiddenConsoleInput().ConvertSecureStringToString();
            Console.WriteLine();
            string decryptVault = AES.Decrypt(encryptedData, masterPassword);
            if (decryptVault.Contains("Error decrypting"))
            {
                ErrorWriteLine("Something went wrong. Check master password or vault name!");
                return;
            }
            Console.WriteLine("Enter application name:");
            string application = Console.ReadLine();
            while (string.IsNullOrEmpty(application))
            {
                if (CheckMaxTries())
                    return;
                ColorConsoleTextLine(ConsoleColor.Yellow, "Application name should not be empty!");
                Console.WriteLine("Enter application name:");
                application = Console.ReadLine();
            }
            s_tries = 0;
            while (!decryptVault.Contains(application))
            {
                if (CheckMaxTries())
                    return;
                ColorConsoleTextLine(ConsoleColor.Yellow, $"Application {application} does not exist!");
                Console.WriteLine("Enter application name:");
                application = Console.ReadLine();
            }
            s_tries = 0;
            WordColorInLine("Enter account name for ", application, ":", ConsoleColor.Magenta);
            string accountName = Console.ReadLine();
            while (string.IsNullOrEmpty(accountName))
            {
                if (CheckMaxTries())
                    return;
                ColorConsoleTextLine(ConsoleColor.Yellow, "Account name should not be empty!");
                WordColorInLine("Enter account name for ", application, ":", ConsoleColor.Magenta);
                accountName = Console.ReadLine();
            }
            s_tries = 0;
            WordColorInLine("Enter new password for ", accountName, ":", ConsoleColor.Green);
            string password = PasswordValidator.GetHiddenConsoleInput().ConvertSecureStringToString();
            Console.WriteLine();
            using (var reader = new StringReader(decryptVault))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length > 0)
                    {
                        var outJson = JsonSerializer.Deserialize<Dictionary<string, string>>(line);
                        if (outJson["site/application"] == application && outJson["account"] == accountName)
                        {
                            var keyValues = new Dictionary<string, object>
                            {
                                 { "site/application", application },
                                 { "account", accountName },
                                 { "password", password },
                            };
                            accountCheck = true;
                            listApps.Add(JsonSerializer.Serialize(keyValues));
                        }
                        else
                        {
                            listApps.Add(line);
                        }
                    }
                }
                string encryptdata = AES.Encrypt(string.Join("\n", listApps), masterPassword);
                listApps.Clear();
                if (File.Exists(s_vaultsDir + $"//{vault}.x"))
                {
                    if (accountCheck)
                    {
                        File.WriteAllText(s_vaultsDir + $"//{vault}.x", encryptdata);
                        WordColorInLine("\n[*]Password for ", accountName, " was updated!\n", ConsoleColor.Green);
                        return;
                    }
                    ColorConsoleTextLine(ConsoleColor.Yellow, $"Account {accountName} does not exist!");
                    return;
                }
                ColorConsoleTextLine(ConsoleColor.Yellow, $"Vault {vault} does not exist!");
            }
        }

        /// <summary>
        /// Color a word from a string.
        /// </summary>
        /// <param name="beforeText">Text to be added befor word.</param>
        /// <param name="word">Word to be colored.</param>
        /// <param name="afterText">Text after the colored word.</param>
        /// <param name="color">Console color for the metioned word.</param>
        private static void WordColorInLine(string beforeText, string word, string afterText, ConsoleColor color)
        {
            Console.Write(beforeText);
            ColorConsoleText(color, word);
            Console.Write(afterText + Environment.NewLine);
        }
    }
}
