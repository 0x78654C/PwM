using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PwM.Encryption
{
    // Cryptograhic password generator class.
    // Credits to: https://www.codeproject.com/Articles/2393/A-C-Password-Generator

    public static class PasswordGenerator
    {
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyz";
        private const string Numbers = "0123456789";
        private const string Symbols = "`~!@#$%^&*()-_=+[]{}\\|;:'\\,<.>/?";
        private static Random _random = new Random(Guid.NewGuid().GetHashCode());

        public static string GeneratePassword(int length = 16, bool useUpper = true, bool useLower = true,
            bool useSymbols = true, bool useNumbers = true)
        {
            if (length < 1)
            {
                throw new ArgumentException($"Can not make a string of {length} length");
            }

            if (!new[] { useLower, useUpper, useSymbols, useNumbers }.Any(e => e))
            {
                throw new ArgumentException($"Can not make a string of {length} length while not using any chars");
            }

            var collection = useLower ? Alphabet.ToLower() : "";
            collection += useNumbers ? Numbers : "";
            collection += useUpper ? Alphabet.ToUpper() : "";
            collection += useSymbols ? Symbols : "";

            return GeneratePassword(string.Join("", collection.OrderBy(r => Guid.NewGuid().GetHashCode())), 0, length, useUpper, useLower, useSymbols, useNumbers);
        }

        private static string GeneratePassword(string collection, int attempt, int length = 16, bool useUpper = true, bool useLower = true,
             bool useSymbols = true, bool useNumbers = true)
        {
             var password = string.Join("", Enumerable.Range(0, length).Select(t => collection[_random.Next(0, collection.Length)]));
            if (length > 7 && attempt < 5 && (useLower && !password.Any(e => Alphabet.ToLower().Contains(e))) ||
                (useSymbols && !password.Any(e => Symbols.Contains(e))) ||
                    (useNumbers && !password.Any(e => Numbers.Contains(e))) ||
                (useUpper && !password.Any(e => Alphabet.ToUpper().Contains(e))))
            {
                return GeneratePassword(collection, attempt + 1, length, useUpper, useUpper, useSymbols,useNumbers);
            }
            return password;
        }
    }
}

