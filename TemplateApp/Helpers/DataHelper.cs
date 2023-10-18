﻿using OtpNet;
using Protecc.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace Protecc.Helpers
{
    ///     Credentials are stored with three parameters. Name, Key and Resource
    ///     The Name contains the account name
    ///     The Key contains the 2FA key string
    ///     The resource contains a 8 digit identifier string with format: 
    ///     #Color in HEX format, Time in seconds (max 2 digits), Number of code digits (max 1 digit), Index representing encryptionmode enum
    ///     Encryption enums: 0 = Sha1, 1 = Sha256, 2 = Sha512
    ///     Example: Color white, 30 seconds, 6 digits, Sha512 will be FFFFFF3062
    ///     Example: Color black, 60 seconds, 8 digits, Sha1 will be 0000006080
    ///     Example: Color blue, 30 seconds, 6 digits, Sha1 will be "0000ff3060"

    public class DataHelper
    {
        public static string Encode(Color color, int TimeIndex, int DigitsIndex, int EncryptionIndex, int OTPTypeIndex) => color.ToString().Remove(0, 3) + (TimeIndex == 0 ? 30 : 60) + (DigitsIndex == 0 ? 6 : 8) + EncryptionIndex + OTPTypeIndex + "0"; // "0" for counter;

        public static SolidColorBrush DecodeColor(string Resource) => ColorUIHelper.HexToBrush(Resource.Substring(0, 6));

        public static int DecodeTime(string Resource) => Int32.Parse(Resource.Substring(6, 2));
        public static int DecodeDigits(string Resource) => Int32.Parse(Resource.Substring(8, 1));

        public static OtpHashMode DecodeEncryption(string Resource)
        {
            if (Resource.Substring(9, 1) == "0")
                return OtpHashMode.Sha1;
            else if (Resource.Substring(9, 1) == "1")
                return OtpHashMode.Sha256;
            else
                return OtpHashMode.Sha512;
        }

        public static int OTPTypeId(string Resource) {
            try
            {
                return Int32.Parse(Resource.Substring(10, 1));
            } catch (Exception)
            {
                return 0;
            }
        }

        public static int Counter(string Resource)
        {
            if (OTPTypeId(Resource) == 0)
                throw new Exception("OTP is not HOTP");
            else
                return Int32.Parse(Resource.Substring(11));
        }

        public static int DecodeEncryptionId(string Resource) => Int32.Parse(Resource.Substring(9, 1));
        /// <summary>
        /// Encodes string using previously decoded values
        /// </summary>
        public static string EncodeEdited(Color color, int savedTime, int savedDigits, int savedEncryptionIndex, int savedOTPTypeIndex, int savedCounter) => color.ToString().Remove(0, 3) + savedTime + savedDigits + savedEncryptionIndex + savedOTPTypeIndex + savedCounter;
        public static string CounterIncrement(string Resource)
        {
            if (OTPTypeId(Resource) == 0)
                throw new Exception("OTP is not HOTP");
            else
                return Resource.Substring(0, 11) + (Int32.Parse(Resource.Substring(11)) + 1);
        }

    }
}
