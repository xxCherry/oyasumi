using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using net.r_eg.Conari;

namespace oyasumi.Utilities
{
    /*public static class BCrypt
    {
        [DllImport(@"lib/BCrypt")]
        public static extern string generate_hash(string password, int rounds = 10);

        [DllImport(@"lib/BCrypt")]
        public static extern bool validate_password(string password, string hash);
    } */

    public static class Crypto
    {
        public static string GenerateHash(string password) =>
            BCrypt.Net.BCrypt.HashPassword(password, 10); // BCrypt.generate_hash(password, 10);

        public static bool VerifyPassword(string plaintext, string hash) => 
            BCrypt.Net.BCrypt.Verify(plaintext, hash); // BCrypt.validate_password(plaintext, hash);


        public static string ComputeHash(string str) => ComputeHash(Encoding.UTF8.GetBytes(str));

        public static string ComputeHash(byte[] buffer)
        {
            var md5 = MD5.Create();
            var data = md5.ComputeHash(buffer);
            var sb = new StringBuilder();

            foreach (var b in data)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }

        public static string DecryptString(string message, byte[] key, string iv)
        {
            var msgBytes = Convert.FromBase64String(message);
            var engine = new RijndaelEngine(256);

            var blockCipher = new CbcBlockCipher(engine);
            var cipher = new PaddedBufferedBlockCipher(blockCipher, new Pkcs7Padding());

            var keyParam = new KeyParameter(key);
            var keyParamWithIv = new ParametersWithIV(keyParam, Convert.FromBase64String(iv), 0, 32);

            cipher.Init(false, keyParamWithIv);

            var comparisonBytes = new byte[cipher.GetOutputSize(msgBytes.Length)];
            var length = cipher.ProcessBytes(msgBytes, comparisonBytes, 0);

            cipher.DoFinal(comparisonBytes, length);

            return Encoding.UTF8.GetString(comparisonBytes);
        }
    }
}