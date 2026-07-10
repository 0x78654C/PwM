using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace PwMLib
{
    public static class PasswordGenerator
    {
        private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
        private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Numbers = "0123456789";
        private const string Symbols = "`~!@#$%^&*()-_=+[]{}\\|;:',<.>/?";

        public static string GeneratePassword(
            int length = 16,
            bool useUpper = true,
            bool useLower = true,
            bool useSymbols = true,
            bool useNumbers = true)
        {
            if (length < 1)
                throw new ArgumentException($"Can not make a string of {length} length", nameof(length));

            var characterSets = new List<string>(4);
            if (useLower) characterSets.Add(Lowercase);
            if (useUpper) characterSets.Add(Uppercase);
            if (useNumbers) characterSets.Add(Numbers);
            if (useSymbols) characterSets.Add(Symbols);

            if (characterSets.Count == 0)
                throw new ArgumentException("At least one character type must be enabled.");
            if (length < characterSets.Count)
                throw new ArgumentException("Password length is too short for all enabled character types.", nameof(length));

            string allCharacters = string.Concat(characterSets);
            var result = new char[length];
            int index = 0;

            // Guarantee at least one character from every requested category.
            foreach (string characterSet in characterSets)
                result[index++] = RandomCharacter(characterSet);

            while (index < result.Length)
                result[index++] = RandomCharacter(allCharacters);

            CryptoShuffle(result);
            return new string(result);
        }

        private static char RandomCharacter(string characters) =>
            characters[RandomNumberGenerator.GetInt32(characters.Length)];

        private static void CryptoShuffle(char[] characters)
        {
            for (int i = characters.Length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (characters[i], characters[j]) = (characters[j], characters[i]);
            }
        }
    }
}
