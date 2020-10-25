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
        [Route("bancho_connect.php")]
        public async Task<IActionResult> BanchoConnect()
        {
            return Ok("<>");
        }


        [HttpPost("osu-submit-modular-selector.php")]
        public async Task<IActionResult> SubmitModular()
        {
            var score = await ((string)Request.Form["score"], (string)Request.Form["iv"], (string)Request.Form["osuver"]).ToScore();

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
                    await score.Presence.UpdateAccuracy(score.PlayMode); // update old accuracy

                    score.PerformancePoints = 0; // no pp system yet

                    score.Presence.AddPlaycount(score.PlayMode);

                    score.Presence.AddScore(score.TotalScore, true, score.PlayMode);
                    score.Presence.AddScore(score.TotalScore, false, score.PlayMode);

                    score.Accuracy = OppaiProvider.CalculateAccuracy(score);

                    var context = OyasumiDbContextFactory.Get();

                    await context.Scores.AddAsync(score.ToDb());
                    await context.SaveChangesAsync();

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

                    await score.Presence.UpdateAccuracy(score.PlayMode); // now get new accuracy

                    score.Presence.UpdateUserStats();

                    var presenceAfter = score.Presence;

                    var bmChart = new Chart("beatmap", "astellia.club", "Beatmap Ranking", score.ScoreId, "", presenceBefore, presenceAfter);
                    var oaChart = new Chart("overall", "astellia.club", "Overall Ranking", score.ScoreId, "", presenceBefore, presenceAfter);

                    foreach (var otherPresence in PresenceManager.Presences.Values)
                        score.Presence.UserStats(otherPresence);

                    return Ok($"beatmapId:{beatmap.BeatmapId}|beatmapSetId:{beatmap.BeatmapSetId}|beatmapPlaycount:0|beatmapPasscount:0|approvedDate:\n\n" +
                              bmChart
                              + "\n"
                              + oaChart);
                default: // map is unranked so we'll just add score and playcount
                    score.Presence.AddScore(score.TotalScore, false, score.PlayMode);
                    score.Presence.AddPlaycount(score.PlayMode);

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
            if (scoreboardVersion != "4") // old client
                return Ok("error: pass");

            if (!(username, password).CheckLogin())
                return Ok("error: pass");

            //var artist = fileName.
            var (status, beatmap) = await BeatmapManager.Get(beatmapChecksum, new BeatmapTitle
            {
                Artist = fileName
            }, true);

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