using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using oyasumi.API.RequestObjects;
using oyasumi.API.ResponseObjects;
using oyasumi.API.Utilities;
using oyasumi.Database;
using oyasumi.Database.Models;
using oyasumi.Enums;
using oyasumi.Extensions;
using oyasumi.Managers;
using oyasumi.Utilities;

namespace oyasumi.API.Controllers
{
    [ApiController]
    [Route("/api/users/")]
    [RequestFormLimits(ValueLengthLimit = 1024 * 1024 * 8, ValueCountLimit = 1024 * 1024 * 8)]
    public class UsersController : Controller
    {
        private readonly OyasumiDbContext _context;

        public UsersController(OyasumiDbContext context) =>
            _context = context;

        [HttpGet("profile/info")]
        public IActionResult UserProfile
        (
            [FromQuery(Name = "u")] int userId,
            [FromQuery(Name = "m")] PlayMode mode,
            [FromQuery(Name = "r")] bool isRelax
        )
        {
            var stats = isRelax switch
            {
                false => Base.UserStatsCache[LeaderboardMode.Vanilla][userId],
                true => Base.UserStatsCache[LeaderboardMode.Relax][userId]
            };

            var user = Base.UserCache[userId];

            return Content(JsonConvert.SerializeObject(new ProfileResponse
            {
                Id = user.Id,
                Username = user.Username,
                Place = stats.Rank(mode),
                UserpageContent = "",
                TotalScore = stats.TotalScore(mode),
                RankedScore = stats.RankedScore(mode),
                Performance = stats.Performance(mode),
                Accuracy = stats.Accuracy(mode),
                Playcount = stats.Playcount(mode),
                AccountCreatedAt = (int) user.JoinDate.ToUnixTimeSeconds(),
            }));
        }

        [HttpGet("profile/best_scores")]
        public async Task<IActionResult> BestScores
        (
            [FromQuery(Name = "u")] int userId,
            [FromQuery(Name = "m")] PlayMode mode,
            [FromQuery(Name = "l")] int limit,
            [FromQuery(Name = "r")] bool isRelax
        )
        {
            var scores = _context.Scores
                .AsAsyncEnumerable()
                .Where(x => x.UserId == userId
                            && x.Completed == CompletedStatus.Best
                            && x.Relaxing == isRelax
                )
                .OrderByDescending(x => x.PerformancePoints)
                .Take(limit);

            var formattedScores = new List<ProfileScoreResponse>();

            await foreach (var score in scores)
            {
                var beatmap = (await BeatmapManager.Get(score.FileChecksum, "", 0, _context)).Item2;
                formattedScores.Add(new()
                {
                    Id = score.Id,
                    Beatmap = beatmap,
                    Mods = score.Mods,
                    Performance = score.PerformancePoints,
                    Accuracy = score.Accuracy,
                    Timestamp = score.Date.ToUnixTimestamp(),
                    Rank = Calculator.CaculateRank(score)
                });
            }

            Response.ContentType = "application/json";
            return Content(JsonConvert.SerializeObject(formattedScores));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest info)
        {
            var user = Base.UserCache[info.Login];

            if (user is null)
                return NetUtils.Error("User not found");

            var passwordMd5 = Crypto.ComputeHash(info.Password);
            
            if (!await Misc.VerifyCaptcha(info.CaptchaKey, Request.Headers["X-Real-IP"]))
                return StatusCode(400);
            
            if (!Base.PasswordCache.TryGetValue(passwordMd5, out _))
            {
                if (!Crypto.VerifyPassword(passwordMd5, user.Password))
                    return NetUtils.Error("Wrong password");

                Base.PasswordCache.TryAdd(passwordMd5, user.Password);
            }
            
            var token = Base.TokenCache[user.Id];

            if (token is null)
            {
                token = new()
                {
                    UserId = user.Id,
                    UserToken = Guid.NewGuid().ToString()
                };

                await _context.Tokens.AddAsync(token);
                await _context.SaveChangesAsync();
            }
            
            var kvp = new Dictionary<string, string>
            {
                ["token"] = token.UserToken
            };
            
            return Content(JsonConvert.SerializeObject(kvp));
        }

