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

namespace oyasumi.Utilities
{
    public static class BCrypt
    {
        [DllImport(@"lib/BCrypt")]
        public static extern string generate_hash(string password, int rounds = 10);

        [DllImport(@"lib/BCrypt")]
        public static extern bool validate_password(string password, string hash);
    }

    public class Crypto
    {
        public static string GenerateHash(string password)
        {
            return BCrypt.generate_hash(password, 10);
        }
        
        public static bool VerifyPassword(string plaintext, string hash)
        {
            return BCrypt.validate_password(plaintext, hash);
        }


        public static string ComputeHash(byte[] buffer)
        {
            MD5 md5 = MD5.Create();

            byte[] data = md5.ComputeHash(buffer);

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
                sb.Append(data[i].ToString("x2"));

            return sb.ToString();
        }

        public static string DecryptString(string message, byte[] key, string iv)
        {
            // Works only on .NET framework
            /*var msgBytes = Convert.FromBase64String(message);

            using RijndaelManaged rj = new RijndaelManaged
            {
                Key = key,
                BlockSize = 256,
                Mode = CipherMode.CBC,
                IV = Convert.FromBase64String(iv)
            };

            using MemoryStream ms = new MemoryStream(msgBytes);

            using CryptoStream cs = new CryptoStream(ms, rj.CreateDecryptor(key, rj.IV), CryptoStreamMode.Read);

            var byteBuffer = new byte[msgBytes.Length];
            var length = await cs.ReadAsync(byteBuffer.AsMemory(0, msgBytes.Length));
            byte[] stringBuffer = new byte[length];
            Array.Copy(byteBuffer, stringBuffer, length);

            return Encoding.UTF8.GetString(stringBuffer); */

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