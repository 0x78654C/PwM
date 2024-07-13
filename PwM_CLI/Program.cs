using PwMLib;
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
If you like this application and want to support the project you can always drink a coffee and stay cool and chill ;).
Thank you very much for your support, I appreciate it!
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
                var rootPath = Path.GetPathRoot(Environment.SystemDirectory);
                var accountName = Environment.UserName;
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
            if (s_tries < 3) return false;
            ColorConsoleTextLine(ConsoleColor.Red, "You have exceeded the number of tries!");
            s_tries = 0;
            return true;
        }

        /// <summary>
        /// Creates a new vault.
        /// </summary>
        private static void CreateVault()
        {
            string vaultName;
            string masterPassword1;
            string masterPassword2;
            var userNameIsValid = false;
            var passValidation = false;
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
                    ColorConsoleTextLine(ConsoleColor.Yellow,
                        "Password must be at least 12 characters, and must include at least one upper case letter, one lower case letter, one numeric digit, one special character and no space!");
                if (CheckMaxTries())
                    return;
            } while ((masterPassword1 != masterPassword2) || !PasswordValidator.ValidatePassword(masterPassword2));

            s_tries = 0;
            if (!passValidation) return;
            var sealVault = AES.Encrypt(string.Empty, masterPassword1);
            if (!sealVault.Contains("Error encrypting"))
            {
                File.WriteAllText(s_vaultsDir + $"//{vaultName}.x", sealVault);
                WordColorInLine("\n[+] Vault ", vaultName, " was created!\n", ConsoleColor.Cyan);
                return;
            }

            ErrorWriteLine(sealVault + ". Check command!");
        }

        /// <summary>
        /// Deletes an existing vault.
        /// </summary>
        private static void DeleteVault()
        {
            Console.WriteLine("Enter vault name: ");
            var vaultName = Console.ReadLine();
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
            var encryptedData = File.ReadAllText(s_vaultsDir + $"//{vaultName}.x");
            WordColorInLine("Enter master password for ", vaultName, " vault:", ConsoleColor.Cyan);
            var masterPassword = PasswordValidator.GetHiddenConsoleInput().ConvertSecureStringToString();
            Console.WriteLine();
            var decryptVault = AES.Decrypt(encryptedData, masterPassword);

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
            var getFiles = Directory.GetFiles(s_vaultsDir);
            if (!getFiles.Any())
            {
                ColorConsoleTextLine(ConsoleColor.Yellow, "There are no vaults created!");
                return;
            }

            var names = getFiles.Select(w => new FileInfo(w).Name[..^2]);
            var outFiles = string.Join(Environment.NewLine, names.Select(w => $"----------------\n{w}"));
            Console.WriteLine("List of current vaults:");
            Console.WriteLine(outFiles + "----------------");
        }


        /// <summary>
        /// Check if vault exists.
        /// </summary>
        /// <param name="vaultName"></param>
        /// <returns></returns>
        private static bool CheckVaultExist(string vaultName) => Directory.GetFiles(s_vaultsDir)
            .Select(file => new FileInfo(file))
            .Any(fileInfo => fileInfo.Name.Contains(vaultName));


        /// <summary>
        /// Add new application to a current vault.
        /// </summary>
        private static void AddPasswords()
        {
            Console.WriteLine("Enter vault name:");
            var vault = Console.ReadLine();
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
            var encryptedData = File.ReadAllText(s_vaultsDir + $"//{vault}.x");
            WordColorInLine("Enter master password for ", vault, " vault:", ConsoleColor.Cyan);
            var masterPassword = PasswordValidator.GetHiddenConsoleInput().ConvertSecureStringToString();
            Console.WriteLine();
            var decryptVault = AES.Decrypt(encryptedData, masterPassword);
            if (decryptVault.Contains("Error decrypting"))
            {
                ErrorWriteLine("Something went wrong. Check master password or vault name!");
                return;
            }

            Console.WriteLine("Enter application name:");
            var application = Console.ReadLine();
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
            var account = Console.ReadLine();
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
            var password = PasswordValidator.GetHiddenConsoleInput().ConvertSecureStringToString();
            Console.WriteLine();
            var keyValues = new Dictionary<string, object>
            {
                { "site/application", application },
                { "account", account },
                { "password", password },
            };
            var encryptData = AES.Encrypt($"{decryptVault}\n{JsonSerializer.Serialize(keyValues)}", masterPassword);
            if (File.Exists(s_vaultsDir + $"//{vault}.x"))
            {
                File.WriteAllText(s_vaultsDir + $"//{vault}.x", encryptData);
                WordColorInLine("\n[+] Data for ", application, " is encrypted and added to vault!\n", ConsoleColor.Magenta);
                return;
            }

            ColorConsoleTextLine(ConsoleColor.Yellow, $"Vault {vault} does not exist!");
        }

        /// <summary>
        /// Displays applications from an existing vault.
        /// </summary>
        private static void ReadPass()
        {
            var decryptVault = DecryptData();
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
            var application = Console.ReadLine();
            if (application.Length > 0)
            {
                WordColorInLine("This is your decrypted data for ", application, ":", ConsoleColor.Magenta);
            }
            else
            {
                Console.WriteLine("This is your decrypted data for the entire vault:");
            }

            using var reader = new StringReader(decryptVault);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (!line.Contains(application) || line.Length <= 0) continue;

                var outJson = JsonSerializer.Deserialize<Dictionary<string, string>>(line);

                if (!outJson["site/application"].Contains(application)) continue;
                Console.WriteLine("-------------------------");
                Console.WriteLine($"Application Name: ".PadRight(20, ' ') + outJson["site/application"]);
                Console.WriteLine($"Account Name: ".PadRight(20, ' ') + outJson["account"]);
                Console.WriteLine($"Password: ".PadRight(20, ' ') + outJson["password"]);
            }

            Console.WriteLine("-------------------------");
        }

        /// <summary>
        /// Decrypts vault data.
        /// </summary>
        /// <returns></returns>
        private static string DecryptData()
        {
            Console.WriteLine("Enter vault name:");
            var vault = Console.ReadLine();
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
            var encryptedData = File.ReadAllText(s_vaultsDir + $"//{vault}.x");
            WordColorInLine("Enter master password for ", vault, " vault:", ConsoleColor.Cyan);
            var masterPassword = PasswordValidator.GetHiddenConsoleInput().ConvertSecureStringToString();
            Console.WriteLine();
            var decryptVault = AES.Decrypt(encryptedData, masterPassword);
            return decryptVault;
        }

        /// <summary>
        /// Delete an application from vault.
        /// </summary>
        private static void DeleteAppUserData()
        {
            var listApps = new List<string>();
            var accountCheck = false;
            Console.WriteLine("Enter vault name:");
            var vault = Console.ReadLine();
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
            var encryptedData = File.ReadAllText(s_vaultsDir + $"//{vault}.x");
            WordColorInLine("Enter master password for ", vault, " vault:", ConsoleColor.Cyan);
            var masterPassword = PasswordValidator.GetHiddenConsoleInput().ConvertSecureStringToString();
            Console.WriteLine();
            var decryptVault = AES.Decrypt(encryptedData, masterPassword);
            if (decryptVault.Contains("Error decrypting"))
            {
                ErrorWriteLine("Something went wrong. Check master password or vault name!");
                return;
            }

            Console.WriteLine("Enter application name:");
            var application = Console.ReadLine();
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
            var accountName = Console.ReadLine();
            while (string.IsNullOrEmpty(accountName))
            {
                if (CheckMaxTries())
                    return;
                ColorConsoleTextLine(ConsoleColor.Yellow, "Account name should not be empty!");
                WordColorInLine("Enter account name for ", application, ":", ConsoleColor.Magenta);
                accountName = Console.ReadLine();
            }

            s_tries = 0;
            using var reader = new StringReader(decryptVault);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length <= 0) continue;

                listApps.Add(line);
                var outJson = JsonSerializer.Deserialize<Dictionary<string, string>>(line);
                if (outJson["site/application"] != application || outJson["account"] != accountName) continue;
                listApps.Remove(line);
                accountCheck = true;
            }

            var encryptdata = AES.Encrypt(string.Join("\n", listApps), masterPassword);
            listApps.Clear();
            if (!File.Exists(s_vaultsDir + $"//{vault}.x"))
            {
                ColorConsoleTextLine(ConsoleColor.Yellow, $"Vault {vault} does not exist!");
                return;
            }

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
        }


        /// <summary>
        /// Update account's password from an application.
        /// </summary>
        private static void UpdateAppUserData()
        {
            var listApps = new List<string>();
            var accountCheck = false;
            Console.WriteLine("Enter vault name:");
            var vault = Console.ReadLine();
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
            var encryptedData = File.ReadAllText(s_vaultsDir + $"//{vault}.x");
            WordColorInLine("Enter master password for ", vault, " vault:", ConsoleColor.Cyan);
            var masterPassword = PasswordValidator.GetHiddenConsoleInput().ConvertSecureStringToString();
            Console.WriteLine();
            var decryptVault = AES.Decrypt(encryptedData, masterPassword);
            if (decryptVault.Contains("Error decrypting"))
            {
                ErrorWriteLine("Something went wrong. Check master password or vault name!");
                return;
            }

            Console.WriteLine("Enter application name:");
            var application = Console.ReadLine();
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
            var accountName = Console.ReadLine();
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
            var password = PasswordValidator.GetHiddenConsoleInput().ConvertSecureStringToString();
            Console.WriteLine();
            using var reader = new StringReader(decryptVault);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length <= 0) continue;
                var outJson = JsonSerializer.Deserialize<Dictionary<string, string>>(line);
                if (outJson["site/application"] != application || outJson["account"] != accountName)
                {
                    listApps.Add(line);
                    continue;
                }

                var keyValues = new Dictionary<string, object>
                {
                    { "site/application", application },
                    { "account", accountName },
                    { "password", password },
                };
                accountCheck = true;
                listApps.Add(JsonSerializer.Serialize(keyValues));
            }

            var encryptData = AES.Encrypt(string.Join("\n", listApps), masterPassword);
            listApps.Clear();
            if (File.Exists(s_vaultsDir + $"//{vault}.x"))
            {
                if (accountCheck)
                {
                    File.WriteAllText(s_vaultsDir + $"//{vault}.x", encryptData);
                    WordColorInLine("\n[*]Password for ", accountName, " was updated!\n", ConsoleColor.Green);
                    return;
                }

                ColorConsoleTextLine(ConsoleColor.Yellow, $"Account {accountName} does not exist!");
                return;
            }

            ColorConsoleTextLine(ConsoleColor.Yellow, $"Vault {vault} does not exist!");
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