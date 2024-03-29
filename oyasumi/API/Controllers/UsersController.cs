﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using oyasumi.API.Request;
using oyasumi.API.Response;
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

            var user = DbContext.Users[userId];

            return Content(JsonConvert.SerializeObject(new ProfileResponse
            {
                Id = user.Id,
                Username = user.Username,
                Country = user.Country,
                Place = stats.Rank(mode),
                UserpageContent = user.UserpageContent,
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
            // TODO: port it
            IEnumerable<ProfileScoreResponse> scores = null;
            /*await using (var db = MySqlProvider.GetDbConnection())
            {
                scores = (await db
                        .QueryAsync<DbScore>($"SELECT * FROM Scores " +
                                             $"WHERE UserId = {userId} " +
                                             $"AND Relaxing = {(isRelax ? "1" : "0")} " +
                                             $"AND PlayMode = {(int) mode} " +
                                             $"AND Completed = {(int)CompletedStatus.Best} " +
                                             $"ORDER BY PerformancePoints DESC " +
                                             $"LIMIT {limit}")
                        )
                        .Select(x => new ProfileScoreResponse()
                        {
                            Id = x.Id,
                            Beatmap = BeatmapManager.Get(x.FileChecksum).Result.Item2,
                            Mods = x.Mods,
                            Count50 = x.Count50,
                            Count100 = x.Count100,
                            Count300 = x.Count300,
                            CountGeki = x.CountGeki,
                            CountKatu = x.CountKatu,
                            CountMiss = x.CountMiss,
                            Combo = x.MaxCombo,
                            Performance = x.PerformancePoints,
                            Accuracy = x.Accuracy,
                            Timestamp = x.Date.ToUnixTimestamp(),
                            Rank = Calculator.CalculateRank(x)
                        });
            }*/

            Response.ContentType = "application/json";
            return Content(JsonConvert.SerializeObject(scores));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest info)
        {
            var user = Base.UserCache[info.Login];

            if (user is null)
                return NetUtils.Error("User not found");

            /*if (!await Misc.VerifyCaptcha(info.CaptchaKey, Request.Headers["X-Real-IP"]))
                return StatusCode(400); */

            var passwordMd5 = Crypto.ComputeHash(info.Password);

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

                //await DbContext.Tokens.Add(token);

                Base.TokenCache.Add(user.Username, user.Id, token);
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
                return NetUtils.Content(JsonConvert.SerializeObject(errors), 422);

            var passwordMd5 = Crypto.ComputeHash(plainPassword);
            var passwordBcrypt = Crypto.GenerateHash(passwordMd5);

            if (!Base.PasswordCache.TryGetValue(passwordMd5, out _))
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

            var vanillaStats = new VanillaStats();
            var relaxStats = new RelaxStats();

            DbContext.Users.Add(user.Id, user.Username, user);

            DbContext.VanillaStats.TryAdd(user.Id, vanillaStats);
            DbContext.RelaxStats.TryAdd(user.Id, relaxStats);

            var token = new Token
            {
                UserId = user.Id,
                UserToken = Guid.NewGuid().ToString()
            };

            //await DbContext.Tokens.AddAsync(token);

            Base.TokenCache.Add(user.Username, user.Id, token);
            Base.UserCache.Add(user.Username, user.Id, user);

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
        public async Task<IActionResult> Me()
        {
            var tokenStr = Request.Headers["Authorization"];

            if (!Misc.VerifyToken(tokenStr))
                return StatusCode(400);

            var token = Base.TokenCache[tokenStr];

            if (token is not null)
                Base.TokenCache.Add(token.UserToken, token.UserId, token);
            else
                return NotFound();

            var user = DbContext.Users[token.UserId];

            if (user is not null)
            {
                Base.UserCache.Add(user.Username, user.Id, user);

                var vanilla = DbContext.VanillaStats[token.UserId];
                var relax = DbContext.RelaxStats[token.UserId];

                Base.UserStatsCache[LeaderboardMode.Vanilla].TryAdd(user.Id, vanilla);
                Base.UserStatsCache[LeaderboardMode.Relax].TryAdd(user.Id, relax);
            }
            else
            {
                return NotFound();
            }


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

        [HttpPost("add_friend")]
        public async Task<IActionResult> AddFriend([FromQuery(Name = "u")] int userId)
        {
            var tokenStr = Request.Headers["Authorization"];
            if (!Misc.VerifyToken(tokenStr))
                return StatusCode(400);

            var token = Base.TokenCache[tokenStr];
            var user = Base.UserCache[token.UserId];

            var exists = DbContext.Friends.FirstOrDefault(x => x.Friend2 == userId);

            if (exists is not null)
                return NetUtils.Error("User already added");

            DbContext.Friends.Add(new Friend
            {
                Friend1 = user.Id,
                Friend2 = userId
            });

            Base.FriendCache[user.Id].Add(userId);

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

            var user = DbContext.Users[token.UserId];
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
            var user = DbContext.Users[token.UserId];

            cachedUser.PreferNightcore = info.PreferNightcore;
            user.PreferNightcore = info.PreferNightcore;

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

            var user = DbContext.Users[cachedToken.UserId];
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

            var dbToken = new Token(); // TODO: update token
            dbToken = token;

            return Ok();
        }

        [HttpPatch("update_avatar")]
        public async Task<IActionResult> UpdateAvatar() 
        {
            var token = Request.Headers["Authorization"];
            if (!Misc.VerifyToken(token))
                return StatusCode(400);
            
            var dbToken = new Token(); // TODO: get token

            var avatar = Request.Form.Files.GetFile("File");
            if (avatar is null)
                return NetUtils.Error("File is invalid.");

            if (!Regex.IsMatch(avatar.FileName, @"([^\s]+(\.(?i)(jpg|png|jpeg))$)"))
                return NetUtils.Error("File is not an image.");

            await using (var m = new MemoryStream())
            {
                await avatar.CopyToAsync(m);
                m.Position = 0;
                await using var avatarFile =
                    System.IO.File.Create($"./data/avatars/{dbToken.UserId}.png");
                m.WriteTo(avatarFile);
                m.Close();
                avatarFile.Close();
            }

            return Ok();
        }
    }
}