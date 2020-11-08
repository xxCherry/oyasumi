﻿using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
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
        public IActionResult BanchoConnect()
        {
            return Ok("<>");
        }

        [HttpPost("osu-submit-modular-selector.php")]
        public async Task<IActionResult> SubmitModular()
        {
            Score score = await ((string)Request.Form["score"], (string)Request.Form["iv"], (string)Request.Form["osuver"]).ToScore(_context);

            if (score is null)
                return Ok("error: no");
            if (score.Presence is null)
                return Ok("error: pass");
            if (!(score.Presence.Username, (string)Request.Form["pass"]).CheckLogin())
                return Ok("error: pass");
            /*if ((score.Presence.Privileges & Privileges.Restricted) > 0)
                 return Ok("error: no"); */

            var beatmap = score.Beatmap;

            var user = Base.UserCache[score.Presence.Username];

            if (user == default)
                return Ok("error: no");

            switch (beatmap.Status)
            {
                case RankedStatus.NotSubmitted:
                    return Ok("error: no");
                case RankedStatus.Loved:
                case RankedStatus.Approved:
                case RankedStatus.Qualified:
                case RankedStatus.Ranked:
                    var failed = Request.Form["x"] == "1";

                    if (failed || !score.Passed)
                    {
                        score.Completed = CompletedStatus.Failed;
                        return Ok("error: no");
                    }

                    var presenceBefore = score.Presence;

                    var stats = await _context.UsersStats.FirstOrDefaultAsync(x => x.Id == score.Presence.Id);

                    await score.Presence.UpdateAccuracy(_context, stats, score.PlayMode); // update old accuracy
                    await score.Presence.UpdatePerformance(_context, stats, score.PlayMode); // update old performance

                    score.Presence.AddPlaycount(stats, score.PlayMode);

                    score.Presence.AddScore(stats, score.TotalScore, true, score.PlayMode);
                    score.Presence.AddScore(stats, score.TotalScore, false, score.PlayMode);

                    score.Accuracy = (float)Calculator.CalculateAccuracy(score);

                    var oldScore = await _context.Scores
                        .AsAsyncEnumerable()
                        .Where(x => x.Completed == CompletedStatus.Best &&
                                    x.UserId == score.Presence.Id &&
                                    x.FileChecksum == score.FileChecksum &&
                                    x.PlayMode == score.PlayMode)
                        .FirstOrDefaultAsync();

                    score.Completed = CompletedStatus.Best;

                    if (oldScore is not null) // if we already have score on the beatmap
                    {
                        if (oldScore.TotalScore <= score.TotalScore) // then check if our last score is better.
                            oldScore.Completed = CompletedStatus.Submitted;
                        else
                            score.Completed = CompletedStatus.Submitted;
                    }

                    var replay = Request.Form.Files.GetFile("score");

                    await using (var m = new MemoryStream())
                    {
                        await replay.CopyToAsync(m);
                        m.Position = 0;

                        score.ReplayChecksum = Crypto.ComputeHash(m.ToArray());

                        if (!string.IsNullOrEmpty(score.ReplayChecksum))
                            await System.IO.File.WriteAllBytesAsync($"data/osr/{score.ReplayChecksum}.osr", m.ToArray());
                    }

                    score.PerformancePoints = (float)await Calculator.CalculatePerformancePoints(score);

                    var dbScore = score.ToDb();

                    await _context.Scores.AddAsync(dbScore);

                    score.ScoreId = dbScore.Id;

                    await _context.SaveChangesAsync();

                    await score.Presence.UpdateAccuracy(_context, stats, score.PlayMode); // now get new accuracy

                    await score.Presence.UpdatePerformance(_context, stats, score.PlayMode); // now get new performance

                    await _context.SaveChangesAsync();

                    await score.Presence.UpdateRank(_context, stats, score.PlayMode);

                    await score.Presence.GetOrUpdateUserStats(_context, true);

                    var presenceAfter = score.Presence;

                    var bmChart = new Chart("beatmap", "astellia.club", "Beatmap Ranking", score.ScoreId, "", presenceBefore, presenceAfter);
                    var oaChart = new Chart("overall", "astellia.club", "Overall Ranking", score.ScoreId, "", presenceBefore, presenceAfter);

                    foreach (var otherPresence in PresenceManager.Presences.Values)
                        score.Presence.UserStats(otherPresence);

                    var scores = await Score.GetRawScores(_context, beatmap.FileChecksum, score.PlayMode);

                    score.Beatmap.LeaderboardCache[score.PlayMode].Clear(); // Clear the cache

                    foreach (var bScore in scores)
                        score.Beatmap.LeaderboardCache[score.PlayMode].TryAdd(bScore.UserId, bScore);

                    score.Beatmap.LeaderboardFormatted[score.PlayMode] = Score.FormatScores(scores, score.PlayMode);

                    return Ok($"beatmapId:{beatmap.Id}|beatmapSetId:{beatmap.SetId}|beatmapPlaycount:0|beatmapPasscount:0|approvedDate:\n\n" +
                              bmChart
                              + "\n"
                              + oaChart);
                default: // map is unranked so we'll just add score and playcount
                    stats = await _context.UsersStats.FirstOrDefaultAsync(x => x.Id == score.Presence.Id);
                    score.Presence.AddScore(stats, score.TotalScore, false, score.PlayMode);
                    score.Presence.AddPlaycount(stats, score.PlayMode);
                    await score.Presence.Apply(_context);

                    foreach (var otherPresence in PresenceManager.Presences.Values)
                        score.Presence.UserStats(otherPresence);

                    return Ok("error: no");
            }
        }

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

            var (status, beatmap) = await BeatmapManager.Get(beatmapChecksum, _context);

            // TODO: add NeedUpdate
            switch (status)
            {
                case RankedStatus.NotSubmitted:
                    return Ok("-1|false");
                case RankedStatus.Approved:
                    var personalBest = beatmap.LeaderboardCache[mode].TryGetValue(Base.UserCache[username].Id, out var score);
                    var personalBestString = string.Empty;

                    if (personalBest)
                        personalBestString = $"{score}";

                    return Ok($"{beatmap.ToString(mode)}\n"
                            + $"{personalBestString}\n"
                            + (string.Join("\n", beatmap.LeaderboardFormatted[mode])));
                default:
                    return Ok("-1|false");
            }
        }
    }
}