using oyasumi.Objects;
using System.Linq;

namespace oyasumi.Helpers
{
    public class UserHelper
    {
        public static int GetId(string username)
        {
            var UsernameSafe = username.ToLower().Replace(' ', '_');
            var Id = Global.Factory.Get().DBUsers.Where(u => u.UsernameSafe == UsernameSafe).Select(x => x.Id).FirstOrDefault();
            if (Id == default)
                return 0;
            return Id;
        }
        // TODO: Change implementation
        public static bool ValidatePassword(string password, string DbPassword)
        {
            if (BCrypt.Net.BCrypt.Verify(password, DbPassword))
                return true;
            return false;
        }
        public static string GenerateHash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, 10);
        }
    }
}
