using System.Security.Cryptography;
using System.Text;

namespace MPESA_V2_APIV2_MSISDN_DECRYPTER
{
    public static class CryptoUtils
    {
        public static byte[] HashToBinary(string val = "")
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes(val));
        }

        public static byte[] StringToBinary(string val = "")
        {
            return Encoding.UTF8.GetBytes(val);
        }

        public static string HashToString(string val = "")
        {
            return BitConverter.ToString(SHA256.HashData(Encoding.UTF8.GetBytes(val))).Replace("-", "").ToLower();
        }

        public static string ShortenHash(string hash = "")
        {
            return string.Concat(hash.AsSpan(0, 8), hash.AsSpan(56, 8));
        }

        public static string HashToShorter(string val = "")
        {
            return ShortenHash(HashToString(val).ToUpper());
        }

    }
}