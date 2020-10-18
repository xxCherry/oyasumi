using oyasumi.Managers;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.Extensions
{
    public static class UserExtensions
    {
        public static async Task<(string username, string password)> ParseLoginDataAsync (this Stream self)
        {
            using var reader = new StreamReader(self, leaveOpen: true);

            var username = await reader.ReadLineAsync();
            var password = await reader.ReadLineAsync();

            return (username, password);
        }

        public static bool CheckLogin(this (string username, string password) self)
        {
            var pr = PresenceManager.GetPresenceByName(self.username);
            if (pr is null)
                return false;

            if (Base.PasswordCache.TryGetValue(self.password, out _))
                return true;

            return false;
        }

        public static string ToSafe(this string self)
        {
            return self.Replace(" ", "_").ToLower();
        }
    }
}