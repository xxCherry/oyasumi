namespace oyasumi.Utilities
{
    public class Crypto
    {
        public static string GenerateHash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, 10);
        }
        
        public static bool VerifyPassword(string plaintext, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(plaintext, hash);
        }
    }
}