        [HttpPost("register_account")]
        public async Task<IActionResult> Registration([FromBody] RegistrationRequest info)
        {
            var username = info.Login;
            var email = info.Email;
            var plainPassword = info.Password;

            if (!await Misc.VerifyCaptcha(info.CaptchaKey, Request.Headers["X-Real-IP"]))
                return StatusCode(400);
                
            var errors = new Dictionary<string, List<string>>
            {
                ["username"] = new(),
                ["email"] = new(),
                ["password"] = new()
            };

            if (!Regex.IsMatch(username, @"^[\w \[\]-]{2,15}$"))
                errors["username"].Add("Must be 2 - 15 characters in length.");
            if (username.Contains(" ") && username.Contains("_"))
                errors["username"].Add("May contain '_' and ' ', but not both.");
            if (await _context.Users.AnyAsync(x => x.Username == username))
                errors["username"].Add("Username already taken by another player..");

            if (!Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250)))
                errors["email"].Add("Invalid email syntax.");
            
            if (await _context.Users.AnyAsync(x => x.Email == email))
                errors["email"].Add("Email already taken by another player.");

            var passwordLength = plainPassword.Length;
            if (!(passwordLength >= 8 && passwordLength <= 32))
                errors["password"].Add("Must be 8-32 characters in length.");

            var uniqueCharsCount = plainPassword.Distinct().Count();
            if (uniqueCharsCount <= 3)
                errors["password"].Add("Must have more than 3 unique characters.");

            if (errors["username"].Count > 0 || errors["user_email"].Count > 0 || errors["password"].Count > 0)
                return NetUtils.Content(JsonConvert.SerializeObject(errors), 422);

            var passwordMd5 = Crypto.ComputeHash(plainPassword);
            var passwordBcrypt = Crypto.GenerateHash(passwordMd5);

            if (!Base.PasswordCache.TryGetValue(passwordMd5, out var _))
                Base.PasswordCache.TryAdd(passwordMd5, passwordBcrypt);

            var user = new User
            {
                Username = username,
                UsernameSafe = username.ToSafe(),
                Password = passwordBcrypt,
                Country = "XX",
                Privileges = Privileges.Normal
            };

            var vanillaStats = new VanillaStats();
            var relaxStats = new RelaxStats();

            await _context.Users.AddAsync(user);
            await _context.VanillaStats.AddAsync(vanillaStats);
            await _context.RelaxStats.AddAsync(relaxStats);

            var token = new Token
            {
                UserId = user.Id,
                UserToken = Guid.NewGuid().ToString()
            };

            await _context.Tokens.AddAsync(token);
            
            await _context.SaveChangesAsync();

            Base.UserCache.Add(username, user.Id, user);
            Base.TokenCache.Add(token.UserToken, token.UserId, token);
            
            Base.UserStatsCache[LeaderboardMode.Vanilla].TryAdd(user.Id, vanillaStats);
            Base.UserStatsCache[LeaderboardMode.Relax].TryAdd(user.Id, relaxStats);

