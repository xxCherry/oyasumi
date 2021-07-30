using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using oyasumi.Database;
using oyasumi.Database.Models;
using oyasumi.Enums;
using oyasumi.Extensions;
using oyasumi.Interfaces;
using oyasumi.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace oyasumi.Controllers
{
    [Route("/users")]
    public class RegistrationController : OyasumiController
    {
        [HttpPost]
        public IActionResult Index()
        {
            var username = (string)Request.Form["user[username]"];
            var email = (string)Request.Form["user[user_email]"];
            var plainPassword = (string)Request.Form["user[password]"]; // plain text

            var errors = new Dictionary<string, List<string>>()
            {
                ["username"] = new (),
                ["user_email"] = new(),
                ["password"] = new()
            };

            var users = DbContext.Users.Values;

            if (!Regex.IsMatch(username, @"^[\w \[\]-]{2,15}$"))
                errors["username"].Add("Must be 2 - 15 characters in length.");

            if (username.Contains(" ") && username.Contains("_"))
                errors["username"].Add("May contain '_' and ' ', but not both.");

            if (users.Any(x => x.Username == username))
                errors["username"].Add("Username already taken by another player..");

            if (!Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250)))
                errors["email"].Add("Invalid email syntax.");

            if (users.Any(x => x.Email == email))
                errors["email"].Add("Email already taken by another player.");

            var passwordLength = plainPassword.Length;
            if (!(passwordLength >= 8 && passwordLength <= 32))
                errors["password"].Add("Must be 8-32 characters in length.");

            var uniqueCharsCount = plainPassword.Distinct().Count();
            if (uniqueCharsCount <= 3)
                errors["password"].Add("Must have more than 3 unique characters.");


            if (errors["username"].Count > 0 || errors["user_email"].Count > 0 || errors["password"].Count > 0)
            {
                var serializedErrors = JsonConvert.SerializeObject(errors);
                var compiledJson = "{'form_error': {'user':" + serializedErrors + "}}";

                return NetUtils.Content(compiledJson, 422);
            }

            if (Request.Form["check"] != "0") 
                return Ok("<>");
            
            var passwordMd5 = Crypto.ComputeHash(plainPassword);
            var passwordBcrypt = Crypto.GenerateHash(passwordMd5);

            if (!Base.PasswordCache.TryGetValue(passwordMd5, out var _))
                Base.PasswordCache.TryAdd(passwordMd5, passwordBcrypt);

            var user = new User
            {
                Id = DbContext.Users.Count + 1,
                Username = username,
                UsernameSafe = username.ToSafe(),
                Password = passwordBcrypt,
                Country = "XX",
                Privileges = Privileges.Normal
            };

            var vanillaStats = new VanillaStats()
            {
                Id = user.Id
            };

            var relaxStats = new RelaxStats()
            {
                Id = user.Id
            };

            DbContext.Users.Add(user.Id, user.Username, user);

            DbContext.VanillaStats.TryAdd(user.Id, vanillaStats);
            DbContext.RelaxStats.TryAdd(user.Id, relaxStats);

            var token = new Token
            {
                UserId = user.Id,
                UserToken = Guid.NewGuid().ToString()
            };

            return Ok("<>");
        }
    }
}
