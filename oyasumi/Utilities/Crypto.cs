using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using scr = Org.BouncyCastle.Crypto.Generators.SCrypt;

namespace oyasumi.Utilities
{
    /*public static class BCrypt
    {
        [DllImport(@"lib/BCrypt")]
        public static extern string generate_hash(string password, int rounds = 10);

        [DllImport(@"lib/BCrypt")]
        public static extern bool validate_password(string password, string hash);
    } */
    public class SCrypt
    {
        public static string RandomString(int n)
        {
            var ret = new StringBuilder();
            const string ascii = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";

            for (var i = 0; i < n; i++)
                ret.Append(ascii[new Random().Next(0, ascii.Length)]);

            return ret.ToString();
        }

        public static byte[] PseudoSecureBytes(int n)
        {
            var provider = new RNGCryptoServiceProvider();
            var byteArray = new byte[n];
            provider.GetBytes(byteArray);
            return byteArray;
        }

        private static byte[] generate_salt() => PseudoSecureBytes(new Random().Next(90, 100));

        public static (string password, byte[] salt) generate_hash(string password, int rounds = 20)
        {
            var pwBytes = Encoding.Default.GetBytes(password);
            var saltBytes = generate_salt();
            var pwHashBytes = scr.Generate(pwBytes, saltBytes, 262144 / 4, rounds, 1, 512);
            return (Convert.ToBase64String(pwHashBytes), saltBytes);
        }

        public static bool validate_password(string password, string hash, byte[] salt, int rounds = 20)
        {
            var pwBytes = Encoding.Default.GetBytes(password);
            var pwHashBytes = scr.Generate(pwBytes, salt, 262144 / 4, rounds, 1, 512);
            var hashBytes = Convert.FromBase64String(hash);

            return pwHashBytes.SequenceEqual(hashBytes);
        }
    }
    
    public static class Crypto
    {
        public static string GenerateHash(string password) =>
            BCrypt.Net.BCrypt.HashPassword(password, 10); // BCrypt.generate_hash(password, 10);

        public static bool VerifyPassword(string plaintext, string hash) => 
            BCrypt.Net.BCrypt.Verify(plaintext, hash); // BCrypt.validate_password(plaintext, hash);

        public static bool VerifySCryptPassword(string dbPassword, string rawPassword, byte[] salt, bool fromMd5 = false)
        {
            return SCrypt.validate_password(fromMd5 ? rawPassword : ComputeHash(rawPassword), dbPassword, salt);
        }
        
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