            return Ok("Account created!");
        }

        [HttpGet("what_id")]
        public IActionResult WhatId([FromQuery(Name = "u")] string name)
        {
            var user = Base.UserCache[name];
            if (!(user is null) && !user.Banned())
                return NetUtils.StatusCode(user.Id, 200);
            return NetUtils.Error("User not found.");
        }

        [HttpGet("password_reset")]
        public IActionResult PasswordReset([FromQuery(Name = "u")] int userId, [FromQuery(Name = "p")] string password)
        {
            if (!Misc.VerifyToken(Request.Headers["Authorization"]))
                return StatusCode(400);
            
            var user = Base.UserCache[userId];
            
            if (user is null)
                return NetUtils.Error("User not found.");

            if ((user.Privileges & Privileges.ManageUsers) <= 0)
                return NetUtils.Error("You're not allowed to do that.");
            
            var passwordMd5 = Crypto.ComputeHash(password);
            var newPassword = Crypto.GenerateHash(passwordMd5);

            Base.PasswordCache.TryAdd(passwordMd5, newPassword);

            user.Password = newPassword;
                
            return Ok();
        }
        
        [HttpGet("me")]
        public IActionResult Me()
        {
            var tokenStr = Request.Headers["Authorization"];
            if (!Misc.VerifyToken(tokenStr))
                return StatusCode(400);

            var token = Base.TokenCache[tokenStr];
            var user = Base.UserCache[token.UserId];

            var response = new MeResponse
            {
                Id = user.Id,
                Username = user.Username,
                Privileges = user.Privileges,
                Email = user.Email,
                Banned = user.Banned(),
                PreferNightcore = user.PreferNightcore
            };

            return Content(JsonConvert.SerializeObject(response));
        }

        [HttpPost("users/add_friend")]
        public async Task<IActionResult> AddFriend([FromQuery(Name = "u")] int userId)
        {
            var tokenStr = Request.Headers["Authorization"];
            if (!Misc.VerifyToken(tokenStr))
                return StatusCode(400);
            
            var token = Base.TokenCache[tokenStr];
            var user = Base.UserCache[token.UserId];
            
            var exists = await _context.Friends.FirstOrDefaultAsync(x => x.Friend2 == userId);

            if (exists is not null)
                return NetUtils.Error("User already added");

            await _context.Friends.AddAsync(new Friend
            {
                Friend1 = user.Id,
                Friend2 = userId
            });

            Base.FriendCache[user.Id].Add(userId);
            await _context.SaveChangesAsync();

            return NetUtils.Content("Friend added");
        }

        [HttpPatch("update_userpage")]
        public async Task<IActionResult> UpdateUserpage([FromBody] UserpageUpdateRequest info)
        {
            var tokenStr = Request.Headers["Authorization"];
            if (!Misc.VerifyToken(tokenStr))
                return StatusCode(400);
            
            var token = Base.TokenCache[tokenStr];

            var isForbidden = true;
            foreach (var line in info.Content.Split("\n"))
            {
                if (!Regex.IsMatch(line, @"/\[(\w+)[^w]*?](.*?)\[\/\1]/g")) 
                    isForbidden = false;
            }

            if (!isForbidden)
                return NetUtils.StatusCode("Forbidden userpage content", 400);

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == token.UserId);
            user.UserpageContent = info.Content;
            
            return Ok();
        }

        [HttpPatch("update_user")]
        public async Task<IActionResult> UpdateUser([FromBody] UserUpdateRequest info)
        {
            var tokenStr = Request.Headers["Authorization"];
            if (!Misc.VerifyToken(tokenStr))
                return StatusCode(400);
            
            var token = Base.TokenCache[tokenStr];
            var cachedUser = Base.UserCache[token.UserId];
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == token.UserId);

            cachedUser.PreferNightcore = info.PreferNightcore;
            user.PreferNightcore = info.PreferNightcore;

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPatch("update_user_password")]
        public async Task<IActionResult> UpdateUserPassword([FromBody] UserUpdatePasswordRequest info)
        {
            var tokenStr = Request.Headers["Authorization"];
            if (!Misc.VerifyToken(tokenStr))
                return StatusCode(400);
            
            var cachedToken = Base.TokenCache[tokenStr];
            var cachedUser = Base.UserCache[cachedToken.UserId];
            
            var currentPasswordMd5 = Crypto.ComputeHash(info.CurrentPassword);
            if (!Base.PasswordCache.TryGetValue(currentPasswordMd5, out _))
            {
                if (!Crypto.VerifyPassword(currentPasswordMd5, cachedUser.Password))
                    return NetUtils.StatusCode("Invalid old password", 400);
            }
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == cachedToken.UserId);
            var newPasswordBcrypt = Crypto.GenerateHash(Crypto.ComputeHash(info.NewPassword));

            user.Password = newPasswordBcrypt;
            
            if (info.Email != user.Email) 
                user.Email = info.Email;
            
            var token = new Token
            {
                Id = cachedToken.Id,
                UserId = user.Id,
                UserToken = Guid.NewGuid().ToString()
            };
            Base.TokenCache[tokenStr] = token;
            
            var dbToken = await _context.Tokens.FirstOrDefaultAsync(x => x.UserId == token.Id);
            dbToken = token;

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}