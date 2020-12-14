using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using oyasumi.API.ResponseObjects;
using oyasumi.API.RequestObjects;
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

            return Content(JsonConvert.SerializeObject(new ProfileResponse()
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

        [HttpPost("register_account")]
        public async Task<IActionResult> Registration([FromBody] RegistrationRequest info)
        {
            var username = info.Login;
            var email = info.Email;
            var plainPassword = info.Password;

            var errors = new Dictionary<string, List<string>>()
            {
                ["username"] = new(),
                ["user_email"] = new(),
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
                errors["user_email"].Add("Invalid email syntax.");

            /* no email yet
            if (await _context.Users.AnyAsync(x => x.Email == email))
                errors["user_email"].Add("Email already taken by another player.");
            */

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

            await _context.SaveChangesAsync();

            Base.UserCache.Add(username, user.Id, user);
            Base.UserStatsCache[LeaderboardMode.Vanilla].TryAdd(user.Id, vanillaStats);
            Base.UserStatsCache[LeaderboardMode.Relax].TryAdd(user.Id, vanillaStats);

            return Ok("Register successful");
        }
    }
}