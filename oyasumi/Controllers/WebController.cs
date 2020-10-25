using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using oyasumi.Database;
using oyasumi.Database.Models;
using oyasumi.Enums;
using oyasumi.Extensions;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;
using oyasumi.Utilities;

namespace oyasumi.Controllers
{
    [Route("/web/")]
    public class WebController : Controller
    {
        private readonly OyasumiDbContext _context;

        public WebController(OyasumiDbContext context)
        {
            _context = context;
        }

        [Route("bancho_connect.php")]
        public async Task<IActionResult> BanchoConnect()
        {
            return Ok("<>");
        }

        [HttpPost("osu-submit-modular-selector.php")]
        public async Task<IActionResult> SubmitModular()
        {
            var score = await ((string)Request.Form["score"], (string)Request.Form["iv"], (string)Request.Form["osuver"]).ToScore(_context);

            if (score is null)
                return Ok("error: no");
            if (score.Presence is null)
                return Ok("error: pass");
            if (!(score.Presence.Username, (string)Request.Form["pass"]).CheckLogin())
                return Ok("error: pass");
            /*if ((score.Presence.Privileges & Privileges.Restricted) > 0)
                 return Ok("error: no"); */

            var beatmap = score.Beatmap;
            Base.UserCache.TryGetValue(score.Presence.Username, out var user);

            switch (beatmap.Status)
            {
                case RankedStatus.NotSubmitted:
                    return Ok("error: no");
                case RankedStatus.Loved:
                case RankedStatus.Approved:
                case RankedStatus.Qualified:
                case RankedStatus.Ranked:
                    var presenceBefore = score.Presence;
                    await score.Presence.UpdateAccuracy(_context, score.PlayMode); // update old accuracy

                    score.PerformancePoints = 0; // no pp system yet

                    await score.Presence.AddPlaycount(_context, score.PlayMode);

                    await score.Presence.AddScore(_context, score.TotalScore, true, score.PlayMode);
                    await score.Presence.AddScore(_context, score.TotalScore, false, score.PlayMode);

                    score.Accuracy = OppaiProvider.CalculateAccuracy(score);

                    var dbScore = score.ToDb();

                    await _context.Scores.AddAsync(dbScore);
                    await score.Presence.Apply(_context);

                    score.ScoreId = dbScore.Id;

                    var replay = Request.Form.Files.GetFile("score");

                    await using (var m = new MemoryStream())
                    {
                        replay.CopyTo(m);
                        m.Position = 0;
                        score.ReplayChecksum = Crypto.ComputeHash(m.ToArray());
                        if (!string.IsNullOrEmpty(score.ReplayChecksum))
                        {
                            await System.IO.File.WriteAllBytesAsync($"data/osr/{score.ReplayChecksum}.osr", m.ToArray());
                        }
                    }

                    await score.Presence.UpdateAccuracy(_context, score.PlayMode); // now get new accuracy

                    await score.Presence.UpdateUserStats(_context);

                    var presenceAfter = score.Presence;

                    var bmChart = new Chart("beatmap", "astellia.club", "Beatmap Ranking", score.ScoreId, "", presenceBefore, presenceAfter);
                    var oaChart = new Chart("overall", "astellia.club", "Overall Ranking", score.ScoreId, "", presenceBefore, presenceAfter);

                    foreach (var otherPresence in PresenceManager.Presences.Values)
                        score.Presence.UserStats(otherPresence);

                    score.Beatmap.Leaderboard = await Score.GetFormattedScores(_context, beatmap.FileChecksum);

                    return Ok($"beatmapId:{beatmap.BeatmapId}|beatmapSetId:{beatmap.BeatmapSetId}|beatmapPlaycount:0|beatmapPasscount:0|approvedDate:\n\n" +
                              bmChart
                              + "\n"
                              + oaChart);
                default: // map is unranked so we'll just add score and playcount
                    await score.Presence.AddScore(_context, score.TotalScore, false, score.PlayMode);
                    await score.Presence.AddPlaycount(_context, score.PlayMode);
                    await score.Presence.Apply(_context);

                    foreach (var otherPresence in PresenceManager.Presences.Values)
                        score.Presence.UserStats(otherPresence);

                    return Ok("error: no");
            }
        }

        // TODO: make scores length configurable

        [HttpGet("osu-osz2-getscores.php")]
        public async Task<IActionResult> GetScores
        (
            [FromQuery(Name = "s")] bool getScores,
            [FromQuery(Name = "vv")] string scoreboardVersion, 
            [FromQuery(Name = "v")] RankingType scoreboardType,
            [FromQuery(Name = "c")] string beatmapChecksum, 
            [FromQuery(Name = "f")] string fileName,
            [FromQuery(Name = "m")] PlayMode mode,
            [FromQuery(Name = "i")] int setId,
            [FromQuery(Name = "mods")] Mods mods,
            [FromQuery(Name = "us")] string username, 
            [FromQuery(Name = "ha")] string password
        )
        {
            if (scoreboardVersion != "4") // check on old client
                return Ok("error: pass");

            if (!(username, password).CheckLogin())
                return Ok("error: pass");

            var (status, beatmap) = await BeatmapManager.Get(beatmapChecksum, true, _context);

            // TODO: add NeedUpdate
            return status switch
            {
                RankedStatus.NotSubmitted => Ok("-1|false"),
                RankedStatus.Approved => Ok($"{beatmap}\n" + beatmap.Leaderboard),
                _ => Ok("error: no")
            };
        }
    }
}