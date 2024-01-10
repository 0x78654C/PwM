using System;
using OtpNet;
using Net.Codecrete.QrCodeGenerator;

namespace PwMLib
{
    /// <summary>
    /// Class for generating QR codes for vaults
    /// </summary>
    public class Otp
    {
        private static string Name = "PwM";

        /// <summary>
        /// Generate URI code.
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="vaultName"></param>
        /// <param name="pwm"></param>
        /// <returns></returns>
        private static string GenerateURI( string secret, string vaultName)
        {
            var secretClean = SecretClean(secret);
            var bytes =  Base32Encoding.ToBytes(secretClean);
            var totp = new Totp(bytes);
            var uriString = new OtpUri(OtpType.Totp, secret, vaultName, Name).ToString();
            return uriString;
        }

        /// <summary>
        /// Clean secret data for Base32 encoding.
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        public static string SecretClean(string secret)
        {
            if (secret.Contains("0"))
                secret = secret.Replace("0", "");
            if (secret.Contains("1"))
                secret = secret.Replace("1", "");
            if (secret.Contains("8"))
                secret = secret.Replace("8", "");
            if (secret.Contains("9"))
                secret = secret.Replace("9", "");

            string specialCharList = @"\|!#$%&/()=?»«@£§€{}.-;'<>_,";
            foreach(var chr in specialCharList)
            {
                if (secret.Contains(chr.ToString()))
                    secret = secret.Replace(chr.ToString(), "");
            }
            return secret;
        }

        /// <summary>
        ///  Generate SVG QR code
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="vaultName"></param>
        /// <param name="pwm"></param>
        private static void GenerateQRSVG(string secret, string vaultName)
        {
            string otpauth = GenerateURI(secret, vaultName);
            var qr = QrCode.EncodeText(otpauth, QrCode.Ecc.Medium);
            string svg = qr.ToSvgString(4);
        }


        /// <summary>
        /// Generate QR code for console or editor WPF. 
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="vaultName"></param>
        /// <param name="pwm"></param>
        public static void GenerateQRDisplay(string secret, string vaultName)
        {
            string otpauth = GenerateURI(secret, vaultName);
            var segments = QrSegment.MakeSegments(otpauth);
            var qr = QrCode.EncodeSegments(segments, QrCode.Ecc.Low, 1, 40, -1, false);
            Console.Write("\n");
            string result = string.Empty;
            for (int y = 0; y < qr.Size; y++)
            {
                for (int x = 0; x < qr.Size; x++)
                {
                    var c = (char)9608;
                    if (qr.GetModule(x, y))
                    {
                        Console.Write(c);
                        Console.Write(c);
                    }
                    else
                        Console.Write("  ");
                }
                Console.Write("\n");
            }
        }
    }
}
