using System;
using System.Collections.Generic;
using System.Text;
using OtpNet;
using Net.Codecrete.QrCodeGenerator;

namespace PwMLib
{
    public class Otp
    {
        private static string Name = "PwM";
        public static void GenerateQR( string secret, string vaultName,string pwm)
        {
            var bytes =  Base32Encoding.ToBytes(secret);
            var totp = new Totp(bytes);
            var uriString = new OtpUri(OtpType.Totp, secret, vaultName, pwm).ToString();

        }

        private static void GenerateQR(string otpauth)
        {
            var qr = QrCode.EncodeText(otpauth, QrCode.Ecc.Medium);
            string svg = qr.ToSvgString(4);
        }

        static void GenerateQRDisplay(string otpauth)
        {
            var segments = QrSegment.MakeSegments(otpauth);
            var qr = QrCode.EncodeSegments(segments, QrCode.Ecc.High, 5, 5, 2, false);
            for (int y = 0; y < qr.Size; y++)
            {
                for (int x = 0; x < qr.Size; x++)
                {
                    Console.Write(qr.GetModule(x, y));
                }
            }
        }
    }
}
