using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using oyasumi.API.Response;
using oyasumi.Database;
using oyasumi.Database.Models;
using oyasumi.Enums;
using oyasumi.Extensions;
using oyasumi.Interfaces;
using oyasumi.Utilities;

namespace oyasumi.API.Controllers
{
    [ApiController]
    [Route("/api/")]
    public class LeaderboardController : Controller
    {
        private readonly OyasumiDbContext _context;

        public LeaderboardController(OyasumiDbContext context) =>
            _context = context;

        [HttpGet("leaderboard")]
        public async Task<IActionResult> Leaderboard(
            [FromQuery(Name = "mode")] PlayMode mode,
            [FromQuery(Name = "l")] int limit,
            [FromQuery(Name = "p")] int page,
            [FromQuery(Name = "relax")] bool isRelax,
            [FromQuery(Name = "country")] string country = "")
        {
            var stats = isRelax switch
            {
                false => Base.UserStatsCache[LeaderboardMode.Vanilla].Values,
                true => Base.UserStatsCache[LeaderboardMode.Relax].Values
            };
            
            Response.ContentType = "application/json";
            return Content(JsonConvert.SerializeObject(stats
                .Where(x => x.Performance(mode) > 0 & !Base.UserCache[x.Id].Banned())
                .OrderByDescending(x => x.Performance(mode))
                .Skip(page <= 1 ? 0 : (page - 1) * limit)
                .Take(limit)
                .Select(x => new LeaderboardResponse
                {
                    Id = x.Id,
                    Username = Base.UserCache[x.Id].Username,
                    Country = Base.UserCache[x.Id].Country,
                    Accuracy = x.Accuracy(mode),
                    Performance = x.Performance(mode),
                    Playcount = x.Playcount(mode)
                })));
        }
    }
}