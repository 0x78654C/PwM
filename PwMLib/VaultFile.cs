using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PwMLib
{
    public static class VaultFile
    {
        public const int MaximumEncodedLength = 128 * 1024 * 1024;

        public static string ReadAllText(string path)
        {
            using var stream = File.OpenRead(path);
            CheckLength(stream);
            using var reader = new StreamReader(stream);
            var text = new StringBuilder();
            var buffer = new char[8192];
            int count;
            while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (text.Length > MaximumEncodedLength - count)
                    throw new InvalidDataException("The vault file is too large.");
                text.Append(buffer, 0, count);
            }
            return text.ToString();
        }

        public static async Task<byte[]> ReadBytesAsync(Stream stream)
        {
            CheckLength(stream);
            using var content = new MemoryStream();
            var buffer = new byte[8192];
            int count;
            while ((count = await stream.ReadAsync(buffer)) > 0)
            {
                if (content.Length > MaximumEncodedLength - count)
                    throw new InvalidDataException("The vault file is too large.");
                content.Write(buffer, 0, count);
            }
            return content.ToArray();
        }

        private static void CheckLength(Stream stream)
        {
            if (stream.CanSeek && stream.Length > MaximumEncodedLength)
                throw new InvalidDataException("The vault file is too large.");
        }
    }
}
