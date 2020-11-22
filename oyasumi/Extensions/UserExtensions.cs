using oyasumi.Managers;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.Extensions
{
    public static class UserExtensions
    {
        public static async Task<(string username, string password, int timezone)> ParseLoginDataAsync(this Stream self)
        {
            using var reader = new StreamReader(self, leaveOpen: true);

            var username = await reader.ReadLineAsync();
            var password = await reader.ReadLineAsync();
            var data = (await reader.ReadLineAsync()).Split("|");

            return (username, password, int.Parse(data[1]));
        }

        public static bool CheckLogin(this (string username, string password) self)
        {
            var pr = PresenceManager.GetPresenceByName(self.username);
            if (pr is null)
                return false;

            return Base.PasswordCache.TryGetValue(self.password, out _);
        }

        public static string ToSafe(this string self)
        {
            return self.Replace(" ", "_").ToLower();
        }
    }
}